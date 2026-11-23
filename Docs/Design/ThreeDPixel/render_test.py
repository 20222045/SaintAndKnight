"""3D -> pixel test render (2026-11-19), after Never's End's GDC 2026 approach:
a 3D figure rendered straight at sprite resolution (128x96 cell, ~55 px tall like the game's Cecilia),
three-step toon shading, 1 px outline, joints on whole pixels. A stand-in made of capsules, NOT Cecilia's
model (none exists): it only tests whether the technique sits next to the hand-drawn sprites.

Run:  python -I Docs/Design/ThreeDPixel/render_test.py
Out:  Docs/Design/ThreeDPixel/out/  (sheet, GIFs)
"""
import math
import os
import numpy as np
from PIL import Image

W, H = 128, 96
ROOT = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(ROOT, "out")
ATLAS = os.path.join(ROOT, "../../../Assets/SaintKnightDay06/Resources/SaintKnightDay06/cecilia_motion.png")

# material -> (shadow, mid, light), picked from the current Cecilia sprite
PAL = {
    1: ((96, 82, 104), (168, 156, 178), (226, 220, 232)),   # hair, lilac silver
    2: ((150, 120, 104), (214, 178, 156), (240, 214, 196)), # skin
    3: ((104, 92, 120), (178, 168, 194), (232, 228, 240)),  # dress
    4: ((120, 88, 40), (190, 150, 70), (236, 204, 120)),    # gold trim
    5: ((40, 30, 34), (74, 58, 60), (110, 90, 88)),         # boots
    6: ((70, 70, 80), (130, 130, 140), (200, 200, 206)),    # chain / flail
}
OUTLINE = (62, 46, 66)
BG = (40, 30, 35)


def capsule(p, a, b, r):
    pa, ba = p - a, b - a
    h = np.clip((pa @ ba) / (ba @ ba), 0, 1)[:, None]
    return np.linalg.norm(pa - ba * h, axis=1) - r


def cone(p, a, b, r1, r2):
    # rounded cone approximated by a capsule whose radius blends along the axis
    pa, ba = p - a, b - a
    h = np.clip((pa @ ba) / (ba @ ba), 0, 1)
    return np.linalg.norm(pa - ba * h[:, None], axis=1) - (r1 + (r2 - r1) * h)


def sphere(p, c, r):
    return np.linalg.norm(p - c, axis=1) - r


def V(x, y, z=0.0):
    return np.array([x, y, z], dtype=float)


def rot(v, ang):   # around x... bend a limb in the side plane (x,y), angle from straight down
    return V(math.sin(ang) * v, -math.cos(ang) * v)


def pose(kind, t):
    """Joint positions in pixels; figure faces +x, feet near y=0."""
    s = math.sin(2 * math.pi * t)
    c = math.cos(2 * math.pi * t)
    parts = []
    if kind == "run":
        bob, lean = 1.5 * abs(c), 3.0
        thigh = [0.65 * s, -0.65 * s]
        knee = [0.9 * max(0, -s) + .2, 0.9 * max(0, s) + .2]
        arm = [-0.8 * s, 0.8 * s]
        sway = -2.5 * s
    else:   # idle: breathing, hair drifting
        bob, lean = 0.6 * s, 0.4
        thigh, knee, arm = [0.08, -0.06], [0.05, 0.05], [0.12, -0.1]
        sway = 0.8 * s
    hip = V(0, 26 + bob)
    chest = hip + V(lean, 14)
    head = chest + V(lean * .4 + 1, 10)
    for i, zs in enumerate((2.6, -2.6)):        # legs: thigh 12, shin 12
        h = hip + V(0, 0, zs)
        k = h + rot(12, thigh[i])
        a = k + rot(12, thigh[i] - knee[i])
        parts += [(capsule, (h, k, 2.4), 3), (capsule, (k, a, 2.0), 5), (capsule, (a, a + V(3, 0), 1.6), 5)]
    hand = None
    for i, zs in enumerate((4.6, -4.6)):        # arms: 8 + 8
        sh = chest + V(0, -1, zs)
        e = sh + rot(8, arm[i])
        hd = e + rot(7.5, arm[i] + .6)
        parts += [(capsule, (sh, e, 2.2), 3), (capsule, (e, hd, 1.9), 2)]
        if zs > 0:
            hand = hd
    parts += [
        (cone, (chest + V(0, 1), hip + V(sway * .4 - 1, -15), 4.2, 8.0), 3),    # dress, flared to mid-shin
        (capsule, (chest + V(0, 2), head + V(0, -4), 1.6), 2),               # neck
        (sphere, (head, 5.4), 2),                                             # face
        (sphere, (head + V(-.9, .9), 5.9), 1),                                # hair cap (behind the face)
        (cone, (head + V(-2.5, 0), head + V(-7 - sway - lean * 1.5, -21 + lean), 5.0, 3.6), 1),   # long hair streaming back
    ]
    # chain from the near hand, swinging behind, ball at the end
    prev = hand
    for k in range(1, 6):
        swing = -0.5 - (0.25 * s if kind == "run" else 0.08 * s)
        nxt = hand + V(math.sin(swing) * 3.2 * k, -math.cos(swing) * 3.2 * k, 1)
        parts.append((capsule, (prev, nxt, 1.0), 6))
        prev = nxt
    parts.append((sphere, (prev, 3.0), 6))
    return parts


def scene_sdf(p, parts):
    d = np.full(len(p), 1e9)
    m = np.zeros(len(p), dtype=int)
    for fn, args, mat in parts:
        di = fn(p, *args)
        better = di < d
        d[better], m[better] = di[better], mat
    return d, m


def render(parts, yaw_deg=90.0, feet_row=92, cx=64):
    yaw = math.radians(yaw_deg)
    fwd = V(-math.cos(yaw), 0, -math.sin(yaw))   # yaw 90: side view of a figure facing +x; 0: from the front
    right = V(-fwd[2], 0, fwd[0])
    up = V(0, 1, 0)
    xs, ys = np.meshgrid(np.arange(W) + .5 - cx, feet_row - (np.arange(H) + .5))
    origin = xs.reshape(-1, 1) * right + ys.reshape(-1, 1) * up - fwd * 60
    dirv = np.tile(fwd, (len(origin), 1))
    t = np.zeros(len(origin))
    hit = np.zeros(len(origin), bool)
    mat = np.zeros(len(origin), int)
    for _ in range(90):
        p = origin + dirv * t[:, None]
        d, m = scene_sdf(p, parts)
        newly = (d < .05) & ~hit
        hit |= newly
        mat[newly] = m[newly]
        t = np.where(hit, t, t + np.maximum(d, .05))
        if (t > 140).all():
            break
    p = origin + dirv * t[:, None]
    e = .25
    n = np.stack([scene_sdf(p + V(*o), parts)[0] - scene_sdf(p - V(*o), parts)[0]
                  for o in ((e, 0, 0), (0, e, 0), (0, 0, e))], axis=1)
    n /= np.linalg.norm(n, axis=1, keepdims=True) + 1e-9
    light = V(.45, .75, .5)
    light /= np.linalg.norm(light)
    lam = np.clip(n @ light, 0, 1)
    band = np.where(lam > .62, 2, np.where(lam > .25, 1, 0))   # three steps
    img = np.zeros((H * W, 3), np.uint8)
    img[:] = BG
    for k, pal in PAL.items():
        for b in range(3):
            sel = hit & (mat == k) & (band == b)
            img[sel] = pal[b]
    img = img.reshape(H, W, 3)
    hit2, mat2, dep = hit.reshape(H, W), mat.reshape(H, W), t.reshape(H, W)
    # 1 px outline: silhouette edge, plus inner lines where depth jumps between parts
    out = img.copy()
    for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)):
        nh = np.roll(np.roll(hit2, dy, 0), dx, 1)
        nd = np.roll(np.roll(dep, dy, 0), dx, 1)
        nm = np.roll(np.roll(mat2, dy, 0), dx, 1)
        edge = hit2 & (~nh | ((nd - dep > 3) & (nm != mat2)))
        out[edge] = OUTLINE
    alpha = np.where(hit2, 255, 0).astype(np.uint8)
    return Image.fromarray(np.dstack([out, alpha]), "RGBA")


def atlas_cell(c):
    im = Image.open(ATLAS).convert("RGBA")
    cols = im.size[0] // 128
    return im.crop(((c % cols) * 128, (c // cols) * 96, (c % cols) * 128 + 128, (c // cols) * 96 + 96))


def on_bg(cell):
    bg = Image.new("RGBA", cell.size, BG + (255,))
    bg.alpha_composite(cell)
    return bg


def main():
    os.makedirs(OUT, exist_ok=True)
    idle = [render(pose("idle", i / 4)) for i in range(4)]
    run = [render(pose("run", i / 8)) for i in range(8)]
    angles = [render(pose("run", .125), yaw) for yaw in (90, 60, 30, 0)]
    have_atlas = os.path.exists(ATLAS)
    cur_idle = [atlas_cell(c) for c in range(0, 4)] if have_atlas else []
    cur_run = [atlas_cell(c) for c in range(4, 16, 3)] if have_atlas else []

    S = 3
    rows = [cur_idle + cur_run, idle + run[::2], angles]
    sheet = Image.new("RGBA", (128 * 8 * S, 96 * len(rows) * S), BG + (255,))
    for r, row in enumerate(rows):
        for i, cell in enumerate(row):
            sheet.alpha_composite(on_bg(cell).resize((128 * S, 96 * S), Image.NEAREST), (i * 128 * S, r * 96 * S))
    sheet.save(os.path.join(OUT, "compare_sheet.png"))

    # animated side by side: current run (12 cells) vs 3D run (8 frames), both on a ~70 ms beat
    frames = []
    for k in range(24):
        f = Image.new("RGBA", (256, 96), BG + (255,))
        if have_atlas:
            f.alpha_composite(atlas_cell(4 + k % 12), (0, 0))
        f.alpha_composite(run[k % 8], (128, 0))
        frames.append(f.resize((256 * S, 96 * S), Image.NEAREST).convert("RGB"))
    frames[0].save(os.path.join(OUT, "run_side_by_side.gif"), save_all=True, append_images=frames[1:], duration=70, loop=0)
    idle_frames = [on_bg(f).resize((128 * S, 96 * S), Image.NEAREST).convert("RGB") for f in idle]
    idle_frames[0].save(os.path.join(OUT, "idle_3d.gif"), save_all=True, append_images=idle_frames[1:], duration=260, loop=0)
    print("wrote", OUT)


if __name__ == "__main__":
    main()
