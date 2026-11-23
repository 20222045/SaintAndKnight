"""Day 9 first stage art v2: ruined, corrupted cathedral in three layers.

Outputs to ./v2/ (copy into Assets/SaintKnightDay09/Art/):
  stage1_far.png     1440x360  sky, moon, distant cathedral; parallax (seen through arches / roof gaps / the yard)
  stage1_bg.png      2560x360  walls, props, corruption, terrain; transparent where the far layer shows
  stage1_front.png   2560x360  dark foreground silhouettes drawn in front of the actors (edges only)
  stage1_layout.json solids / spawn / enemies in art pixels (x right, y down) - the Unity builder reads this

Terrain rectangles are identical to v1 (make_day09_art_v1.py); tests depend on them.
Tiles/props: GothicVania Church Pack (CC0, ../Lobby/external/gothicvania_church), desaturated like lobby v6.
"""
import json
from pathlib import Path

import numpy as np
from PIL import Image

HERE = Path(__file__).parent
OUT = HERE / 'v2'
EXT = HERE.parent / 'Lobby' / 'external' / 'gothicvania_church'
W, H, FLOOR = 2560, 360, 310
FAR_W = 1440
rng = np.random.default_rng(9)

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
    'ppu': 48, 'width': W, 'height': H, 'farWidth': FAR_W,
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
GATE = (612, 674)          # yard -> cloister
INSIDE = GATE[1]


# ---------------------------------------------------------------- helpers
def noise(scale, seed, w=W, h=H):
    g = np.random.default_rng(seed).random((h // scale + 2, w // scale + 2))
    img = Image.fromarray((g * 255).astype(np.uint8)).resize((w + scale * 2, h + scale * 2), Image.BICUBIC)
    return np.asarray(img, np.float32)[:h, :w] / 255


def fbm(seed, w=W, h=H):
    return .45 * noise(60, seed, w, h) + .3 * noise(20, seed + 1, w, h) + .25 * noise(5, seed + 2, w, h)


class Layer:
    def __init__(self, w, h):
        self.rgb = np.zeros((h, w, 3), np.float32)
        self.a = np.zeros((h, w), np.float32)
        self.w, self.h = w, h

    def put(self, mask, col, alpha=1.0):
        col = np.broadcast_to(np.asarray(col, np.float32), self.rgb.shape)[mask] if np.ndim(col) == 3 else col
        self.rgb[mask] = self.rgb[mask] * (1 - alpha) + np.asarray(col) * alpha
        self.a[mask] = np.maximum(self.a[mask], alpha)

    def rect(self, x0, y0, x1, y1, col, alpha=1.0):
        x0, y0, x1, y1 = max(int(x0), 0), max(int(y0), 0), min(int(x1), self.w), min(int(y1), self.h)
        if x1 <= x0 or y1 <= y0:
            return
        r = self.rgb[y0:y1, x0:x1]
        r[:] = r * (1 - alpha) + np.asarray(col, np.float32) * alpha
        self.a[y0:y1, x0:x1] = np.maximum(self.a[y0:y1, x0:x1], alpha)

    def px(self, x, y, col, alpha=1.0):
        self.rect(x, y, x + 1, y + 1, col, alpha)


def load_ext(name, box, key=None):
    a = np.asarray(Image.open(EXT / name).convert('RGBA'), np.float32)[box[1]:box[3], box[0]:box[2]].copy()
    if key is not None:
        a[(np.abs(a[..., :3] - key).sum(-1) < 6), 3] = 0
    rgb = a[..., :3] / 255
    lum = rgb @ [.3, .59, .11]
    warm = rgb[..., 0] > rgb[..., 2] + .12
    grey = lum[..., None] * np.array([1.0, .97, 1.02]) * 1.55
    rgb = np.where(warm[..., None], rgb * 1.15, grey * .8 + rgb * .2 * 1.55)
    return np.clip(rgb, 0, 1), a[..., 3] / 255, warm & (a[..., 3] > 0)


MAIN, FRONT = Layer(W, H), Layer(W, H)
EMIT = np.zeros((H, W, 3), np.float32)
LIGHTS = []
yy, xx = np.mgrid[0:H, 0:W]


def paste(layer, sprite, x, y, flip=False, glow=True):
    rgb, alpha, warm = sprite
    if flip:
        rgb, alpha, warm = rgb[:, ::-1], alpha[:, ::-1], warm[:, ::-1]
    h, w = alpha.shape
    x0, y0, x1, y1 = max(x, 0), max(y, 0), min(x + w, layer.w), min(y + h, layer.h)
    if x1 <= x0 or y1 <= y0:
        return
    sl = (slice(y0 - y, y1 - y), slice(x0 - x, x1 - x))
    a = alpha[sl][..., None]
    layer.rgb[y0:y1, x0:x1] = layer.rgb[y0:y1, x0:x1] * (1 - a) + rgb[sl] * a
    layer.a[y0:y1, x0:x1] = np.maximum(layer.a[y0:y1, x0:x1], alpha[sl])
    if glow and layer is MAIN:
        EMIT[y0:y1, x0:x1] += np.where(warm[sl][..., None], rgb[sl] * .6, 0)


def light(x, y, r, strength=1.0, col=(1.0, .62, .3)):
    LIGHTS.append((x, y, r, strength, np.array(col, np.float32)))


def flame(x, y, size=1):
    EMIT[y - 2 - size:y, x:x + 1] += [1.0, .78, .35]
    EMIT[y - 1 - size:y, x - 1:x + 2] += [.55, .22, .05]
    EMIT[y - 4 - size:y - 2 - size, x:x + 1] += [.5, .3, .1]
    light(x, y - 2, 46 + 14 * size, .5)


STONE = np.array([.52, .5, .53])
IRON = np.array([.2, .2, .22])
WOOD = np.array([.24, .17, .12])
FLESH = np.array([.46, .1, .09])
SLIME = np.array([.36, .5, .18])
BONE = np.array([.62, .59, .52])
CLOTH = np.array([.32, .07, .08])
GOLD = np.array([.55, .43, .2])

# ---------------------------------------------------------------- FAR layer (parallax)
FAR = Layer(FAR_W, H)
fy, fx = np.mgrid[0:H, 0:FAR_W]
sky = np.stack([np.linspace(.035, .12, H), np.linspace(.035, .12, H), np.linspace(.06, .2, H)], -1)
FAR.rgb[:] = sky[:, None, :]
FAR.a[:] = 1
moon = (160, 70, 20)
d = np.hypot(fx - moon[0], fy - moon[1])
FAR.rgb += (np.clip(1 - d / 140, 0, 1) ** 2 * .22)[..., None] * [.6, .65, .8]
disc = d < moon[2]
crater = noise(4, 31, FAR_W, H)
FAR.rgb[disc] = (np.array([.82, .82, .76]) * (.85 + .15 * crater[..., None]))[disc]
FAR.rgb[(d >= moon[2]) & (d < moon[2] + 1)] = [.5, .52, .58]
cloud = fbm(40, FAR_W, H)
band = np.exp(-((fy - 95 - 25 * noise(80, 41, FAR_W, H)) / 22) ** 2)
cl = np.clip((cloud - .45) * 3, 0, 1) * band
lit = np.clip(1 - d / 260, .15, 1)
FAR.rgb = FAR.rgb * (1 - cl[..., None] * .8) + (cl * lit)[..., None] * np.array([.32, .33, .4]) * .8
stars = (rng.random((H, FAR_W)) > .9975) & (fy < 170) & ~disc & (cl < .2)
FAR.rgb[stars] = [.6, .6, .66]
# far hills and tree line
ridge = 250 + 18 * noise(90, 51, FAR_W, 1)[0] + 6 * noise(12, 52, FAR_W, 1)[0]
FAR.rgb[fy > ridge[None, :]] = [.05, .055, .08]
trees = 268 + 10 * noise(6, 53, FAR_W, 1)[0] - 14 * (noise(3, 54, FAR_W, 1)[0] > .78)
FAR.rgb[fy > trees[None, :]] = [.03, .035, .05]
# distant cathedral (the one being entered), right of the yard view
sil = np.zeros((H, FAR_W), bool)
def spire(cx, top, base, half):
    for y in range(top, base):
        hw = max(1, int(half * min(1, (y - top) / 50)))
        sil[y, cx - hw:cx + hw] = True
sil[150:300, 420:760] = True
spire(470, 30, 300, 16); spire(700, 46, 300, 14); spire(585, 10, 160, 26)
for bx in range(440, 760, 40):
    sil[150 + (bx % 3) * 6:300, bx:bx + 6] = True       # buttresses
FAR.rgb[sil] = [.045, .045, .065]
rd = np.hypot(fx - 585, fy - 185)
rose = (rd < 12) & (np.cos(np.arctan2(fy - 185, fx - 585) * 6) > -.2) & (rd > 3)
FAR.rgb[rose & sil] = [.17, .09, .06]               # faint, traced, not a sun
for wx in (500, 540, 630, 670):
    FAR.rgb[(fx >= wx) & (fx < wx + 4) & (fy > 200) & (fy < 222)] = [.32, .19, .08]
fog = np.clip((fy - 230) / 80, 0, 1) * .12
FAR.rgb = FAR.rgb * (1 - fog[..., None]) + fog[..., None] * np.array([.3, .32, .4])

# ---------------------------------------------------------------- MAIN: walls
N = fbm(1)
def ashlar(bw, bh, seed):
    row = yy // bh
    off = (row % 2) * (bw // 2)
    col = (xx + off) // bw
    tone = (np.sin(col * 12.9898 + row * 78.233 + seed) * 43758.5453) % 1
    lx, ly = (xx + off) % bw, yy % bh
    v = (.72 + .28 * tone) * (.75 + .25 * N)
    v = np.where(ly == 1, v * 1.25, v)               # top edge catches light
    v = np.where(lx == 1, v * 1.12, v)
    v = np.where(ly == bh - 1, v * .7, v)            # lower edge shadow
    v = np.where((ly == 0) | (lx == 0), v * .45, v)  # mortar
    return v

inside = xx >= INSIDE
wall_v = ashlar(32, 16, 1)
base_v = ashlar(48, 24, 7)
v = np.where(yy > FLOOR - 28, base_v * .8, wall_v)
v *= np.clip(.45 + yy / H * .75, .45, 1.1)          # grime: darker toward the vault
cracks = np.abs(noise(14, 12) - .5) < .012
v = np.where(cracks & (noise(40, 13) > .55), v * .5, v)
MAIN.put(inside, (STONE * v[..., None]) * .62)
moss = inside & (yy > FLOOR - 40) & (noise(8, 14) > .62)
MAIN.put(moss, np.array([.16, .2, .12]) * (.6 + .4 * N[..., None]), .7)
# cornice and vault ribs
MAIN.rect(INSIDE, 52, W, 58, STONE * .5)
MAIN.rect(INSIDE, 52, W, 53, STONE * .8)
MAIN.put(inside & (yy < 52), (STONE * wall_v[..., None]) * .28)
pil_x = list(range(700, W, 160))
for px_ in pil_x:
    for cx in (px_ + 14,):
        for side in (-1, 1):                       # pointed rib from the capital to the vault apex
            t = np.linspace(0, 1, 80)
            rx = cx + side * (80 * t)
            ry = 52 - 46 * np.sin(t * np.pi / 2)
            for x_, y_ in zip(rx.astype(int), ry.astype(int)):
                MAIN.rect(x_, y_, x_ + 2, y_ + 2, STONE * .42)
for px_ in pil_x:                                  # pilasters with capital and plinth
    m = inside & (xx >= px_) & (xx < px_ + 28) & (yy >= 52) & (yy < FLOOR)
    MAIN.put(m, (STONE * (.5 + .3 * N[..., None])) * .78)
    MAIN.rect(px_, 52, px_ + 1, FLOOR, STONE * 1.0)
    MAIN.rect(px_ + 27, 52, px_ + 28, FLOOR, STONE * .3)
    MAIN.rect(px_ - 4, 58, px_ + 32, 66, STONE * .62)
    MAIN.rect(px_ - 4, 58, px_ + 32, 59, STONE * .95)
    MAIN.rect(px_ - 3, FLOOR - 14, px_ + 31, FLOOR, STONE * .55)

# cloister (INSIDE..1280): pointed arch openings onto the night (transparent -> far layer)
def pointed(cx, half, spring, apex, y_bottom):
    r = half * 1.6
    left = (xx - (cx + half - r)) ** 2 + (yy - spring) ** 2 < r * r
    right = (xx - (cx - half + r)) ** 2 + (yy - spring) ** 2 < r * r
    body = (np.abs(xx - cx) < half) & (yy >= spring) & (yy < y_bottom)
    return body | ((yy < spring) & (yy > apex) & left & right & (np.abs(xx - cx) < half))

openings = np.zeros((H, W), bool)
ARCHES = range(714, 1200, 160)                       # arch centres 780 / 940 / 1100
for ax in ARCHES:
    cx = ax + 66
    ring = pointed(cx, 54, 120, 66, FLOOR - 20)
    hole = pointed(cx, 46, 124, 76, FLOOR - 20)
    MAIN.put(ring & ~hole, (STONE * ashlar(10, 8, 3)[..., None]) * .7)
    sill = (np.abs(xx - cx) < 50) & (yy >= FLOOR - 22) & (yy < FLOOR - 16)
    MAIN.put(sill, STONE * .7)
    openings |= hole
# collapsed roof over the cloister
gap = (xx > 870) & (xx < 990) & (yy < 30 + 26 * noise(9, 60))
openings |= gap
MAIN.a[openings] = 0
MAIN.rgb[openings] = 0
for gx in range(872, 990, 7):                       # broken rafters across the gap
    if rng.random() < .5:
        MAIN.rect(gx, 0, gx + 3, int(10 + rng.integers(0, 30)), WOOD * .55)
light(930, 10, 230, .5, (.5, .56, .78))
for ax in ARCHES:
    light(ax + 66, 230, 70, .22, (.45, .52, .75))     # moon spill under each arch

# yard (x < gate): cemetery wall only; sky comes from the far layer
yard = xx < GATE[0]
wall_y = 248
MAIN.put(yard & (yy >= wall_y), (STONE * ashlar(24, 12, 9)[..., None]) * .38)
MAIN.rect(0, wall_y - 3, GATE[0], wall_y + 1, STONE * .62)
MAIN.rect(0, wall_y - 3, GATE[0], wall_y - 2, STONE * .9)
for sx in range(4, GATE[0], 9):                     # iron railing on the wall
    MAIN.rect(sx, wall_y - 18, sx + 1, wall_y - 3, IRON)
    MAIN.px(sx, wall_y - 19, IRON * 1.6)
MAIN.rect(0, wall_y - 15, GATE[0], wall_y - 14, IRON)
MAIN.put(yard & (yy >= wall_y) & (noise(6, 15) > .66), np.array([.14, .18, .11]), .8)

def dead_tree(x, y, length, angle, depth, layer, col):
    if depth == 0 or length < 3:
        return
    x2 = x + length * np.cos(angle)
    y2 = y - length * np.sin(angle)
    n = int(max(abs(x2 - x), abs(y2 - y))) + 1
    for i in range(n):
        t = i / n
        w = max(1, int(depth * .7))
        layer.rect(int(x + (x2 - x) * t), int(y + (y2 - y) * t), int(x + (x2 - x) * t) + w,
                   int(y + (y2 - y) * t) + w, col)
    for k in range(2 + (depth > 3)):
        dead_tree(x2, y2, length * (.62 + .2 * rng.random()), angle + rng.normal(0, .55),
                  depth - 1, layer, col)

dead_tree(520, wall_y, 34, np.pi / 2 + .08, 6, MAIN, np.array([.06, .055, .06]))
dead_tree(60, wall_y, 26, np.pi / 2 - .1, 5, MAIN, np.array([.07, .065, .07]))

# gate between yard and cloister
for gx in GATE:
    m = (np.abs(xx - gx) < 12) & (yy > 70) & (yy < FLOOR)
    MAIN.put(m, (STONE * (.5 + .3 * N[..., None])) * .82)
    MAIN.rect(gx - 12, 70, gx - 11, FLOOR, STONE * 1.1)
    MAIN.rect(gx - 15, 62, gx + 15, 72, STONE * .7)
    MAIN.rect(gx - 15, 62, gx + 15, 63, STONE)
garch = pointed((GATE[0] + GATE[1]) // 2, 34, 96, 44, 100) & ~pointed((GATE[0] + GATE[1]) // 2, 26, 100, 56, 100)
MAIN.put(garch, STONE * .66)

# ---------------------------------------------------------------- MAIN: pack props
window = load_ext('backgrounds.png', (0, 0, 160, 192), key=(39, 38, 56))
column = load_ext('backgrounds.png', (176, 0, 304, 192), key=(39, 38, 56))
altar = load_ext('backgrounds.png', (320, 0, 448, 192), key=(39, 38, 56))
gargoyle = load_ext('backgrounds.png', (464, 0, 544, 192), key=(39, 38, 56))
sconce = load_ext('backgrounds.png', (560, 0, 624, 192), key=(39, 38, 56))
wall_candles = load_ext('tileset.png', (192, 16, 224, 80))
balustrade = load_ext('tileset.png', (192, 92, 272, 144))
arch_door = load_ext('tileset.png', (288, 16, 320, 80))

paste(MAIN, gargoyle, 300, FLOOR - 192)
paste(MAIN, gargoyle, 2, FLOOR - 192, flip=True)
paste(MAIN, window, 1290, FLOOR - 222)
light(1370, 130, 110, .35, (.55, .6, .8))
paste(MAIN, column, 1820, FLOOR - 192, glow=True)
light(1884, 200, 90, .5)
paste(MAIN, altar, 2200, FLOOR - 192)
light(2264, 210, 140, .65)
for sx in (770, 1090, 1990):
    paste(MAIN, sconce, sx, 100)
    flame(sx + 30, 175)
    light(sx + 30, 172, 120, .7)
paste(MAIN, wall_candles, 1600, 180)
light(1616, 205, 60, .45)
paste(MAIN, balustrade, 2040, FLOOR - 52)

# rose window above the altar (stained glass, desaturated)
rc = (2264, 82, 30)
dr = np.hypot(xx - rc[0], yy - rc[1])
ang = np.arctan2(yy - rc[1], xx - rc[0])
glass = dr < rc[2]
MAIN.put((dr >= rc[2]) & (dr < rc[2] + 5), STONE * .7)
petal = (np.cos(ang * 8) > .2) & (dr > 9)
glass_col = np.where(petal[..., None], [.34, .15, .13], [.13, .14, .22])
MAIN.put(glass, glass_col * .4)
MAIN.put(glass & ((np.abs(np.cos(ang * 8)) < .08) | (np.abs(dr - 9) < 1)), IRON)
EMIT[glass] += (glass_col * .14)[glass]
light(2264, 140, 160, .35, (.7, .35, .3))

# ---------------------------------------------------------------- MAIN: hand props
def chain(layer, x, y0, length, col=IRON):
    for y in range(y0, y0 + length, 3):
        layer.rect(x, y, x + 2, y + 2, col * (1.6 if (y // 3) % 2 else 1))
    layer.rect(x - 1, y0 + length, x + 3, y0 + length + 1, col * 1.4)
    layer.rect(x + 2, y0 + length - 2, x + 3, y0 + length, col * 1.4)

def skull(layer, x, y, s=1.0):
    c = BONE * s
    layer.rect(x, y, x + 5, y + 4, c)
    layer.rect(x + 1, y + 4, x + 4, y + 5, c * .8)
    layer.px(x + 1, y + 1, [0, 0, 0]); layer.px(x + 3, y + 1, [0, 0, 0])

def bones(layer, x, y):
    for k in range(3):
        bx, by = x + int(rng.integers(-8, 8)), y + int(rng.integers(-1, 2))
        ln = int(rng.integers(4, 8))
        layer.rect(bx, by, bx + ln, by + 1, BONE * .75)
        layer.px(bx - 1, by, BONE * .9); layer.px(bx + ln, by, BONE * .9)

def cage(x, top):
    chain(MAIN, x + 7, 0, top)
    for bx in range(x, x + 17, 4):
        MAIN.rect(bx, top + 2, bx + 1, top + 26, IRON * 1.3)
    MAIN.rect(x - 1, top, x + 18, top + 3, IRON * 1.5)
    MAIN.rect(x - 1, top + 25, x + 18, top + 28, IRON * 1.5)
    skull(MAIN, x + 6, top + 19, .8)

def banner(x, y0, w, h):
    hem = (h + 5 * np.sin(np.arange(w) * 1.7) + rng.integers(-4, 4, w)).astype(int)
    for i in range(w):
        fold = .75 + .25 * np.cos(i / w * np.pi * 3)
        MAIN.rect(x + i, y0, x + i + 1, y0 + hem[i], CLOTH * fold)
    MAIN.rect(x - 2, y0 - 2, x + w + 2, y0, WOOD * .9)
    MAIN.rect(x, y0, x + w, y0 + 2, GOLD * .8)
    MAIN.rect(x + w // 2 - 1, y0 + 10, x + w // 2 + 1, y0 + 26, GOLD * .7)
    MAIN.rect(x + w // 2 - 5, y0 + 15, x + w // 2 + 5, y0 + 17, GOLD * .7)

def candles(x, y, n):
    for k in range(n):
        cx = x + k * 4 + int(rng.integers(0, 2))
        h = int(rng.integers(4, 12))
        MAIN.rect(cx, y - h, cx + 2, y, np.array([.7, .66, .55]))
        MAIN.rect(cx - 1, y - 1, cx + 3, y, np.array([.6, .56, .46]))
        if rng.random() < .75:
            flame(cx, y - h)

def pew(x, y, broken=False):
    MAIN.rect(x, y - 14, x + 46, y - 11, WOOD)
    MAIN.rect(x, y - 14, x + 46, y - 13, WOOD * 1.5)
    MAIN.rect(x + 2, y - 11, x + 4, y, WOOD * .7)
    MAIN.rect(x + 42, y - 11, x + 44, y, WOOD * .7)
    MAIN.rect(x, y - 24, x + 3, y - 11, WOOD * .8)
    if broken:                                   # snapped seat: a missing middle and a fallen plank
        MAIN.rect(x + 20, y - 14, x + 30, y - 11, STONE * .2)
        MAIN.rect(x + 22, y - 6, x + 34, y - 4, WOOD * .8)

def statue(x, y):
    MAIN.rect(x - 10, y - 12, x + 10, y, STONE * .6)
    MAIN.rect(x - 10, y - 12, x + 10, y - 11, STONE * .9)
    for k in range(48):
        half = 5 + k // 8
        MAIN.rect(x - half, y - 12 - 48 + k, x + half, y - 12 - 48 + k + 1, STONE * (.55 + .2 * (k % 6 == 0)))
    MAIN.rect(x - 7, y - 58, x + 7, y - 54, STONE * .7)           # shoulders, head broken off
    MAIN.rect(x + 6, y - 50, x + 9, y - 30, STONE * .62)           # arm with a cracked censer
    MAIN.rect(x - 1, y - 62, x + 2, y - 58, STONE * .5)

def cobweb(x, y, size, flip=1):
    for k in range(5):
        a = k / 4 * np.pi / 2
        for r in range(size):
            MAIN.px(x + flip * int(r * np.cos(a)), y + int(r * np.sin(a)), [.6, .6, .64], .35)
    for r in range(4, size, 5):
        for k in range(12):
            a = k / 11 * np.pi / 2
            MAIN.px(x + flip * int(r * np.cos(a)), y + int(r * np.sin(a)), [.6, .6, .64], .25)

for x in (742, 1060, 1990, 2380):
    chain(MAIN, x, 0, int(rng.integers(30, 70)))
cage(1180, 86)
banner(830, 70, 22, 70)
banner(2120, 66, 24, 80)
banner(2430, 66, 22, 64)
for px_ in pil_x:
    cobweb(px_ + 28, 66, 16)
    cobweb(px_, 66, 14, -1)
candles(1010, FLOOR, 4)
candles(2190, FLOOR, 5)
candles(2330, FLOOR, 3)
candles(1530, 250, 3)
pew(1960, FLOOR)
pew(2030, FLOOR, broken=True)
pew(2340, FLOOR, broken=True)
statue(1925, FLOOR)
for bx in (220, 690, 1150, 1700, 2075, 2470):
    floor_y = 250 if 1500 <= bx < 1760 else FLOOR
    bones(MAIN, bx, floor_y - 1)
    skull(MAIN, bx + 8, floor_y - 5)

# graves
for gx, gh, kind in ((110, 26, 0), (178, 34, 1), (250, 22, 0), (420, 30, 2), (548, 24, 1), (360, 18, 2)):
    c = STONE * .45
    if kind == 0:                                   # cross
        MAIN.rect(gx + 5, FLOOR - gh, gx + 8, FLOOR, c)
        MAIN.rect(gx + 1, FLOOR - gh + 6, gx + 12, FLOOR - gh + 9, c)
        MAIN.rect(gx + 5, FLOOR - gh, gx + 6, FLOOR, c * 1.5)
    elif kind == 1:                                 # rounded headstone
        top = (np.hypot(xx - (gx + 7), yy - (FLOOR - gh + 7)) < 7) | (
            (xx >= gx) & (xx < gx + 14) & (yy >= FLOOR - gh + 7) & (yy < FLOOR))
        MAIN.put(top, c)
        MAIN.rect(gx + 4, FLOOR - gh + 10, gx + 10, FLOOR - gh + 11, c * .5)
    else:                                           # tilted slab
        for k in range(gh):
            MAIN.rect(gx + k // 5, FLOOR - k, gx + 12 + k // 5, FLOOR - k + 1, c)
# lantern post in the yard
MAIN.rect(230, FLOOR - 70, 232, FLOOR, IRON * 1.2)
MAIN.rect(224, FLOOR - 72, 238, FLOOR - 70, IRON * 1.4)
MAIN.rect(226, FLOOR - 84, 236, FLOOR - 72, IRON)
MAIN.rect(228, FLOOR - 82, 234, FLOOR - 74, [.4, .28, .12])
EMIT[FLOOR - 82:FLOOR - 74, 228:234] += [.7, .45, .15]
light(231, FLOOR - 78, 110, .75)

# Day 11 door (closed, not interactive yet)
door_x = LAYOUT['doorDay11']
MAIN.rect(door_x - 28, 150, door_x + 28, FLOOR, WOOD * .5)
for dy in range(150, FLOOR, 12):
    MAIN.rect(door_x - 28, dy, door_x + 28, dy + 1, WOOD * .25)
for sx in (door_x - 22, door_x + 18):
    MAIN.rect(sx, 160, sx + 4, FLOOR - 6, IRON * 1.1)     # iron straps
MAIN.rect(door_x - 1, 150, door_x + 1, FLOOR, WOOD * .2)
darch = pointed(door_x, 36, 150, 110, 152) & ~pointed(door_x, 28, 152, 122, 152)
MAIN.put(darch, STONE * .72)
MAIN.put(pointed(door_x, 28, 152, 122, 152), WOOD * .45)
MAIN.rect(door_x + 10, 228, door_x + 15, 234, GOLD)
light(door_x, 240, 60, .2)

# ---------------------------------------------------------------- MAIN: terrain
floor_tiles = [load_ext('tileset.png', b) for b in ((0, 168, 48, 208), (64, 167, 112, 208), (128, 167, 176, 208))]
block = load_ext('tileset.png', (16, 112, 48, 144))
ceiling_edge = load_ext('tileset.png', (48, 32, 80, 64))
rubble = noise(4, 70)
for name, (x0, y0, x1, y1) in SOLIDS.items():
    m = (xx >= x0) & (xx < x1) & (yy >= y0) & (yy < y1)
    MAIN.put(m, (np.array([.07, .065, .075]) * (.7 + .6 * rubble[..., None])))
    if name == 'Low_Ceiling':
        for tx in range(x0, x1, 32):
            paste(MAIN, ceiling_edge, tx, y1 - 32)
        continue
    for i, tx in enumerate(range(x0, x1, 48)):
        paste(MAIN, floor_tiles[(tx // 48 + i) % 3], tx, y0 - 1)
    if y0 < FLOOR:
        for bx in range(x0, min(x0 + 64, x1), 32):
            for ty in range(y0 + 32, H, 32):
                paste(MAIN, block, bx, ty)
earth = (xx < GATE[0]) & (yy >= FLOOR + 4)
MAIN.put(earth, (np.array([.15, .13, .12]) * (.5 + .6 * N[..., None])) * .7)
MAIN.rect(0, FLOOR, GATE[0], FLOOR + 4, np.array([.19, .18, .17]))
path = (xx < GATE[0]) & (yy >= FLOOR) & (yy < FLOOR + 3) & ((xx // 14) % 2 == 0)
MAIN.put(path, STONE * .42)
tuft = (xx < GATE[0]) & (yy >= FLOOR - 3) & (yy < FLOOR) & (noise(2, 71) > .6)
MAIN.put(tuft, np.array([.13, .16, .1]))

# ---------------------------------------------------------------- MAIN: corruption
def flesh(cx, cy, rx, ry, seed):
    n = noise(5, seed)
    dist = ((xx - cx) / rx) ** 2 + ((yy - cy) / ry) ** 2 + (n - .5) * .9
    m = dist < 1
    if not m.any():
        return
    ys, xs = np.where(m)
    body = np.clip(1.2 - dist, .25, 1.0)
    # light from the upper left: a rim where the mass edge faces it
    edge_ul = m & ~np.roll(np.roll(m, 2, 0), 2, 1)
    lobe = noise(9, seed + 4)
    col = FLESH[None, None, :] * (body * (.6 + .6 * lobe))[..., None]
    crease = np.abs(noise(7, seed + 5) - .5) < .05
    col = np.where(crease[..., None], col * .45, col)
    vein = np.abs(noise(11, seed + 3) - .5) < .02
    col = np.where(vein[..., None], col * 1.5 + [.12, .02, 0], col)
    MAIN.put(m, col)
    MAIN.put(edge_ul, FLESH * 2.0)
    for _ in range(int(m.sum() / 400) + 2):         # glossy pustules
        k = int(rng.integers(0, len(xs)))
        MAIN.rect(xs[k], ys[k], xs[k] + 2, ys[k] + 2, FLESH * 1.6)
        MAIN.px(xs[k], ys[k], [.85, .55, .5])

def tendril(x, y, dx, dy, length, seed, width=4):
    r = np.random.default_rng(seed)
    px_, py_, ang = float(x), float(y), np.arctan2(dy, dx)
    for i in range(length):
        w = max(1, int(width * (1 - i / length) + .5))
        ang += r.normal(0, .12)
        px_ += np.cos(ang); py_ += np.sin(ang)
        MAIN.rect(px_ - w / 2, py_ - w / 2, px_ + w / 2 + 1, py_ + w / 2 + 1, FLESH * .75)
        MAIN.px(px_, py_ - w / 2, FLESH * 1.5)

flesh(1040, FLOOR + 14, 74, 20, 101)
tendril(990, FLOOR - 2, -1, -1.4, 90, 1); tendril(1090, FLOOR - 2, 1, -1.2, 70, 2)
flesh(1460, 166, 40, 18, 102)
tendril(1440, 172, -1, .3, 60, 3, 3)
flesh(1768, 282, 22, 30, 103)
flesh(2060, -14, 120, 82, 104)
tendril(1990, 52, -1, 1.6, 110, 4); tendril(2130, 60, 1, 1.5, 95, 5); tendril(2060, 66, .1, 1, 70, 6, 3)
flesh(2525, FLOOR + 10, 44, 16, 105)
tendril(2500, FLOOR - 3, -1, -1.6, 80, 7)
flesh(905, -8, 70, 30, 106)
# slime: drips from the low ceiling, pool in the Day 10 strip
c_low = SOLIDS['Low_Ceiling'][3]
for sx in range(1450, 1800, 19):
    ln = int(rng.integers(3, 12))
    MAIN.rect(sx, c_low, sx + 2, c_low + ln, SLIME * .8)
    MAIN.rect(sx - 1, c_low + ln, sx + 3, c_low + ln + 2, SLIME)
    MAIN.px(sx, c_low + ln, SLIME * 1.7)
t0, t1 = LAYOUT['trapDay10']
pool = (xx > t0 + 20) & (xx < t1 - 20) & (yy >= FLOOR - 2) & (yy < FLOOR + 3)
MAIN.put(pool, SLIME * (.75 + .3 * noise(3, 80)[..., None]))
for bx in range(t0 + 30, t1 - 30, 23):
    MAIN.rect(bx, FLOOR - 4, bx + 3, FLOOR - 2, SLIME * 1.2)
    MAIN.px(bx + 1, FLOOR - 4, SLIME * 2)
EMIT[pool] += SLIME * .12

# ---------------------------------------------------------------- FRONT: silhouettes at the edges
SIL = np.array([.025, .022, .03])
grass = (yy > FLOOR + 6 - 16 * (noise(2, 90) > .7) * (noise(30, 91) > .5)) & (xx < GATE[0] + 10)
FRONT.put(grass & (yy > FLOOR - 6), SIL)
for gx, gh in ((24, 46), (586, 38)):
    FRONT.rect(gx, FLOOR - gh + 12, gx + 5, H, SIL)
    FRONT.rect(gx - 7, FLOOR - gh + 20, gx + 12, FLOOR - gh + 25, SIL)
for x in (1290, 1905):                            # broken pillar stubs at section seams
    for y in range(FLOOR - 26, H):
        FRONT.rect(x - 9 - (y % 7 == 0), y, x + 9 + (y % 5 == 0), y + 1, SIL)
    FRONT.rect(x - 11, FLOOR - 30, x + 7, FLOOR - 26, SIL)
for x in range(INSIDE + 30, W, 97):              # foreground chains/roots from the top
    ln = int(rng.integers(8, 36))
    if rng.random() < .6:
        chain(FRONT, x, 0, ln, SIL * 3)
    else:
        for y in range(ln):
            FRONT.rect(x + int(3 * np.sin(y * .3)), y, x + 3 + int(3 * np.sin(y * .3)), y + 1, SIL)
rub = (yy > FLOOR + 14 + 10 * noise(6, 92)) & (xx >= INSIDE)
FRONT.put(rub, SIL)

# ---------------------------------------------------------------- lighting + output
def lightmap(w, x_off=0, ambient_yard=.62, ambient_in=.3):
    gy, gx = np.mgrid[0:H, 0:w]
    gx = gx + x_off
    lm = np.where(gx[..., None] < GATE[0], ambient_yard, ambient_in) * np.ones(3, np.float32)
    lm = lm * np.where(gx[..., None] < GATE[0], [.85, .9, 1.1], 1.0)
    for x, y, r, s, col in LIGHTS:
        d2 = ((gx - x) ** 2 + (gy - y) ** 2) / (r * r)
        lm = lm + (s * np.clip(1 - d2, 0, 1) ** 1.6)[..., None] * col
    inside_m = gx >= INSIDE
    lm[inside_m] *= np.clip(.55 + gy[inside_m] / H * .7, 0, 1.2)[..., None]
    return lm

BAYER = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16 - .5

def quantize(rgb, levels=28):
    h, w = rgb.shape[:2]
    gy, gx = np.mgrid[0:h, 0:w]
    return np.floor(np.clip(rgb, 0, 1) * levels + BAYER[gy % 4, gx % 4][..., None] * .9 + .5) / levels

def shafts(rgb):
    """Additive slanted light beams with dust: roof gap, arches, rose window."""
    add = np.zeros_like(rgb)
    for x0, top, slope, width, s, col in ((930, 0, .55, 60, .10, (.55, .6, .8)),
                                          (2264, 100, .2, 36, .08, (.7, .4, .3))):
        cx = x0 + (yy - top) * slope
        m = (yy > top) & (np.abs(xx - cx) < width / 2 + (yy - top) * .12) & (yy < FLOOR)
        fall = np.clip(1 - (yy - top) / (FLOOR - top), 0, 1) * .6 + .4
        add += (m * fall * s)[..., None] * np.array(col)
        dust = m & (rng.random((H, W)) > .996)
        add[dust] += np.array(col) * .35
    return rgb + add

main_rgb = shafts(MAIN.rgb * lightmap(W) + EMIT)
front_rgb = FRONT.rgb * lightmap(W) * .8
OUT.mkdir(exist_ok=True)
def save(rgb, a, name):
    img = np.concatenate([quantize(rgb), a[..., None]], -1)
    Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8), 'RGBA').save(OUT / name)
save(main_rgb, (MAIN.a > .5).astype(np.float32), 'stage1_bg.png')
save(front_rgb, (FRONT.a > .5).astype(np.float32), 'stage1_front.png')
save(FAR.rgb, FAR.a, 'stage1_far.png')
(OUT / 'stage1_layout.json').write_text(json.dumps(LAYOUT, indent=1), encoding='utf-8')

# preview: what a player sees, with the far layer at the camera-left parallax position
prev = Image.new('RGBA', (W, H))
far_img = Image.open(OUT / 'stage1_far.png')
for i in range(0, W, 640):          # approximate parallax per screen for the contact sheet
    shift = int(i * (FAR_W - 640) / (W - 640))
    prev.paste(far_img.crop((shift, 0, shift + 640, H)), (i, 0))
prev.alpha_composite(Image.open(OUT / 'stage1_bg.png'))
prev.alpha_composite(Image.open(OUT / 'stage1_front.png'))
prev.save(OUT / 'preview.png')
print('saved', sorted(p.name for p in OUT.iterdir()))
