"""Day 9 first stage art: ruined cathedral (4 screens) + NOT_FINAL enemy silhouettes.

v1 (kept for history; v2 is make_day09_art.py). Outputs to ./v1/ (copy into Assets/SaintKnightDay09/Art/):
  stage1_bg.png        2560x360 painted background + terrain, 48 PPU = 4 gameplay screens
  stage1_layout.json   solid rects / spawn / enemy points in art pixels (the Unity builder reads this)
  enemy_a_monk_NOT_FINAL.png, enemy_b_executioner_NOT_FINAL.png  distinguishable placeholders

Tiles/props: GothicVania Church Pack (CC0, ../Lobby/external/gothicvania_church), desaturated like lobby v6.
"""
import json
from pathlib import Path

import numpy as np
from PIL import Image

HERE = Path(__file__).parent
OUT = HERE / 'v1'
EXT = HERE.parent / 'Lobby' / 'external' / 'gothicvania_church'
W, H = 2560, 360
FLOOR = 310
rng = np.random.default_rng(9)

# ---------- layout (single source of truth for art and colliders) ----------
SOLIDS = {
    'Ground_West': (0, FLOOR, 1360, H),
    'Stair_Step': (1360, 280, 1500, H),
    'Stair_Upper': (1500, 250, 1760, H),
    # 80 px over the upper floor. Player collider 45 px + jump 54 px = 99 px, so a jump there bonks and
    # nobody clears A2 (55 px); from the step (110 px clearance) the jump up to the upper floor still works.
    'Low_Ceiling': (1440, 0, 1800, 170),
    'Ground_East': (1760, FLOOR, W, H),
}
LAYOUT = {
    'ppu': 48, 'width': W, 'height': H,
    'solids': [{'name': k, 'x0': v[0], 'y0': v[1], 'x1': v[2], 'y1': v[3]} for k, v in SOLIDS.items()],
    'spawn': [120, FLOOR],
    # Learning order: lone A -> B with room to back off -> A in a cramped space -> A + B together.
    'enemies': [
        {'name': 'Enemy_A1_Monk', 'kind': 'A', 'x': 470, 'floor': FLOOR},
        {'name': 'Enemy_B1_Executioner', 'kind': 'B', 'x': 1000, 'floor': FLOOR},
        {'name': 'Enemy_A2_Monk', 'kind': 'A', 'x': 1630, 'floor': 250},
        {'name': 'Enemy_A3_Monk', 'kind': 'A', 'x': 2150, 'floor': FLOOR},
        {'name': 'Enemy_B2_Executioner', 'kind': 'B', 'x': 2380, 'floor': FLOOR},
    ],
    # Reserved, not interactive yet: Day 10 trap strip and Day 11 door.
    'trapDay10': [1120, 1300],
    'doorDay11': 2500,
}


# ---------- helpers ----------
def noise(scale, seed):
    g = np.random.default_rng(seed).random((H // scale + 2, W // scale + 2))
    img = Image.fromarray((g * 255).astype(np.uint8)).resize((W + scale * 2, H + scale * 2), Image.BICUBIC)
    return np.asarray(img, np.float32)[:H, :W] / 255


def fbm(seed):
    return .45 * noise(60, seed) + .3 * noise(20, seed + 1) + .25 * noise(5, seed + 2)


def load_ext(name, box, key=None):
    a = np.asarray(Image.open(EXT / name).convert('RGBA'), np.float32)[box[1]:box[3], box[0]:box[2]].copy()
    if key is not None:
        a[(np.abs(a[..., :3] - key).sum(-1) < 6), 3] = 0
    rgb = a[..., :3] / 255
    lum = rgb @ [.3, .59, .11]
    warm = (rgb[..., 0] > rgb[..., 2] + .12)       # flames/brass keep colour
    grey = lum[..., None] * np.array([1.0, .97, 1.02]) * 1.55
    rgb = np.where(warm[..., None], rgb * 1.15, grey * .8 + rgb * .2 * 1.55)
    return np.clip(rgb, 0, 1), a[..., 3] / 255, warm & (a[..., 3] > 0)


IMG = np.zeros((H, W, 3), np.float32)
EMIT = np.zeros((H, W, 3), np.float32)   # unlit glow added after lighting
LIGHTS = []


def paste(sprite, x, y, flip=False):
    rgb, alpha, warm = sprite
    if flip:
        rgb, alpha, warm = rgb[:, ::-1], alpha[:, ::-1], warm[:, ::-1]
    h, w = alpha.shape
    x0, y0 = max(x, 0), max(y, 0)
    x1, y1 = min(x + w, W), min(y + h, H)
    if x1 <= x0 or y1 <= y0:
        return
    sl = (slice(y0 - y, y1 - y), slice(x0 - x, x1 - x))
    a = alpha[sl][..., None]
    IMG[y0:y1, x0:x1] = IMG[y0:y1, x0:x1] * (1 - a) + rgb[sl] * a
    wm = (warm[sl] & (alpha[sl] > 0))[..., None]
    EMIT[y0:y1, x0:x1] += np.where(wm, rgb[sl] * .6, 0)


def rect(x0, y0, x1, y1, col):
    IMG[max(y0, 0):min(y1, H), max(x0, 0):min(x1, W)] = col


def light(x, y, r, strength=1.0, col=(1.0, .62, .3)):
    LIGHTS.append((x, y, r, strength, np.array(col)))


def flame(x, y):
    EMIT[y - 3:y, x:x + 1] += [1.0, .75, .3]
    EMIT[y - 2:y, x - 1:x + 2] += [.6, .25, .05]
    light(x, y - 2, 70, .55)


# ---------- 1. back walls per section ----------
yy, xx = np.mgrid[0:H, 0:W]
N = fbm(1)
stone = np.array([.5, .49, .52])
# masonry: 32x16 bricks, offset rows
row = yy // 16
mortar = ((yy % 16) == 0) | (((xx + (row % 2) * 16) % 32) == 0)
brick_tone = noise(16, 5) * .25 + .75
wall = stone * (brick_tone * (.55 + .45 * N))[..., None]
wall = np.where(mortar[..., None], wall * .55, wall)
IMG[:] = wall * .55

# Section 1 (0-640): outdoors. Night sky, distant cathedral, cemetery wall.
sky = np.linspace(.03, .11, H)[:, None, None] * np.array([.8, .82, 1.0])
IMG[:, :640] = sky
stars = (rng.random((H, 640)) > .9985) & (yy[:, :640] < 160)
IMG[:, :640][stars] = [.55, .55, .6]
# far cathedral silhouette (spires) on the right of section 1, it continues into section 2
sil = np.zeros((H, W), bool)
for cx, top, half in ((560, 40, 22), (640, 10, 30), (720, 50, 20)):
    for y in range(top, FLOOR):
        hw = half if y > top + 60 else max(1, int(half * (y - top) / 60))
        sil[y, cx - hw:cx + hw] = True
sil[120:FLOOR, 470:820] = True
sil &= xx < 640
IMG[sil] = [.05, .05, .07]
# lit windows in the silhouette
for wx in (540, 600):
    rect(wx, 170, wx + 6, 190, [.0, .0, .0])
    EMIT[172:190, wx + 1:wx + 5] += [.5, .3, .12]
# cemetery wall
rect(0, 250, 640, FLOOR, [.0, .0, .0])
IMG[250:FLOOR, :640] = wall[250:FLOOR, :640] * .4
IMG[248:252, :640] = [.22, .21, .23]

# Sections 2-4: interior wall with pillars and arches
inside = xx >= 640
IMG[inside] = wall[inside] * .5
for px in range(700, W, 160):
    pill = (xx >= px) & (xx < px + 28) & inside
    IMG[pill] = (stone * (.45 + .3 * N[..., None]))[pill] * .75
    IMG[(xx == px) & inside] *= 1.5
# cloister (640-1280): broken arch openings to the night outside
for ax in range(660, 1280, 160):
    cx = ax + 80 + 14
    inner = (np.abs(xx - cx) < 50) & (yy > 60) & (yy < FLOOR - 10)
    top = (yy - 110) ** 2 / 2500 + ((xx - cx) / 50) ** 2 < 1
    hole = inner & ((yy > 110) | top)
    IMG[hole] = sky[yy[hole], 0, 0][:, None] * np.array([.8, .82, 1.0]) * 1.4
# cathedral gate between the yard and the cloister
for gx in (612, 652):
    pill = (xx >= gx) & (xx < gx + 22) & (yy > 70) & (yy < FLOOR)
    IMG[pill] = (stone * (.5 + .3 * N[..., None]))[pill] * .8
    IMG[(xx == gx) & (yy > 70) & (yy < FLOOR)] *= 1.6
    rect(gx - 3, 64, gx + 25, 72, [.3, .29, .31])
# collapsed roof gap in the cloister (light comes in)
gap = (xx > 880) & (xx < 980) & (yy < 40 + 20 * N)
IMG[gap] = [.07, .07, .1]
light(930, 20, 220, .45, (.55, .6, .8))

# ---------- 2. pack props ----------
window = load_ext('backgrounds.png', (0, 0, 160, 192), key=(39, 38, 56))
column = load_ext('backgrounds.png', (176, 0, 304, 192), key=(39, 38, 56))
altar = load_ext('backgrounds.png', (320, 0, 448, 192), key=(39, 38, 56))
gargoyle = load_ext('backgrounds.png', (464, 0, 544, 192), key=(39, 38, 56))
sconce = load_ext('backgrounds.png', (560, 0, 624, 192), key=(39, 38, 56))
wall_candles = load_ext('tileset.png', (192, 16, 224, 80))
balustrade = load_ext('tileset.png', (192, 92, 272, 144))

paste(gargoyle, 300, FLOOR - 192)
paste(gargoyle, 40, FLOOR - 192, flip=True)
light(340, 220, 60, .25, (.5, .55, .7))
paste(window, 1300, FLOOR - 192 - 30)
light(1380, 140, 120, .35, (.55, .6, .8))
paste(column, 1820, FLOOR - 192)   # pack flames are already drawn in; only add their light
light(1884, 200, 90, .5)
paste(altar, 2200, FLOOR - 192)
light(2264, 200, 140, .6)
for sx in (760, 1080, 1980):
    paste(sconce, sx, 100)
    flame(sx + 30, 175)
    light(sx + 30, 172, 110, .7)
paste(wall_candles, 1560, 160)
light(1576, 190, 60, .4)
paste(balustrade, 2040, FLOOR - 52)

# graves in section 1
for gx, gh in ((110, 26), (190, 34), (250, 22), (420, 30), (520, 24)):
    rect(gx, FLOOR - gh, gx + 14, FLOOR, [.2, .2, .22])
    rect(gx + 1, FLOOR - gh, gx + 13, FLOOR - gh + 2, [.32, .31, .33])
    rect(gx + 6, FLOOR - gh - 10, gx + 8, FLOOR - gh, [.24, .24, .26])
    rect(gx + 3, FLOOR - gh - 7, gx + 11, FLOOR - gh - 5, [.24, .24, .26])
flame(196, FLOOR - 36); light(196, FLOOR - 40, 70, .5)

# Day 11 door (closed, not interactive yet)
door_x = LAYOUT['doorDay11']
rect(door_x - 26, 150, door_x + 26, FLOOR, [.09, .07, .06])
for dy in range(150, FLOOR, 12):
    rect(door_x - 26, dy, door_x + 26, dy + 1, [.05, .04, .04])
rect(door_x - 1, 150, door_x + 1, FLOOR, [.04, .03, .03])
arch = (np.abs(xx - door_x) < 34) & (yy > 118) & (yy < 156) & (((yy - 156) ** 2 / 1400 + ((xx - door_x) / 34) ** 2) < 1)
IMG[arch & ~((np.abs(xx - door_x) < 26) & (yy > 130))] = [.3, .29, .31]
rect(door_x + 14, 228, door_x + 20, 234, [.45, .36, .2])

# ---------- 3. terrain ----------
floor_tiles = [load_ext('tileset.png', b) for b in ((0, 168, 48, 208), (64, 167, 112, 208), (128, 167, 176, 208))]
block = load_ext('tileset.png', (16, 112, 48, 144))
ceiling_edge = load_ext('tileset.png', (48, 32, 80, 64))
FILL = np.array([.06, .055, .065])

for name, (x0, y0, x1, y1) in SOLIDS.items():
    if name == 'Low_Ceiling':
        rect(x0, y0, x1, y1, FILL)
        for tx in range(x0, x1, 32):
            paste(ceiling_edge, tx, y1 - 32)
        continue
    rect(x0, y0, x1, y1, FILL)
    i = 0
    for tx in range(x0, x1, 48):
        t = floor_tiles[(tx // 48 + i) % 3]
        paste(t, tx, y0 - 1)
        i += 1
    if y0 < FLOOR:  # raised block: stone face on the riser
        for ty in range(y0 + 32, H, 32):
            paste(block, x0, ty)
# graveyard: earth instead of flagstones
earth = (xx < 640) & (yy >= FLOOR + 6)
IMG[earth] = (np.array([.16, .14, .13]) * (.6 + .5 * N[..., None]))[earth] * .6
IMG[FLOOR:FLOOR + 6, :640] = [.2, .19, .18]
IMG[FLOOR - 2:FLOOR, :640][rng.random((2, 640)) > .5] = [.14, .16, .12]  # grass tufts

# ---------- 4. corruption: flesh growths and slime ----------
FLESH = np.array([.42, .09, .08])
def flesh_blob(cx, cy, rx, ry, seed):
    n = noise(6, seed)
    d = ((xx - cx) / rx) ** 2 + ((yy - cy) / ry) ** 2 + (n - .5) * .9
    m = d < 1
    shade = np.clip(1.15 - d, .3, 1.2)[..., None]
    vein = (np.abs(noise(9, seed + 3) - .5) < .04)
    col = FLESH * shade
    col = np.where(vein[..., None], col * 1.6 + [.1, 0, 0], col)
    IMG[m] = col[m]
    hi = m & (np.roll(~m, 1, 0))
    IMG[hi] = FLESH * 1.8
# Growths stay attached to solids and never rise into the walk space more than a few pixels,
# so nothing non-solid reads as a ledge.
for cx, cy, rx, ry in ((1040, FLOOR + 16, 70, 20), (1460, 160, 40, 20), (1768, 280, 22, 32),
                       (2060, -10, 110, 80), (2520, FLOOR + 12, 40, 16), (900, -6, 70, 34)):
    flesh_blob(cx, cy, rx, ry, cx)
# slime drips from the low ceiling and the chapel arches
SLIME = np.array([.3, .42, .16])
for sx in range(1450, 1800, 23):
    length = int(rng.integers(3, 12))  # short: drips must not read as solid
    c = SOLIDS['Low_Ceiling'][3]
    IMG[c:c + length, sx:sx + 2] = SLIME
    IMG[c + length:c + 2 + length, sx - 1:sx + 3] = SLIME * 1.2
    EMIT[c + length, sx] += SLIME * .3
pool = (np.abs(xx - 1210) < 60) & (yy >= FLOOR - 2) & (yy < FLOOR + 2)
IMG[pool] = SLIME * .9   # Day 10 slime pool lives here

# ---------- 5. lighting ----------
lightmap = np.full((H, W, 3), .32, np.float32)
lightmap[:, :640] = .55          # moonlit yard
for x, y, r, s, col in LIGHTS:
    d2 = ((xx - x) ** 2 + (yy - y) ** 2) / (r * r)
    lightmap += (s * np.clip(1 - d2, 0, 1) ** 1.6)[..., None] * col
# darker toward the ceiling of the interior
lightmap[:, 640:] *= np.clip(.55 + yy[:, 640:] / H * .7, 0, 1.2)[..., None]
out = IMG * lightmap + EMIT
# ordered dither to a reduced level count (pixel-art banding, as in lobby v6)
bayer = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16 - .5
levels = 24
out = np.floor(np.clip(out, 0, 1) * levels + bayer[yy % 4, xx % 4][..., None] * .9 + .5) / levels
OUT.mkdir(exist_ok=True)
Image.fromarray((np.clip(out, 0, 1) * 255).astype(np.uint8)).save(OUT / 'stage1_bg.png')
(OUT / 'stage1_layout.json').write_text(json.dumps(LAYOUT, indent=1), encoding='utf-8')


# ---------- 6. NOT_FINAL enemy silhouettes ----------
def sprite(w, h):
    return np.zeros((h, w, 4), np.uint8)


def fill(s, x0, y0, x1, y1, col):
    s[y0:y1, x0:x1] = (*col, 255)


def outline(s, col=(8, 6, 8)):
    p = np.pad(s[..., 3] > 0, 1)   # pad: no wrap-around onto the opposite edge
    a = p[1:-1, 1:-1]
    edge = ~a & (p[:-2, 1:-1] | p[2:, 1:-1] | p[1:-1, :-2] | p[1:-1, 2:])
    s[edge] = (*col, 255)


# A: hunched monk, long tentacle arms reaching forward (faces right). Feet at the bottom row.
ROBE, ROBE_D, SKIN, EYE, TENT = (70, 62, 58), (44, 38, 38), (150, 140, 128), (200, 40, 30), (110, 34, 34)
a = sprite(48, 56)
for y in range(14, 56):                      # robe widens downward, leaning forward
    lean = int((56 - y) * .18)
    half = 7 + (y - 14) * 7 // 42
    fill(a, 22 - half + lean, y, 22 + half + lean, y + 1, ROBE if (y // 6) % 2 else ROBE_D)
for y in range(4, 18):                       # hood, pushed forward
    half = 6 if y > 7 else 3 + (y - 4)
    fill(a, 27 - half, y, 27 + half, y + 1, ROBE_D)
fill(a, 29, 10, 33, 15, SKIN); a[11, 31] = (*EYE, 255)
for i, y0 in enumerate((22, 27)):            # two long arms ending in tentacle fingers
    for x in range(28, 44):
        y = y0 + int(2 * np.sin(x * .5 + i))
        fill(a, x, y, x + 1, y + 2, TENT if x > 38 else ROBE)
    for k in range(3):
        fill(a, 43, y0 - 2 + k * 2, 47, y0 - 1 + k * 2, TENT)
outline(a)
Image.fromarray(a).save(OUT / 'enemy_a_monk_NOT_FINAL.png')

# B: tall executioner (~80 px), hooded, big scythe raised behind. Body centre at x48 so flipX mirrors cleanly.
METAL, LEATHER, HOOD, BLADE = (96, 94, 98), (62, 46, 38), (36, 30, 32), (170, 168, 172)
b = sprite(96, 80)
fill(b, 38, 50, 46, 80, LEATHER); fill(b, 50, 50, 58, 80, LEATHER)      # legs
fill(b, 36, 74, 47, 80, METAL); fill(b, 49, 74, 60, 80, METAL)          # boots
fill(b, 34, 22, 62, 52, LEATHER)                                        # torso
fill(b, 30, 22, 66, 30, METAL)                                          # pauldrons
fill(b, 36, 40, 60, 46, METAL)                                          # belt plate
for y in range(4, 24):                                                  # pointed hood
    half = 2 + (y - 4) * 8 // 20
    fill(b, 48 - half, y, 48 + half, y + 1, HOOD)
fill(b, 46, 14, 54, 16, (10, 8, 8)); b[15, 51] = (*EYE, 255)            # eye slit
fill(b, 60, 26, 66, 44, LEATHER)                                        # arm
for t in range(0, 57):                                                  # scythe shaft held across the body
    x, y = 34 + t * 36 // 56, 62 - t
    fill(b, x, y, x + 2, y + 2, (70, 52, 40))
for x in range(40, 72):                                                 # blade hooked back over the head
    y = 5 + int(((71 - x) / 31) ** 2 * 12)
    fill(b, x, y, x + 1, y + 3, BLADE)
outline(b)
Image.fromarray(b).save(OUT / 'enemy_b_executioner_NOT_FINAL.png')
print('saved', sorted(p.name for p in OUT.iterdir()))
