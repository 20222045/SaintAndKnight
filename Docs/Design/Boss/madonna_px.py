"""The saint inside the cocoon as true pixel art at game resolution (256x300 cell, floor = bottom row).

No blur, no downscaling: every region is filled from a 4-5 colour ramp in hard cel bands (light from the upper
left, ordered dither only on band borders), outlined (darker outline, lighter on the lit side), and the face
features are placed pixel by pixel. Fully clothed: white coif, grey veil, dark habit, white collar, prayer hands.
"""
import numpy as np

W, H = 256, 300
CX = 128


def rgb(*c):
    return np.array(c, np.float32) / 255


SKIN = [rgb(120, 84, 96), rgb(170, 128, 134), rgb(208, 172, 168), rgb(232, 206, 196), rgb(248, 234, 224)]
COIF = [rgb(96, 96, 118), rgb(150, 150, 170), rgb(200, 200, 214), rgb(236, 236, 244)]
VEIL = [rgb(44, 44, 58), rgb(70, 70, 88), rgb(102, 102, 122), rgb(138, 138, 158), rgb(172, 172, 190)]
HABIT = [rgb(16, 15, 24), rgb(28, 27, 40), rgb(42, 40, 58), rgb(60, 58, 80)]
HAIR = [rgb(18, 12, 16), rgb(34, 24, 30), rgb(54, 38, 44), rgb(86, 62, 66)]
GOLD = [rgb(110, 80, 36), rgb(170, 132, 62), rgb(224, 190, 110)]
LASH = rgb(30, 16, 24)
IRIS = [rgb(70, 30, 40), rgb(116, 54, 62)]
EYEW = rgb(226, 220, 226)
LIP = [rgb(110, 40, 52), rgb(168, 82, 90), rgb(204, 118, 120)]
BLUSH = rgb(228, 160, 158)
BLOOD = [rgb(96, 14, 22), rgb(150, 28, 34)]
RED = [rgb(120, 14, 14), rgb(220, 40, 34), rgb(255, 176, 150)]
LIGHT = np.array([-.6, -.8], np.float32)          # direction toward the light (x, y)


class Px:
    def __init__(self):
        self.rgb = np.zeros((H, W, 3), np.float32)
        self.a = np.zeros((H, W), bool)
        self.y, self.x = np.mgrid[0:H, 0:W]

    # ---- hard masks
    def ell(self, cx, cy, rx, ry, taper=0.):
        dx, dy = (self.x - cx) / rx, (self.y - cy) / ry
        if taper:
            dx = dx / (1 - taper * np.clip(dy, 0, 1) ** 2)      # narrower toward the chin
        return dx * dx + dy * dy <= 1

    def poly(self, pts):
        """Even-odd point-in-polygon on pixel centres."""
        x, y = self.x + .5, self.y + .5
        inside = np.zeros((H, W), bool)
        n = len(pts)
        for i in range(n):
            x1, y1 = pts[i]
            x2, y2 = pts[(i + 1) % n]
            cond = (y1 > y) != (y2 > y)
            xi = x1 + (y - y1) * (x2 - x1) / ((y2 - y1) if y2 != y1 else 1e-6)
            inside ^= cond & (x < xi)
        return inside

    def line(self, pts, width=1.):
        m = np.zeros((H, W), bool)
        for (x1, y1), (x2, y2) in zip(pts, pts[1:]):
            steps = int(max(abs(x2 - x1), abs(y2 - y1)) * 2) + 1
            for t in np.linspace(0, 1, steps):
                px, py = x1 + (x2 - x1) * t, y1 + (y2 - y1) * t
                if width <= 1:
                    m[int(round(py)), int(round(px))] = True
                else:
                    m |= (self.x - px) ** 2 + (self.y - py) ** 2 <= (width / 2) ** 2
        return m

    # ---- shading
    @staticmethod
    def dist(mask, cap=14):
        d = np.zeros(mask.shape, np.float32)
        cur = mask.copy()
        for _ in range(cap):
            d += cur
            p = np.pad(cur, 1)
            cur = cur & p[:-2, 1:-1] & p[2:, 1:-1] & p[1:-1, :-2] & p[1:-1, 2:]
            if not cur.any():
                break
        return d

    def light_poly(self, mask, cap=14, flat=.0):
        """Light for an arbitrary shape: outward normal from the distance field, faded toward the interior."""
        d = self.dist(mask, cap)
        gy, gx = np.gradient(d)
        norm = np.hypot(gx, gy) + 1e-6
        nx, ny = -gx / norm, -gy / norm
        edge = np.clip(1 - d / (cap * .55), 0, 1)
        return np.clip(.55 + .5 * (nx * LIGHT[0] + ny * LIGHT[1]) * edge + flat, 0, 1)

    def light_ell(self, cx, cy, rx, ry):
        nx, ny = (self.x - cx) / rx, (self.y - cy) / ry
        r = np.hypot(nx, ny)
        nz = np.sqrt(np.clip(1 - r * r, 0, 1))
        lam = -.55 * nx * .6 * 1.6 - .55 * ny * .8 * 1.2 + .7 * nz
        return np.clip(lam, 0, 1)

    def fill(self, mask, ramp, light, bias=0., dither=True):
        k = len(ramp)
        v = np.clip(light + bias, 0, .999) * k
        idx = np.floor(v).astype(int)
        frac = v - idx
        dith = (frac < .1) & ((self.x + self.y) % 2 == 0) & dither   # checker only on a thin band border
        idx = np.clip(idx - dith, 0, k - 1)
        self.rgb[mask] = np.stack(ramp)[idx[mask]]
        self.a |= mask

    def edge(self, mask, dark, lit=None):
        """Inner outline of a region; the side facing the light gets a lighter line (selout)."""
        p = np.pad(mask, 1)
        rim = mask & ~(p[:-2, 1:-1] & p[2:, 1:-1] & p[1:-1, :-2] & p[1:-1, 2:])
        if lit is None:
            self.rgb[rim] = dark
            return
        up_left = mask & ~p[:-2, 1:-1] | mask & ~p[1:-1, :-2]
        self.rgb[rim] = dark
        self.rgb[rim & up_left] = lit

    def dot(self, pts, col):
        for x, y in pts:
            if 0 <= x < W and 0 <= y < H:
                self.rgb[y, x] = col
                self.a[y, x] = True


def madonna(phase=1, frame=0, dead=False):
    p = Px()
    b = frame
    dy = 12 if dead else -b
    hx = CX - (3 if dead else 0)
    fy = 89 + dy                                          # face centre

    # halo: a 2 px gold ring, highlight on the upper left; broken in phase 2; gone when defeated
    if not dead:
        r = np.hypot(p.x - CX, p.y - (80 - b))
        ring = (r >= 33.5) & (r < 35.5)
        if phase == 2:
            ring &= np.cos(np.arctan2(p.y - 80 + b, p.x - CX) * 5 + .7) > -.3
        lit = np.clip(.5 - (p.x - CX) / 70 - (p.y - 80) / 70, 0, 1)
        p.fill(ring, GOLD, lit)

    # veil
    veil = p.poly([(hx, 54 + dy), (hx + 20, 57 + dy), (hx + 34, 70 + dy), (hx + 40, 96 + dy), (hx + 44, 128 + dy * .5),
                   (CX + 46, 160), (CX + 26, 166), (CX + 22, 150), (CX - 22, 150), (CX - 26, 166), (CX - 46, 160),
                   (hx - 44, 128 + dy * .5), (hx - 40, 96 + dy), (hx - 34, 70 + dy), (hx - 20, 57 + dy)])
    p.fill(veil, VEIL, p.light_poly(veil, 16, .05), dither=False)
    for sx, pts in ((-1, [(hx - 12, 62 + dy), (hx - 24, 96 + dy), (hx - 30, 130), (hx - 34, 158)]),
                    (-1, [(hx - 26, 74 + dy), (hx - 36, 110 + dy), (hx - 40, 150)]),
                    (1, [(hx + 12, 62 + dy), (hx + 24, 96 + dy), (hx + 30, 130), (hx + 34, 158)]),
                    (1, [(hx + 26, 74 + dy), (hx + 36, 110 + dy), (hx + 40, 150)])):
        fold = p.line(pts) & veil
        p.rgb[fold] = VEIL[1] if sx > 0 else VEIL[2]                   # fold shadows
    inner = p.ell(hx, fy + 2, 22, 27) & veil                          # the veil's dark inside, around the face
    p.rgb[inner] = VEIL[0]
    p.edge(veil, VEIL[0] * .7, VEIL[2])

    # habit
    sh = 124 + dy // 2
    habit = p.poly([(CX, sh - 8), (CX - 20, sh - 5), (CX - 33, sh + 3), (CX - 40, sh + 18), (CX - 42, 232),
                    (CX + 42, 232), (CX + 40, sh + 18), (CX + 33, sh + 3), (CX + 20, sh - 5)])
    p.fill(habit, HABIT, p.light_poly(habit, 16, .02))
    sleeves = p.poly([(CX - 39, sh + 16), (CX - 29, sh + 60), (CX - 6, sh + 54), (CX - 15, sh + 22)]) | \
        p.poly([(CX + 39, sh + 16), (CX + 29, sh + 60), (CX + 6, sh + 54), (CX + 15, sh + 22)])
    p.fill(sleeves, HABIT, p.light_poly(sleeves, 8, .12))
    p.edge(sleeves, HABIT[0], HABIT[3])
    for x0 in (CX - 28, CX - 16, CX + 16, CX + 28):
        p.rgb[p.line([(x0, sh + 64), (x0 + (x0 - CX) * .08, 230)]) & habit] = HABIT[0]
    p.edge(habit, HABIT[0])
    collar = p.ell(CX, sh - 5, 15, 7) & (p.y > sh - 6)
    p.fill(collar, COIF, p.light_ell(CX, sh - 8, 15, 7) * .9 + .1)
    p.edge(collar, COIF[0], COIF[2])

    # neck, shadowed under the jaw
    neck = p.poly([(hx - 5, fy + 9), (hx + 5, fy + 9), (CX + 6, sh - 6), (CX - 6, sh - 6)])
    p.fill(neck, SKIN, np.clip(.45 - (p.x - CX) / 30, 0, 1), dither=False)
    p.rgb[neck & (p.y < fy + 20)] = SKIN[1]
    p.edge(neck, SKIN[0])

    # hair framing the face down to the jaw
    hair = (p.poly([(hx - 9, fy - 19), (hx - 17, fy - 13), (hx - 20, fy + 2), (hx - 18, fy + 18), (hx - 12, fy + 23),
                    (hx - 10, fy + 8)]) |
            p.poly([(hx + 9, fy - 19), (hx + 17, fy - 13), (hx + 20, fy + 2), (hx + 18, fy + 18), (hx + 12, fy + 23),
                    (hx + 10, fy + 8)]))
    p.fill(hair, HAIR, p.light_poly(hair, 5, -.05), dither=False)
    for s in (-1, 1):                                             # glossy strand highlights
        p.rgb[p.line([(hx + 15 * s, fy - 9), (hx + 17 * s, fy + 2), (hx + 15 * s, fy + 14)]) & hair] = HAIR[3]
        p.rgb[p.line([(hx + 12 * s, fy - 4), (hx + 13 * s, fy + 10)]) & hair] = HAIR[2]
    p.edge(hair, HAIR[0])

    # face: egg shape, ellipse shading (rounded), jaw edge drawn in shadow
    face = p.ell(hx, fy, 14, 17.5, taper=.4)
    nx, ny = (p.x - hx) / 14, (p.y - fy) / 17.5
    p.fill(face, SKIN, np.clip(.86 - .34 * np.clip(nx, 0, 1) ** 2 - .3 * np.clip(ny - .45, 0, 1), 0, 1), dither=False)
    p.edge(face, SKIN[1], SKIN[2])
    p.rgb[face & (p.y >= fy - 15) & (p.y <= fy - 12)] = SKIN[2]       # coif's shadow on the forehead
    # coif band across the forehead and temples
    coif = p.ell(hx, fy - 13, 17, 9) & ~p.ell(hx, fy - 5, 13, 8) & (p.y <= fy - 8)
    p.fill(coif, COIF, p.light_ell(hx - 3, fy - 17, 18, 10) * .9 + .15)
    p.edge(coif, COIF[0], COIF[3])

    # ---- features, pixel by pixel (eyes at hx -/+ 6 on row fy + 1; dx > 0 = toward the outer corner)
    ey = fy + 1
    HI = rgb(252, 244, 244)
    for side in (-1, 1):
        ex = hx + 6 * side
        o = lambda dx, dyy: (ex + dx * side, ey + dyy)
        if dead:
            p.dot([o(-3, 0), o(-2, 1), o(-1, 1), o(0, 1), o(1, 1), o(2, 0), o(3, 0)], LASH)
            p.dot([o(-2, -3), o(-1, -3), o(0, -3), o(1, -3), o(2, -2)], HAIR[2])
            continue
        iris = IRIS if phase == 1 else [RED[0], RED[1]]
        # upper lash line (thick at the outer corner) and a small outer flick
        p.dot([o(-3, -1), o(-2, -2), o(-1, -2), o(0, -2), o(1, -2), o(2, -2), o(3, -1), o(4, -1), o(4, -2),
               o(5, -2)], LASH)
        p.dot([o(2, -1), o(3, 0)], LASH)
        # eye: whites at the corners, a two-row iris, highlight
        p.dot([o(-2, -1), o(-2, 0)], EYEW)
        p.dot([o(-1, -1), o(0, -1), o(1, -1), o(-1, 0), o(0, 0), o(1, 0)], iris[0])
        p.dot([o(0, 0), o(1, 0)], iris[1])
        p.dot([o(2, 0)], EYEW)
        p.dot([o(-1, -1)], HI if phase == 1 else RED[2])
        p.dot([o(-2, 1), o(-1, 1), o(0, 1), o(1, 1), o(2, 1)], SKIN[2])           # lower lid
        if phase == 1:                                                            # downcast: heavy lid shadow
            p.dot([o(-2, -3), o(-1, -3), o(0, -3), o(1, -3), o(2, -3)], SKIN[2])
        # thin sorrowful brow, raised toward the centre
        p.dot([o(-2, -6), o(-1, -6), o(0, -5), o(1, -5), o(2, -5), o(3, -4)], HAIR[2])
        p.dot([o(1, 4), o(2, 4), o(3, 4), o(2, 5), o(3, 5)], BLUSH)                # cheek blush
    # nose: bridge shadow on the far side, nostril, tip highlight
    p.dot([(hx + 1, fy + 5), (hx + 1, fy + 6)], SKIN[2])
    p.dot([(hx - 1, fy + 7)], SKIN[1])
    p.dot([(hx, fy + 6)], SKIN[4])
    # mouth: small, lips parted (more in phase 2)
    my = fy + 11
    p.dot([(hx - 2, my), (hx - 1, my), (hx + 1, my), (hx + 2, my)], LIP[1])
    p.dot([(hx, my)], LIP[0]); p.dot([(hx - 3, my + 1), (hx + 3, my + 1)], LIP[0])
    p.dot([(hx - 2, my + 1), (hx - 1, my + 1), (hx, my + 1), (hx + 1, my + 1), (hx + 2, my + 1)],
          LIP[0] if (phase == 2 and not dead) else LIP[1])
    p.dot([(hx - 1, my + 2), (hx, my + 2), (hx + 1, my + 2)], LIP[2])
    if phase == 1 and not dead:                            # a single dark tear from her left eye
        p.dot([(hx - 7, ey + 2), (hx - 7, ey + 3), (hx - 8, ey + 5), (hx - 8, ey + 6)], BLOOD[1])
        p.dot([(hx - 8, ey + 8)], BLOOD[0])
    if phase == 2 and not dead:                            # corruption creeping up the neck and jaw
        vein = p.line([(hx - 4, fy + 26), (hx - 5, fy + 21), (hx - 7, fy + 17), (hx - 9, fy + 13)]) |             p.line([(hx + 4, fy + 26), (hx + 5, fy + 22), (hx + 7, fy + 18), (hx + 9, fy + 15), (hx + 11, fy + 11)])
        p.rgb[vein & p.a] = BLOOD[1]

    # hands pressed in prayer, fingertips up
    hy0 = 166 + int(dy * .6)
    palms = p.ell(CX - 3, hy0, 4.5, 11.5) | p.ell(CX + 3, hy0, 4.5, 11.5)
    p.fill(palms, SKIN, p.light_ell(CX - 2, hy0 - 3, 8, 12) * .9 + .1)
    p.edge(palms, SKIN[1], SKIN[3])
    p.dot([(CX, hy0 + y) for y in range(-10, 11)], SKIN[1])                       # line between the palms
    for k in range(3):                                                             # knuckle creases
        p.dot([(CX - 5, hy0 - 4 + 4 * k), (CX - 4, hy0 - 4 + 4 * k)], SKIN[2])
    cuffs = p.ell(CX, hy0 + 12, 8, 3)
    p.fill(cuffs, HABIT, np.full((H, W), .7, np.float32))

    col, mask = p.rgb, p.a
    if dead:                                               # drained of colour
        lum = col @ np.array([.3, .59, .11], np.float32)
        col = col * .45 + lum[..., None] * np.array([.55, .52, .5], np.float32)
    return col, mask
