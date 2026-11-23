"""Fit the user's v4 full-body Rose Window Madonna to the game (2026-11-10, user approved).

Keeps her design (veil, spiked halo, long hair, rosary, lilies, gown) and adds what the fight needs:
- colour graded down to the game's dark, low-saturation range (halo and face keep a little light);
- phase 1: a mound of the cathedral's fibrous flesh swallowing the hem, the heart (weak point, drawn separately
  by Unity at 36 px above the floor) nested on top of it, roots crawling up the skirt and out along the floor -
  the tentacle attacks now visibly grow from her;
- phase 2: the flesh climbs to her waist and red veins spread over the dress (the v4 red halo stays);
- defeated: her kneeling sprite inside withered roots.
Reads source_*.png here, writes Assets/SaintKnightDay09/Art/Boss/boss_body_*_NOT_FINAL.png (256x300, floor = bottom row).
Run from anywhere: python Docs/Design/Boss/RoseMadonna-v4/fit_to_game.py
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))
from flesh_tools import FLESH, WITHER, OUT, Canvas, fronds, noise, rgb, tube, volume  # noqa: E402

CELL = (256, 300)
CX, FLOOR = 128, 299
HEART_Y = FLOOR - 36          # gameplay height of the heart: fixed, the attacks must reach it
STAND = 130                   # standing height (2026-11-10: 240 -> 130, about 2.2x the heroines like the user's reference)
K = STAND / 240               # the figure-relative positions below were measured on the 240 px sprite


def Y(y):
    """A height measured on the 240 px sprite, moved onto the current one (feet stay on the floor)."""
    return FLOOR - (FLOOR - y) * K


def X(dx):
    return CX + dx * K


def load_visible_art(name):
    with Image.open(HERE / name) as source:
        art = source.convert('RGBA')
    bounds = art.getchannel('A').point(lambda a: 255 if a >= 96 else 0).getbbox()
    return art.crop(bounds)


def to_cell(art, height, max_width=246, palette=None):
    """Same scaling/quantising as import_rose_madonna.py, returned as arrays (rgb 0..1, bool mask)."""
    scale = min(height / art.height, max_width / art.width)
    sprite = art.resize((round(art.width * scale), round(art.height * scale)), Image.Resampling.LANCZOS)
    alpha = sprite.getchannel('A').point(lambda v: 255 if v >= 112 else 0)
    quant = sprite.convert('RGB').quantize(colors=96, palette=palette, dither=Image.Dither.NONE)
    cell = Image.new('RGBA', CELL)
    cell.paste(quant.convert('RGBA'), ((CELL[0] - sprite.width) // 2, CELL[1] - sprite.height), alpha)
    a = np.asarray(cell, np.float32) / 255
    return a[..., :3].copy(), a[..., 3] > .5, quant


def grade(col, mask, phase, dead=False):
    """Pull the bright pinks/ivories down to the game's range; spare the halo and the face."""
    y, x = np.mgrid[0:CELL[1], 0:CELL[0]]
    lum = col @ np.array([.3, .59, .11], np.float32)
    cool = lum[..., None] * np.array([.92, .88, 1.0], np.float32)
    out = (col * .62 + cool * .38) * .8
    out = np.clip((out - .02) * 1.05, 0, 1)
    warm = (col[..., 0] > col[..., 1] + .08) & (col[..., 0] > .42)
    halo = mask & (y < Y(150)) & warm & (np.hypot(x - CX, y - Y(104)) > 14 * K)
    face = mask & (np.hypot((x - CX) / (9 * K), (y - Y(106)) / (11 * K)) < 1)
    keep = halo | face
    out[keep] = col[keep] * (.95 if not dead else .7)
    if dead:
        out = out * .82 + (out @ np.array([.3, .59, .11], np.float32))[..., None] * .18 * np.array([.9, .85, .85])
    return out


def roots(c, phase, frame, ramp, seed, dead=False):
    """The flesh she is rooted in: a mound over the hem with the heart nest on top, roots up the skirt and
    out along the floor. Phase 2 climbs to the waist."""
    W, H = CELL
    swell = frame
    top = FLOOR - 16 if dead else HEART_Y - 4 if phase == 1 else Y(212)
    mound = ((c.x - CX) / ((84 + 6 * phase) * K)) ** 2 + ((c.y - FLOOR) / (FLOOR - top + swell)) ** 2 \
        + (noise(W, H, 7, seed) - .5) * .5 < 1
    nest = (np.hypot(c.x - CX, c.y - HEART_Y) < 15 + swell) & (c.y < FLOOR) & (not dead)   # cradle for the heart
    m = (mound | nest) & (c.y <= FLOOR)
    flow = np.zeros((H, W, 2), np.float32)
    flow[..., 0] = np.clip((c.x - CX) / 70, -.9, .9)
    flow[..., 1] = -1.0                                                                   # fibres climb
    fronds(c, m, seed, flow=flow, ramp=ramp, density=1.3)
    volume(c, m, seed + 1)
    climb = FLOOR - 28 if dead else Y(214) if phase == 1 else Y(160)                     # how high the roots reach
    r = np.random.default_rng(seed + 2)
    for k, x0 in enumerate(X(dx) for dx in (-44, -26, -8, 12, 30, 48)):
        pts = [(x0, FLOOR - 10)]
        for step in range(5):
            y = FLOOR - 10 - (FLOOR - 10 - climb - r.integers(-6, 6)) * (step + 1) / 5
            pts.append((x0 + r.normal(0, 5) + (CX - x0) * .08 * step, y))
        m2, f2 = tube(c, pts, 2.2 if dead else 3.0 if phase == 1 else 3.8, 1.0, 14)
        fronds(c, m2, seed + 10 + k, flow=f2, ramp=ramp, density=1.6)
    for side in (-1, 1):                                                                 # out along the floor
        for k in range(2):
            x1 = X(side * (70 + 16 * k))
            pts = [(X(side * 40), FLOOR - 4), (x1, FLOOR - 3 - 3 * k), (x1 + side * (24 + 10 * k) * K, FLOOR - 1)]
            m3, f3 = tube(c, pts, 2.8, 1.0, 16)
            fronds(c, m3, seed + 30 + k + side, flow=f3, ramp=ramp, density=1.6)
    return m


def veins(c, body_mask, seed):
    """Phase 2: red veins spreading over the dress from the roots."""
    W, H = CELL
    v = body_mask & (c.y > Y(150)) & (np.abs(noise(W, H, 9, seed) - .5) < .012) & (noise(W, H, 18, seed + 1) > .45)
    c.rgb[v] = rgb(170, 40, 42)


def build(name, rgbcell, mask, phase, frame, dead=False):
    c = Canvas(*CELL)
    c.rgb[:] = grade(rgbcell, mask, phase, dead)
    c.a[:] = mask
    if dead:
        roots(c, 1, 0, WITHER, 300, dead=True)
        c.a &= ~((c.y > FLOOR - 2) & ~mask)
    else:
        if phase == 2:
            veins(c, mask.copy(), 210)
        roots(c, phase, frame, FLESH, 100 + phase)
    c.outline()
    c.save(name)


def main():
    for phase in (1, 2):
        art = load_visible_art(f'source_phase{phase}.png')
        col, mask, quant = to_cell(art, STAND, max_width=round(246 * K))
        build(f'boss_body_p{phase}_f00', col, mask, phase, 0)
        col, mask, _ = to_cell(art, STAND + 2, max_width=round(246 * K), palette=quant)
        build(f'boss_body_p{phase}_f01', col, mask, phase, 1)
    col, mask, _ = to_cell(load_visible_art('source_defeated.png'), round(124 * K), max_width=round(190 * K))
    build('boss_body_dead_f00', col, mask, 2, 0, dead=True)
    print('saved ->', OUT)


if __name__ == '__main__':
    main()
