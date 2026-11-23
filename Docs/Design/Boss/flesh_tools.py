"""Shared pixel tools for the corrupted-flesh boss art: palettes, Canvas, shingled fibrous flesh (fronds),
tapered tubes (tentacles) and large-scale volume. Used by make_boss_sprites.py and RoseMadonna-v4/fit_to_game.py."""
from pathlib import Path

import numpy as np
from PIL import Image

OUT = Path(__file__).resolve().parents[3] / 'Assets' / 'SaintKnightDay09' / 'Art' / 'Boss'


def rgb(*c):
    return np.array(c, np.float32) / 255


# flesh ramp sampled from the reference mass (dark -> lit); figure colours are ours
FLESH = [rgb(20, 8, 11), rgb(36, 14, 18), rgb(52, 20, 28), rgb(66, 26, 36), rgb(84, 38, 44),
         rgb(102, 46, 52), rgb(122, 62, 62), rgb(160, 104, 96), rgb(198, 136, 110)]
WITHER = [rgb(18, 14, 14), rgb(30, 24, 24), rgb(42, 34, 33), rgb(54, 44, 42), rgb(66, 54, 50),
          rgb(78, 64, 58), rgb(92, 76, 68), rgb(110, 92, 82), rgb(126, 108, 94)]
SKIN = [rgb(96, 74, 80), rgb(140, 112, 116), rgb(184, 158, 156), rgb(214, 194, 188), rgb(232, 218, 210)]
VEIL = [rgb(58, 58, 66), rgb(92, 92, 102), rgb(132, 132, 144), rgb(170, 170, 180)]
HABIT = [rgb(20, 20, 28), rgb(32, 32, 44), rgb(48, 48, 62), rgb(66, 66, 80)]
HAIR = [rgb(22, 16, 20), rgb(38, 28, 32), rgb(56, 42, 44)]
GOLD = [rgb(96, 74, 40), rgb(146, 116, 62), rgb(196, 162, 92)]
OUTLINE = rgb(10, 5, 8)
CORE = rgb(220, 46, 38)
SLIME = [rgb(40, 54, 22), rgb(70, 92, 34), rgb(108, 138, 52), rgb(160, 190, 90)]


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

    def px(self, x, y, col):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.rgb[y, x] = col
            self.a[y, x] = True

    def ell(self, cx, cy, rx, ry):
        return ((self.x - cx) / rx) ** 2 + ((self.y - cy) / ry) ** 2 <= 1

    def outline(self):
        p = np.pad(self.a, 1)
        edge = ~self.a & (p[:-2, 1:-1] | p[2:, 1:-1] | p[1:-1, :-2] | p[1:-1, 2:])
        self.rgb[edge] = OUTLINE
        self.a |= edge

    def save(self, name):
        img = np.concatenate([np.clip(self.rgb, 0, 1), self.a[..., None].astype(np.float32)], -1)
        Image.fromarray((img * 255 + .5).astype(np.uint8), 'RGBA').save(OUT / f'{name}_NOT_FINAL.png')


def shade(c, mask, ramp, light, lo=0, hi=None):
    """Fill a mask from a ramp by a 0..1 light field (light from the upper left)."""
    hi = len(ramp) - 1 if hi is None else hi
    light = np.broadcast_to(np.asarray(light, np.float32), mask.shape)
    idx = np.clip(np.round(lo + light * (hi - lo)), lo, hi).astype(int)
    c.put(mask, np.stack(ramp)[idx])


def fronds(c, mask, seed, flow=None, ramp=FLESH, density=1.0, lift=0.):
    """Shingled fibrous flesh like the reference mass: overlapping feather/scale petals that hang along a flow
    field (down and outward by default), each dark at the root, lit toward the tip, with a bright upper rim.
    Painted top to bottom so lower petals overlap the tips above. density scales the petal count."""
    if not mask.any():
        return
    r = np.random.default_rng(seed)
    ys, xs = np.where(mask)
    cx = xs.mean()
    bundle = noise(c.w, c.h, 10, seed + 7)
    c.put(mask, np.stack(ramp)[np.clip((1 + 2 * bundle).astype(int), 0, 3)])           # shadowed bed
    n = int(len(xs) / 14 * density)
    pick = r.integers(0, len(xs), n)
    pal = np.stack(ramp)
    for k in pick[np.argsort(ys[pick])]:                                                 # top first
        x0, y0 = float(xs[k]), float(ys[k])
        if flow is None:
            dx, dy = float(np.clip((x0 - cx) / 55, -1, 1)), 1.0
        else:
            dx, dy = (float(v) for v in flow[int(y0), int(x0)])
        a = np.arctan2(dy, dx) + r.normal(0, .25)
        L = r.uniform(6, 13)
        Wd = r.uniform(2.6, 4.6)
        ca, sa = np.cos(a), np.sin(a)
        x1, y1 = max(int(x0 - 14), 0), max(int(y0 - 14), 0)
        x2, y2 = min(int(x0 + 15), c.w), min(int(y0 + 15), c.h)
        if x2 <= x1 or y2 <= y1:
            continue
        gx, gy = c.x[y1:y2, x1:x2] - x0, c.y[y1:y2, x1:x2] - y0
        u = gx * ca + gy * sa                      # along the petal (0 = root)
        v = -gx * sa + gy * ca                     # across
        half = Wd / 2 * np.clip(np.sin(np.pi * np.clip(u / L, 0, 1)), 0, 1) ** .6
        inside = (u >= 0) & (u <= L) & (np.abs(v) <= half) & mask[y1:y2, x1:x2]
        if not inside.any():
            continue
        upper = v * ca < 0                         # the side facing up/left catches light
        tone = (.18 + .5 * (u / L) - .32 * np.abs(v) / (Wd / 2) + .35 * (bundle[y1:y2, x1:x2] - .5)
                + .12 * np.where(upper, 1, -1) + lift)
        idx = np.clip((tone * (len(ramp) - 1)).astype(int) + 1, 1, len(ramp) - 2)
        ring = inside & (np.abs(np.abs(v) - half) < .9)
        rim = ring & upper & (u > L * .35)
        tip = inside & (u > L - 1.6) & (np.abs(v) < 1)
        reg = c.rgb[y1:y2, x1:x2]
        reg[inside] = pal[idx[inside]]
        reg[rim] = pal[np.clip(idx[rim] + 2, 0, len(ramp) - 1)]
        if r.random() < .35:
            reg[tip] = pal[-1]
        reg[ring & ~upper] = pal[0]                                                       # underside shadow


def tube(c, pts, w0, w1, steps=40):
    """Mask + tangent field for a tapered tube along a polyline."""
    m = np.zeros((c.h, c.w), bool)
    flow = np.zeros((c.h, c.w, 2), np.float32)
    pts = np.asarray(pts, np.float32)
    seg = np.concatenate([np.linspace(pts[i], pts[i + 1], steps) for i in range(len(pts) - 1)])
    for i, (px, py) in enumerate(seg):
        rr = w0 + (w1 - w0) * i / (len(seg) - 1)
        tvec = seg[min(i + 1, len(seg) - 1)] - seg[max(i - 1, 0)]
        disc = (c.x - px) ** 2 + (c.y - py) ** 2 <= rr * rr
        m |= disc
        flow[disc] = tvec / (np.linalg.norm(tvec) + 1e-6)
    return m, flow


def volume(c, mask, seed, aura=None):
    """Large-scale form on top of the fibres: bulging lobes catch light, the mass darkens toward its edges and
    (optionally) in a halo of shadow around the figure so she reads first."""
    from PIL import ImageFilter
    m = Image.fromarray((mask * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(9))
    inner = np.asarray(m, np.float32) / 255
    lobes = noise(c.w, c.h, 34, seed)
    f = .5 + .45 * np.clip(inner * 1.4, 0, 1) * (.55 + .7 * lobes)
    f += .18 * np.clip((60 - c.x) / 60, 0, 1) * inner                                  # light from the left
    if aura is not None:
        d = np.hypot(c.x - aura[0], (c.y - aura[1]) * .8)
        f *= np.clip(.45 + d / 70, .45, 1)
    c.rgb[mask] = np.clip(c.rgb[mask] * f[mask][:, None], 0, 1)
