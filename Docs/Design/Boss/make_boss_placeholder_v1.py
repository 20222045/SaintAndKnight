"""v1 (kept for history; v2 = make_boss_sprites.py). NOT_FINAL placeholder sprites for the Rose Window Madonna (BOSS_ROSE_MADONNA.md), game resolution, 48 PPU.

Writes Assets/SaintKnightDay09/Art/Boss/*_NOT_FINAL.png (+ a preview here). Replace with the user's sprites later;
cell sizes follow the sprite request (section 8). Every floor-based cell has the floor on its bottom row.
"""
from pathlib import Path

import numpy as np
from PIL import Image

HERE = Path(__file__).parent
OUT = HERE.parents[2] / 'Assets' / 'SaintKnightDay09' / 'Art' / 'Boss'
OUT.mkdir(parents=True, exist_ok=True)
rng = np.random.default_rng(22)

STONE = np.array([.5, .5, .53]); FLESH = np.array([.27, .06, .07]); FLESH_H = np.array([.48, .15, .14])
FLESH_D = np.array([.11, .025, .03]); VEIN = np.array([.58, .17, .15]); CORE = np.array([.85, .16, .12])
SLIME = np.array([.42, .56, .2]); OUTLINE = np.array([.05, .03, .04])


def noise(w, h, scale, seed):
    g = np.random.default_rng(seed).random((h // scale + 2, w // scale + 2))
    img = Image.fromarray((g * 255).astype(np.uint8)).resize((w + 2 * scale, h + 2 * scale), Image.BICUBIC)
    return np.asarray(img, np.float32)[:h, :w] / 255


class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.rgb = np.zeros((h, w, 3), np.float32)
        self.a = np.zeros((h, w), bool)
        self.y, self.x = np.mgrid[0:h, 0:w]

    def put(self, m, col):
        col = np.asarray(col, np.float32)
        self.rgb[m] = col[m] if col.ndim == 3 else col
        self.a |= m

    def outline(self):
        p = np.pad(self.a, 1)
        edge = ~self.a & (p[:-2, 1:-1] | p[2:, 1:-1] | p[1:-1, :-2] | p[1:-1, 2:])
        self.rgb[edge] = OUTLINE
        self.a |= edge

    def save(self, name, levels=20):
        q = np.floor(np.clip(self.rgb, 0, 1) * levels + .5) / levels
        img = np.concatenate([q, self.a[..., None].astype(np.float32)], -1)
        Image.fromarray((img * 255).astype(np.uint8), 'RGBA').save(OUT / f'{name}_NOT_FINAL.png')


def flesh(c, mask, seed, light=1.0):
    """Shaded flesh: lobes, creases, a top-left rim and bright veins."""
    lobe = noise(c.w, c.h, 9, seed)
    crease = np.abs(noise(c.w, c.h, 7, seed + 1) - .5) < .05
    vein = np.abs(noise(c.w, c.h, 13, seed + 2) - .5) < .022
    col = FLESH * (.65 + .7 * lobe[..., None]) * light
    col = np.where(crease[..., None], FLESH_D, col)
    col = np.where(vein[..., None], VEIN * light, col)
    p = np.pad(mask, 2)
    rim = mask & ~p[:-4, :-4][:mask.shape[0], :mask.shape[1]]
    c.put(mask, col)
    c.put(rim, FLESH_H * light)


def tentacle(c, pts, w0, w1, seed, light=1.0):
    """Thick tapered tentacle along a polyline of points."""
    m = np.zeros((c.h, c.w), bool)
    pts = np.asarray(pts, np.float32)
    seg = np.concatenate([np.linspace(pts[i], pts[i + 1], 40) for i in range(len(pts) - 1)])
    for i, (px, py) in enumerate(seg):
        r = w0 + (w1 - w0) * i / (len(seg) - 1)
        m |= (c.x - px) ** 2 + (c.y - py) ** 2 <= r * r
    flesh(c, m, seed, light)
    return m


# ---------------------------------------------------------------- body 192x256 (floor = bottom row)
ALTAR_TOP = 255 - 52


def statue(c, cracked=False, split=False):
    cx = 96
    robe = (c.y >= 108) & (c.y <= ALTAR_TOP) & (np.abs(c.x - cx) <= 12 + (c.y - 108) * 10 // 95)
    head = (c.x - cx) ** 2 + (c.y - 94) ** 2 <= 11 ** 2
    veil = (c.y >= 88) & (c.y <= 150) & (np.abs(c.x - cx) <= np.clip(13 + (c.y - 88) // 3, 13, 21)) & ~head
    body = robe | head | veil
    shade = .62 + .38 * np.clip((cx + 14 - c.x) / 28, 0, 1)      # light from the left
    folds = ((c.x - cx + 40) % 7 == 0) & robe & (c.y > 150)
    c.put(body, STONE[None, None, :] * shade[..., None] * .55)
    c.put(folds, STONE * .45)
    c.put((np.abs(c.x - cx) <= 7) & (np.abs(c.y - 134) <= 3), STONE * 1.05)   # clasped hands
    if cracked:
        crack = body & (np.abs(c.x - cx - 3 * np.sin(c.y / 6)) < 1)
        c.put(crack, OUTLINE)
    if split:   # face torn open, the heart's glow behind it
        slit = (np.abs(c.x - cx) <= (2 + np.clip(14 - np.abs(c.y - 112), 0, 14) // 3)) & (c.y > 84) & (c.y < 140)
        c.put(slit, CORE * (.6 + .4 * noise(c.w, c.h, 3, 9)[..., None]))


def body(name, frame, phase=1, dead=False):
    c = Canvas(192, 256)
    swell = .04 * frame
    # mass behind the statue reaching up toward the rose window
    n = noise(192, 256, 14, 3 + phase)
    halo = ((c.x - 96) / 70) ** 2 + ((c.y - 120) / 105) ** 2 + (n - .5) * .7 < (.85 + swell if not dead else .45)
    halo &= c.y <= ALTAR_TOP + 4
    flesh(c, halo, 10 + phase, .55 if not dead else .35)
    for side in (-1, 1):                                            # tendrils climbing the wall
        x0 = 96 + side * 40
        pts = [(x0, 160), (x0 + side * 22, 130), (x0 + side * 14, 104), (x0 + side * 38, 80),
               (x0 + side * 30, 56 - 8 * phase), (x0 + side * 50, 34), (x0 + side * 44, 14)]
        if not dead:
            tentacle(c, pts, 7, 2, 20 + side + frame, .8)
    statue(c, cracked=phase == 2 or dead, split=phase == 2 and not dead)
    # flesh smothering the statue's face and robe (phase 1 hides the face)
    if not dead:
        face = ((c.x - 96) / 10) ** 2 + ((c.y - 98) / (8 + frame)) ** 2 + (noise(192, 256, 5, 7) - .5) < .9
        if phase == 1:
            flesh(c, face, 31, .85)
        skirt = (c.y > 160) & (c.y <= ALTAR_TOP) & (np.abs(c.x - 96) < 30 + (c.y - 160) * .5) & \
            (noise(192, 256, 6, 32 + frame) > .55 - swell)
        flesh(c, skirt, 33, .8)
        for k in range(4):                                       # roots gripping the altar edge
            x = 60 + k * 24
            c.put((np.abs(c.x - x) <= 2) & (c.y >= ALTAR_TOP - 4) & (c.y <= ALTAR_TOP + 6), FLESH * .8)
    c.outline()
    c.save(name)


for f in range(2):
    body(f'boss_body_p1_f{f:02d}', f, 1)
    body(f'boss_body_p2_f{f:02d}', f, 2)
body('boss_body_dead_f00', 0, 2, dead=True)

# ---------------------------------------------------------------- heart shell 48x48 (centre = heart)
def core(name, state, frame=0):
    c = Canvas(48, 48)
    r = np.hypot(c.x - 24, c.y - 24)
    shell = r <= 17
    if state == 'open':
        heart = r <= 13 + frame
        c.put(heart, CORE * (.55 + .45 * np.clip(1 - r / 15, 0, 1))[..., None])
        c.put((r > 13 + frame) & (r <= 17) & (noise(48, 48, 3, 5) > .5), FLESH_D)   # torn shell rim
        c.put(r <= 4, np.array([1, .6, .5]))
    else:
        plates = noise(48, 48, 6, 4) > .5          # irregular bone-stone plates over flesh
        col = np.where(plates[..., None], STONE * .38, FLESH_D * 2.2)
        c.put(shell, col * (.8 + .4 * np.clip((30 - c.x) / 30, 0, 1))[..., None])
        if state == 'cracked':
            crack = shell & ((np.abs(c.x - c.y) < 1) | (np.abs(c.x + c.y - 48) < 1) | (np.abs(c.y - 24) < 1))
            c.put(crack, CORE)
    c.outline()
    c.save(name)


core('boss_core_closed_f00', 'closed')
core('boss_core_cracked_f00', 'cracked')
core('boss_core_open_f00', 'open', 0)
core('boss_core_open_f01', 'open', 1)

# ---------------------------------------------------------------- high sweep 256x96, root at x0, floor = bottom row
# Strike band: 38-62 px above the floor -> rows 33..57.
c = Canvas(256, 96)
tentacle(c, [(4, 46), (60, 44), (140, 46), (226, 45)], 7, 2, 40)
c.outline(); c.save('boss_sweep_strike_f00')
c = Canvas(256, 96)
t = np.linspace(0, 3.6 * np.pi, 14)
tentacle(c, [(4, 50)] + [(30 + 14 * np.cos(a) * (1 - a / 14), 34 - 14 * np.sin(a) * (1 - a / 14)) for a in t], 7, 3, 41)
c.put((c.x - 30) ** 2 + (c.y - 34) ** 2 <= 4, VEIN)          # glowing tip = tell
c.outline(); c.save('boss_sweep_tell_f00')

# ---------------------------------------------------------------- floor tentacle 32x96 (Day 10 shares this)
c = Canvas(32, 96)
mound = ((c.x - 16) / 13) ** 2 + ((c.y - 95) / 7) ** 2 < 1
flesh(c, mound, 50)
c.put(mound & (np.abs(c.x - 16 - 2 * np.sin(c.y)) < 1), OUTLINE)
c.outline(); c.save('boss_spike_bulge_f00')
c = Canvas(32, 96)
tentacle(c, [(16, 95), (14, 70), (18, 50), (16, 29)], 6, 1.5, 51)
c.outline(); c.save('boss_spike_strike_f00')

# ---------------------------------------------------------------- grab: floor crawl 192x32 and coil 48x64
c = Canvas(192, 32)
tentacle(c, [(4, 26), (60, 28), (120, 27), (150, 24), (158, 20)], 5, 2, 60)
c.put((c.x - 158) ** 2 + (c.y - 20) ** 2 <= 4, np.array([.75, .4, 1.0]))   # purple tip = grab tell
c.outline(); c.save('boss_grab_reach_f00')
c = Canvas(48, 64)
for k, y in enumerate((52, 38, 24)):
    tentacle(c, [(4, y + 6), (24, y), (44, y - 4)], 2.2, 1.6, 61 + k)   # thin bands: the heroine shows between
tentacle(c, [(24, 63), (6, 56), (4, 46)], 2.5, 1.5, 64)
c.outline(); c.save('boss_grab_coil_f00')

# ---------------------------------------------------------------- slime
c = Canvas(12, 12)
ball = np.hypot(c.x - 6, c.y - 6) <= 5
c.put(ball, SLIME); c.put(np.hypot(c.x - 4, c.y - 4) <= 1.2, SLIME * 1.8)
c.outline(); c.save('boss_slime_ball_f00')
c = Canvas(64, 12)
pool = ((c.x - 32) / 31) ** 2 + ((c.y - 11) / 4) ** 2 < 1
c.put(pool, SLIME * (.7 + .4 * noise(64, 12, 3, 70)[..., None]))
for bx in (14, 30, 47):
    c.put((c.x - bx) ** 2 + (c.y - 8) ** 2 <= 2, SLIME * 1.6)
c.outline(); c.save('boss_slime_puddle_f00')

# preview sheet
files = sorted(OUT.glob('*_NOT_FINAL.png'))
sheet = Image.new('RGBA', (1100, 300), (40, 40, 46, 255))
x = 4
for f in files:
    im = Image.open(f)
    if x + im.width > sheet.width:
        break
    sheet.alpha_composite(im, (x, 296 - im.height))
    x += im.width + 6
sheet.save(HERE / 'placeholder_preview.png')
print(len(files), 'sprites ->', OUT)
