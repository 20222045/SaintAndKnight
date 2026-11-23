"""Rose Window Madonna boss parts: heart shell, attack tentacles, floor tentacle, grab coil, slime (48 PPU).

The body frames come from RoseMadonna-v4/fit_to_game.py (the user's v4 full-body sprite fitted to the game).
The earlier cocoon bodies (v2/v3) and madonna*.py figure painters are superseded; the last generator that drew
them is kept at Logs/Day09/BeforeBoss/make_boss_sprites_v3.py. File names keep _NOT_FINAL so Unity GUIDs stay.
"""
from pathlib import Path

import sys

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).parent))
from flesh_tools import *  # noqa: E402,F401,F403  (palettes, Canvas, fronds, tube, volume)

HERE = Path(__file__).parent
OUT.mkdir(parents=True, exist_ok=True)


# ---------------------------------------------------------------- heart in the cocoon 48x48 (centre = heart)
def core(name, state, frame=0):
    c = Canvas(48, 48)
    r = np.hypot(c.x - 24, c.y - 24)
    if state == 'open':
        heart = r <= 8.5 + frame
        fronds(c, (r <= 13) & ~heart, 60, density=1.5)                                 # torn fibrous rim
        glow = np.clip(1 - r / 10, 0, 1)
        c.put(heart, np.stack([rgb(110, 16, 18), rgb(170, 28, 26), CORE, rgb(255, 140, 110)])[
            np.clip((glow * 3.4).astype(int), 0, 3)])
    else:
        shell = r <= 12                           # small next to a 130 px boss; the hurtbox stays larger
        fronds(c, shell, 61, density=1.5)
        plates = shell & (noise(48, 48, 6, 4) > .5)                                   # bone-pale plates
        shade(c, plates, [rgb(70, 62, 60), rgb(110, 100, 94), rgb(150, 140, 128)],
              np.clip(.8 - (c.x + c.y) / 70, 0, 1))
        if state == 'cracked':
            c.put(shell & ((np.abs(c.x - c.y) < 1) | (np.abs(c.x + c.y - 48) < 1) | (np.abs(c.y - 25) < 1)), CORE)
    c.outline()
    c.save(name)


core('boss_core_closed_f00', 'closed')
core('boss_core_cracked_f00', 'cracked')
core('boss_core_open_f00', 'open', 0)
core('boss_core_open_f01', 'open', 1)


# ---------------------------------------------------------------- tentacles (same fibres)
def tentacle_sprite(name, w, h, pts, w0, w1, seed, tip=None):
    c = Canvas(w, h)
    m, flow = tube(c, pts, w0, w1)
    fronds(c, m, seed, flow=flow, density=1.6)
    if tip is not None:
        x, y = int(pts[-1][0]), int(pts[-1][1])
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                c.px(x + dx, y + dy, tip)
    c.outline()
    c.save(name)


# high sweep 256x96, root at x0; strike band 38-62 px above the floor -> rows 33..57
tentacle_sprite('boss_sweep_strike_f00', 256, 96, [(4, 46), (60, 43), (140, 46), (226, 45)], 8, 2, 40)
t = np.linspace(0, 3.4 * np.pi, 16)
tentacle_sprite('boss_sweep_tell_f00', 256, 96,
                [(4, 52)] + [(32 + 15 * np.cos(a) * (1 - a / 13), 34 - 15 * np.sin(a) * (1 - a / 13)) for a in t],
                8, 3, 41, tip=rgb(255, 120, 90))
c = Canvas(32, 96)                                                 # floor tentacle (Day 10 shares it)
mound = ((c.x - 16) / 14) ** 2 + ((c.y - 95) / 8) ** 2 < 1
fronds(c, mound, 50, density=2)
c.outline(); c.save('boss_spike_bulge_f00')
tentacle_sprite('boss_spike_strike_f00', 32, 96, [(16, 95), (13, 72), (18, 50), (16, 29)], 7, 1.5, 51)
tentacle_sprite('boss_grab_reach_f00', 192, 32, [(4, 25), (60, 27), (120, 26), (152, 22), (160, 18)], 6, 2, 60,
                tip=rgb(190, 110, 255))
c = Canvas(48, 64)                                                 # coil drawn over the held player
coil = np.zeros((64, 48), bool)
coil_flow = np.zeros((64, 48, 2), np.float32)
for pts in ([(3, 58), (24, 52), (45, 48)], [(3, 44), (24, 38), (45, 34)], [(3, 30), (24, 24), (45, 20)],
            [(24, 63), (6, 56), (4, 46)]):
    m, f = tube(c, pts, 2.4, 1.6)
    coil |= m
    coil_flow[m] = f[m]
fronds(c, coil, 62, flow=coil_flow, density=2)
c.outline(); c.save('boss_grab_coil_f00')

# ---------------------------------------------------------------- slime
c = Canvas(12, 12)
ball = np.hypot(c.x - 6, c.y - 6) <= 5
shade(c, ball, SLIME, np.clip(1 - np.hypot(c.x - 4, c.y - 4) / 7, 0, 1))
c.outline(); c.save('boss_slime_ball_f00')
c = Canvas(64, 12)
pool = ((c.x - 32) / 31) ** 2 + ((c.y - 11) / 4) ** 2 < 1
shade(c, pool, SLIME, np.clip(.3 + .5 * noise(64, 12, 3, 70) - (c.y - 8) / 10, 0, 1), 0, 2)
for bx in (14, 30, 47):
    c.put((c.x - bx) ** 2 + (c.y - 8) ** 2 <= 2, SLIME[3])
c.outline(); c.save('boss_slime_puddle_f00')

# ---------------------------------------------------------------- preview: bodies 2x
files = ['boss_body_p1_f00', 'boss_body_p1_f01', 'boss_body_p2_f00', 'boss_body_dead_f00']
sheet = Image.new('RGBA', (4 * 260 + 4, 304), (30, 30, 36, 255))
for i, f in enumerate(files):
    sheet.alpha_composite(Image.open(OUT / f'{f}_NOT_FINAL.png'), (4 + i * 260, 2))
sheet.resize((sheet.width * 2, sheet.height * 2), Image.NEAREST).save(HERE / 'boss_v2_preview.png')
print('saved ->', OUT)
