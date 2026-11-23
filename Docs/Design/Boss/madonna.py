"""The saint inside the cocoon, painted at 4x and reduced to game pixels (soft dithered shading).

Coordinates are final pixels in the 256x300 boss cell (floor = bottom row). Light comes from the upper left.
Fully clothed: white coif, grey veil, dark habit with a white collar, hands pressed in prayer, cracked halo.
"""
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

S = 4
W, H = 256, 300
CX = 128.0
BAYER = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]], np.float32) / 16 - .5


def rgb(*c):
    return np.array(c, np.float32) / 255


SKIN = rgb(236, 216, 206)
SKIN_SHADOW = rgb(150, 112, 118)
COIF = rgb(226, 224, 230)
VEIL = rgb(150, 150, 164)
HABIT = rgb(46, 44, 58)
HAIR = rgb(40, 28, 32)
GOLD = rgb(214, 176, 96)
LIP = rgb(176, 92, 96)
IRIS = rgb(96, 44, 52)
LASH = rgb(34, 20, 26)


class Painter:
    def __init__(self):
        self.rgb = np.zeros((H * S, W * S, 3), np.float32)
        self.a = np.zeros((H * S, W * S), np.float32)
        self.yy, self.xx = np.mgrid[0:H * S, 0:W * S].astype(np.float32) / S

    # ---- masks (anti-aliased, 0..1)
    def _draw(self, fn):
        im = Image.new('L', (W * S, H * S), 0)
        fn(ImageDraw.Draw(im))
        return np.asarray(im, np.float32) / 255

    def poly(self, pts):
        return self._draw(lambda d: d.polygon([(x * S, y * S) for x, y in pts], fill=255))

    def ellipse(self, cx, cy, rx, ry, angle=0., taper=0.):
        t = np.linspace(0, 2 * np.pi, 96)
        x = rx * np.cos(t) * (1 - taper * np.clip(np.sin(t), 0, 1) ** 2)   # taper narrows the lower half (jaw)
        y = ry * np.sin(t)
        ca, sa = np.cos(angle), np.sin(angle)
        return self.poly(list(zip(cx + x * ca - y * sa, cy + x * sa + y * ca)))

    def line(self, pts, width):
        return self._draw(lambda d: d.line([(x * S, y * S) for x, y in pts], fill=255, width=max(1, int(width * S)),
                                           joint='curve'))

    def ring(self, cx, cy, r, width):
        return self._draw(lambda d: d.ellipse([(cx - r) * S, (cy - r) * S, (cx + r) * S, (cy + r) * S],
                                              outline=255, width=int(width * S)))

    # ---- shading
    @staticmethod
    def blur(a, r):
        return np.asarray(Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))
                          .filter(ImageFilter.GaussianBlur(r)), np.float32) / 255

    def lit(self, mask, radius=6., depth=1., ambient=.62):
        """Soft form shading: blurred mask = roundness, offset difference = facing the upper-left light."""
        b = self.blur(mask, radius * S)
        k = int(radius * S * .7)
        ly, lx = -k, -int(k * .8)
        emboss = np.roll(b, (-ly, -lx), (0, 1)) - np.roll(b, (ly, lx), (0, 1))     # + on the lit side
        return np.clip(ambient + depth * (1.2 * emboss + .35 * (b - .5)), .25, 1.25)

    def paint(self, mask, col, shade=1., alpha=1.):
        a = np.clip(mask * alpha, 0, 1)[..., None]
        col = np.asarray(col, np.float32)
        col = col * (shade[..., None] if np.ndim(shade) == 2 else shade)
        self.rgb = self.rgb * (1 - a) + col * a
        self.a = np.maximum(self.a, a[..., 0])

    def tint(self, mask, col, strength):
        a = np.clip(mask * strength, 0, 1)[..., None]
        self.rgb = self.rgb * (1 - a) + np.asarray(col, np.float32) * a

    # ---- output: reduce to game pixels, limited levels with ordered dither
    def reduce(self, levels=16):
        pre = Image.fromarray((np.clip(self.rgb * self.a[..., None], 0, 1) * 255).astype(np.uint8))
        al = Image.fromarray((self.a * 255).astype(np.uint8))
        pre = np.asarray(pre.resize((W, H), Image.LANCZOS), np.float32) / 255
        al = np.asarray(al.resize((W, H), Image.LANCZOS), np.float32) / 255
        col = pre / np.maximum(al[..., None], 1e-3)
        y, x = np.mgrid[0:H, 0:W]
        col = np.floor(np.clip(col, 0, 1) * levels + BAYER[y % 4, x % 4][..., None] * .85 + .5) / levels
        return np.clip(col, 0, 1), al > .5


def madonna(phase=1, frame=0, dead=False):
    """Returns (rgb HxWx3, mask HxW) of the figure at final resolution."""
    p = Painter()
    b = frame * 1.0                                   # breathing: 1 px rise
    dy = 14 if dead else -b                           # slumped forward when defeated
    tilt = .22 if dead else .05                       # head tilt (radians, toward her right/our left)
    hx, hy = CX - (4 if dead else 0), 86 + dy

    def R(x, y):                                      # rotate a head-space point about the neck
        ox, oy = x - CX, y - 104
        ca, sa = np.cos(tilt), np.sin(tilt)
        return CX + ox * ca - oy * sa - (4 if dead else 0), 104 + ox * sa + oy * ca + dy

    def Rp(pts):
        return [R(x, y) for x, y in pts]

    # halo (cracked in phase 2, gone when defeated)
    if not dead:
        ring = p.ring(CX, 81 - b, 35, 2.4)
        if phase == 2:
            ang = np.arctan2(p.yy - (81 - b), p.xx - CX)
            ring *= (np.cos(ang * 5 + .7) > -.3)
        glow = p.blur(ring, 5 * S)
        p.paint(glow, GOLD * .55, alpha=.35)
        p.paint(ring, GOLD, p.lit(ring, 2, .8, .8))

    # veil behind: falls from the crown over the shoulders, folds catch the light
    veil = p.poly(Rp([(128, 56), (146, 58), (160, 68), (166, 88), (168, 112)]) +
                  [(174, 140 + dy * .3), (176, 162), (156, 168), (150, 150), (106, 150), (100, 168), (80, 162),
                   (82, 140 + dy * .3)] + Rp([(88, 112), (90, 88), (96, 68), (110, 58)]))
    folds = sum(p.line(Rp([(118 - 14 * i, 66), (112 - 18 * i, 110)]) + [(98 - 10 * i, 150)], 1.4)
                for i in range(2)) + sum(p.line(Rp([(138 + 14 * i, 66), (144 + 18 * i, 110)]) +
                                                [(160 + 10 * i, 150)], 1.4) for i in range(2))
    vshade = p.lit(veil, 8, 1.0, .66) * (1 - .3 * p.blur(folds, 1.5 * S))
    p.paint(veil, VEIL, vshade)

    # habit: rounded shoulders, sleeves folding in to the chest, a few deep folds
    sh = 124 + dy * .5
    habit = p.poly([(128, sh - 8), (108, sh - 5), (95, sh + 3), (88, sh + 18), (86, 230), (170, 230), (168, sh + 18),
                    (161, sh + 3), (148, sh - 5)])
    sleeves = p.poly([(89, sh + 16), (99, sh + 60), (122, sh + 54), (113, sh + 22)]) + \
        p.poly([(167, sh + 16), (157, sh + 60), (134, sh + 54), (143, sh + 22)])
    p.paint(habit, HABIT, p.lit(habit, 10, 1.0, .7))
    p.paint(np.clip(sleeves, 0, 1), HABIT * 1.12, p.lit(np.clip(sleeves, 0, 1), 6, 1.0, .72))
    for x0 in (100, 112, 146, 158):
        f = p.line([(x0, sh + 66), (x0 + (x0 - 128) * .08, 228)], 1.2)
        p.paint(f, HABIT * .55, alpha=.8)
    # white collar
    collar = p.ellipse(CX, sh - 5, 15, 7) * (p.yy > sh - 6)
    p.paint(collar, COIF, p.lit(collar, 3, .8, .78))

    # neck with the jaw's shadow
    neck = p.poly(Rp([(121, 100), (135, 100), (136, 118)]) + [(137, sh - 6), (119, sh - 6)] + Rp([(120, 118)]))
    p.paint(neck, SKIN * .9, p.lit(neck, 4, .8, .55))
    jaw_shadow = p.blur(p.ellipse(*R(128, 108), 12, 6), 2.5 * S) * neck
    p.tint(jaw_shadow, SKIN_SHADOW, .85)

    # coif framing the face, then the face (egg shape, narrower jaw)
    veil_in = p.blur(p.ellipse(*R(128, 92), 20, 24, tilt), 4 * S) * veil
    p.tint(veil_in, VEIL * .35, .6)                                   # shadowed inside of the veil
    for side in (-1, 1):                                              # dark hair framing the face to the jaw
        panel = p.poly(Rp([(128 + 9 * side, 70), (128 + 17 * side, 76), (128 + 19 * side, 92),
                           (128 + 17 * side, 108), (128 + 11 * side, 112), (128 + 10 * side, 96)]))
        p.paint(panel, HAIR, p.lit(panel, 4, .8, .75))
        sheen = p.line(Rp([(128 + 15 * side, 80), (128 + 16.5 * side, 92), (128 + 15 * side, 104)]), .8)
        p.paint(sheen, HAIR * 2.6, alpha=.5)
    face = p.ellipse(*R(128, 89), 14.2, 18.2, tilt, taper=.34)
    fshade = p.lit(face, 7, 1.0, .74)
    p.paint(face, SKIN, fshade)
    coif = p.ellipse(*R(128, 77), 17, 10, tilt) * (1 - p.ellipse(*R(128, 84), 13, 9, tilt))   # forehead band
    coif = np.clip(coif, 0, 1) * (p.yy < R(128, 82)[1])
    p.paint(coif, COIF, p.lit(coif, 4, .9, .78))
    # shadow cast by the coif on the forehead, cheek blush, eye sockets
    brow_shadow = p.blur(p.ellipse(*R(128, 76), 14, 4, tilt), 2 * S) * face
    p.tint(brow_shadow, SKIN_SHADOW, .55)
    for ex in (-7, 7):
        p.tint(p.blur(p.ellipse(*R(128 + ex, 88), 5, 3, tilt), 1.5 * S) * face, SKIN_SHADOW, .35)
        p.tint(p.blur(p.ellipse(*R(128 + ex * 1.15, 96), 4, 2.4, tilt), 2.2 * S) * face, rgb(230, 150, 150), .35)


    # eyes
    for side in (-1, 1):
        ex = 128 + 7 * side
        if dead:
            p.paint(p.line(Rp([(ex - 3.5, 89), (ex, 90), (ex + 3.5, 89)]), .9), LASH)
            continue
        if phase == 1:                                # heavy, downcast lids: sorrow
            iris = p.ellipse(*R(ex, 89.8), 2.5, 1.6, tilt)
            white = p.ellipse(*R(ex, 89.5), 4.0, 1.7, tilt)
            p.paint(white, rgb(236, 230, 232), alpha=.9)
            p.paint(iris, IRIS)
            lid = p.line(Rp([(ex - 4.4, 89.2), (ex - 1.5, 87.8), (ex + 1.5, 87.8), (ex + 4.4, 89.2)]), 1.35)
            p.paint(p.ellipse(*R(ex, 87.6), 4, 1.6, tilt) * face, SKIN * .86)      # lowered upper lid
        else:                                         # opened wide, burning red
            white = p.ellipse(*R(ex, 88.6), 3.8, 2.2, tilt)
            p.paint(white, rgb(240, 226, 226))
            iris = p.ellipse(*R(ex, 88.6), 2.2, 2.0, tilt)
            p.paint(p.blur(iris, 2 * S), rgb(255, 60, 40), alpha=.6)
            p.paint(iris, rgb(210, 30, 30))
            p.paint(p.ellipse(*R(ex - .6, 88), .7, .7), rgb(255, 210, 190))
            lid = p.line(Rp([(ex - 4, 88.6), (ex - 1.5, 86.6), (ex + 1.5, 86.6), (ex + 4, 88.6)]), 1.0)
        p.paint(lid, LASH)
        p.paint(p.line(Rp([(ex + 3.6 * side, 88.8), (ex + 5.2 * side, 87.6)]), .8), LASH)    # outer lash
        p.paint(p.line(Rp([(ex - 3, 91.3), (ex + 3, 91.3)]), .5), SKIN_SHADOW, alpha=.5)  # lower lid
        brow = p.line(Rp([(ex - 4 * side, 82.6 - .9), (ex, 82.8), (ex + 4 * side, 83.8)]), .9)  # inner end raised
        p.paint(brow, HAIR, alpha=.85)
    # nose and mouth
    p.paint(p.line(Rp([(129.4, 89), (129.8, 94), (130.2, 96.5)]), .8), SKIN_SHADOW, alpha=.55)
    p.paint(p.ellipse(*R(128, 97.8), 2.6, .9, tilt), SKIN_SHADOW, alpha=.5)
    p.paint(p.ellipse(*R(127.2, 95.6), 1, .8), rgb(255, 245, 240), alpha=.6)
    mouth_open = 0.6 if phase == 1 else 1.2
    p.paint(p.ellipse(*R(128, 102.2), 4.2, 1.2, tilt), LIP)
    p.paint(p.ellipse(*R(128, 103.6), 3.6, 1.3, tilt), LIP * 1.15)
    p.paint(p.line(Rp([(124.5, 102.8), (128, 103.2 + mouth_open), (131.5, 102.8)]), .8), rgb(70, 22, 30))
    if phase == 1 and not dead:                       # one dark tear
        p.paint(p.line(Rp([(121, 92), (120.6, 96), (121.2, 99)]), .9), rgb(120, 24, 30), alpha=.85)
    if phase == 2 and not dead:                       # corruption creeping up the neck
        for pts in ([(122, 118), (123, 110), (121, 104)], [(134, 118), (133, 112), (135, 106), (134, 100)]):
            p.paint(p.line(Rp(pts), .7), rgb(150, 30, 34), alpha=.8)

    # hands pressed in prayer, fingers up
    hy0 = 166 + dy * .6
    for side in (-1, 1):
        palm = p.ellipse(128 + 3.2 * side, hy0, 4.6, 12, -.1 * side)
        p.paint(palm, SKIN, p.lit(palm, 3, .9, .76))
    p.paint(p.line([(128, hy0 - 11), (128, hy0 + 10)], .6), SKIN_SHADOW, alpha=.7)
    for k in range(3):
        p.paint(p.line([(124 + k * .2, hy0 - 6 + k * 3.2), (126.2, hy0 - 6.4 + k * 3.2)], .5), SKIN_SHADOW, alpha=.5)
    p.paint(p.ellipse(128, hy0 + 11, 7, 3), HABIT * 1.1)                                 # cuffs

    col, mask = p.reduce()
    if dead:                                          # drained of colour
        lum = col @ np.array([.3, .59, .11], np.float32)
        col = col * .45 + lum[..., None] * np.array([.55, .52, .5], np.float32)
    return col, mask
