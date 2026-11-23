"""Draw the Day 10 flesh wall at game resolution with a transparent canvas.

The wall and tendrils are drawn as pixel clusters. Grab composites reuse the
project's Day 6 player poses at their original resolution and layer the wall's
lip and tendrils in front. All cels share a bottom-center pivot and 48 PPU.
Run: python Docs/Design/Stage/make_flesh_wall.py
"""
from functools import lru_cache
from pathlib import Path

from PIL import Image, ImageDraw, ImageOps


ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "Assets/SaintKnightDay10/Art"
ATLAS = ROOT / "Assets/SaintKnightDay06/Resources/SaintKnightDay06"
W, H = 160, 112
INK = "#160d17"
DARK = "#2c1724"
RIB = ("#2c1724", "#492330", "#73383e", "#a25350", "#c77765")
DUSK = ("#201421", "#371f2c", "#55303b", "#79404b", "#9a5260")
BITE = ("#341726", "#5a2a3a", "#90434d", "#bb625d", "#d78770")
TELL = ("#27152b", "#502547", "#7f3c66", "#a65a91", "#df9ae4")
MAW = "#1f111c"
MAW_DARK = "#180f18"


def curve(points):
    """Trace a Catmull-Rom curve on the native pixel grid."""
    padded = [points[0], *points, points[-1]]
    for p0, p1, p2, p3 in zip(padded, padded[1:], padded[2:], padded[3:]):
        steps = max(abs(p2[0] - p1[0]), abs(p2[1] - p1[1]), 2)
        for step in range(steps):
            t = step / steps
            yield tuple(
                .5 * (2 * b + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * t**2
                      + (-a + 3 * b - 3 * c + d) * t**3)
                for a, b, c, d in zip(p0, p1, p2, p3)
            )
    yield points[-1]


def strokes(draw, points, fill, width=1):
    draw.line([(round(x), round(y)) for x, y in curve(points)], fill, width=width)


def cord(draw, points, root, tip, palette=RIB):
    """Paint the shadow, body, and upper-left bevel of a tapered tendon."""
    samples = list(curve(points))
    radii = [root + (tip - root) * i / (len(samples) - 1) for i in range(len(samples))]
    for color, scale, lift in zip(palette, (1, .83, .62, .33, .12), (0, -.02, -.17, -.39, -.51)):
        for (x, y), radius in zip(samples, radii):
            y += radius * lift
            x -= radius * .10
            r = radius * scale
            draw.ellipse((round(x - r), round(y - r), round(x + r), round(y + r)), fill=color)


def fleck(draw, x, y, color):
    draw.line(((x, y), (x + 1, y - 1), (x + 3, y - 1)), fill=color)


def wall_image(breath=0, gap=0, reaching=False):
    image = Image.new("RGBA", (W, H))
    draw = ImageDraw.Draw(image)
    top = 14 - breath

    # Grounded roots and the dark silhouette of the wall.
    cord(draw, [(76, 103), (60, 101), (48, 106), (31, 107), (23, 111)], 9, .7, DUSK)
    cord(draw, [(101, 107), (117, 102), (128, 107), (139, 111)], 8, .8, DUSK)
    cord(draw, [(101, 44), (117, 32), (120, 23), (130, 16)], 5, .3, DUSK)
    cord(draw, [(64, 57), (47, 46), (44, 32), (36, 26)], 5, .2, DUSK)
    outline = [(38, 110), (41, 99), (40, 92), (46, 83), (44, 75), (49, 67), (47, 57),
               (52, 50), (52, 41), (59, 34), (58, 26), (65, 24), (66, top + 4),
               (74, top + 6), (80, top), (88, top + 1), (93, top + 5), (100, top + 4),
               (102, 23), (110, 28), (111, 38), (118, 47), (118, 56), (123, 65),
               (120, 76), (125, 85), (123, 94), (130, 102), (133, 111), (118, 111),
               (110, 109), (104, 111), (56, 111), (49, 108), (45, 111)]
    draw.polygon(outline, fill=DARK, outline=INK)

    # Contour folds, with smaller branched fibres visible through the gaps.
    cords = [
        ([(67, 102), (58, 88), (57, 75), (59, 63), (65, 56), (67, 44), (65, 33), (71, 25)], 8, .5, DUSK),
        ([(49, 99), (53, 86), (49, 75), (55, 63), (54, 51), (62, 44)], 5, 1, DUSK),
        ([(112, 103), (115, 91), (111, 78), (114, 63), (108, 53), (108, 42), (101, 34)], 9, .6, DUSK),
        ([(121, 107), (120, 96), (125, 86), (117, 72)], 6, .6, DUSK),
        ([(95, 48), (103, 37), (97, 27), (96, top + 7)], 8, .6, RIB),
        ([(64, 51), (72, 43), (74, 36), (71, 29), (73, top + 8)], 6, .3, RIB),
        ([(85, 41), (88, 33), (85, 24), (84, top + 4)], 7, .5, BITE),
        ([(44, 110), (55, 104), (65, 103), (75, 95)], 7, .4, RIB),
        ([(123, 111), (112, 106), (101, 106), (91, 100)], 6, .7, RIB),
    ]
    for points, root, tip, palette in cords:
        cord(draw, points, root, tip, palette)

    # A narrow closed seam opens toward the approaching character.
    y = 46 - breath
    maw = [(83, y), (93, y + 4), (99 + gap // 3, 59), (100 + gap // 3, 76),
           (94, 95), (86, 104), (79 - gap // 2, 98), (78 - gap, 85),
           (80 - gap, 70), (77 - gap // 2, 60)]
    draw.polygon(maw, fill=MAW, outline=INK)
    draw.polygon([(86, y + 8), (94, 57), (95 + gap // 3, 75), (90, 91), (83, 94),
                  (82 - gap // 2, 82), (84 - gap // 2, 66)], fill=MAW_DARK)
    for x, y in [(60, 52), (67, 35), (77, 32), (99, 30), (105, 53), (112, 73), (63, 92),
                 (54, 81), (119, 95), (104, 104), (64, 103), (57, 67)]:
        fleck(draw, x, y, "#ad645d" if (x + y) % 2 else "#87515a")

    if reaching:
        for side in (-1, 1):
            for height, end in ((103, 58), (96, 50)):
                points = [(80 + side * 26, height - 8), (80 + side * 35, height - 1),
                          (80 + side * 44, height + 3), (80 + side * end, height)]
                cord(draw, points, 6 if height == 103 else 3.7, .5, TELL)
                x = 80 + side * end
                draw.point([(x, height - 2), (x + side * 2, height - 4)], fill=TELL[-1])
    return image


def lips(draw, breath=0, gap=0, foreground=True):
    y = 46 - breath
    left = [(81, y), (75 - gap // 2, 54), (75 - gap, 68), (73 - gap, 83),
            (77 - gap // 2, 97), (85, 104)]
    right = [(83, y), (93, y + 6), (100 + gap // 3, 61), (99 + gap // 3, 77),
             (94, 90), (87, 102)]
    if foreground:
        cord(draw, right, 3.8, 5.2, RIB)
        for x, ry in [(94 + gap // 3, 61), (95 + gap // 3, 68), (94 + gap // 3, 76), (91, 85)]:
            strokes(draw, [(x + 1, ry - 1), (x - 3, ry + 1), (x - 4, ry + 3)], "#ab605a")
    else:
        cord(draw, left, 2.6, 4.5, BITE)
        for x, ry in [(77 - gap // 2, 56), (74 - gap, 65), (73 - gap, 74), (73 - gap, 83)]:
            fleck(draw, x - 2, ry, "#cd826f")


@lru_cache(maxsize=2)
def player_atlas(name):
    return Image.open(ATLAS / f"{name}_motion.png").convert("RGBA")


def place_player(canvas, name, cell, x, feet, flip=False):
    atlas = player_atlas(name)
    columns = atlas.width // 128
    left, top = (cell % columns) * 128, (cell // columns) * 96
    pose = atlas.crop((left, top, left + 128, top + 96))
    if flip:
        pose = ImageOps.mirror(pose)
    # Transparent atlas border is cropped before compositing against the wall.
    l, t, r, b = pose.getbbox()
    canvas.alpha_composite(pose.crop((l, t, r, b)), (x - 64 + l, feet - b + t))


# x is the actor's atlas anchor, not the bounding-box center.
POSES = {
    "cecilia": (44, 47, 53, 51, 61),
    "adela": (54, 57, 63, 65, 71),
}
STAGES = (
    ("swallow_f00", 57, 111, 10),
    ("swallow_f01", 67, 109, 15),
    ("struggle_f00", 70, 108, 17),
    ("struggle_f01", 67, 110, 15),
    ("escape_f00", 42, 110, 9),
)


def composite(name, index):
    stage, x, feet, gap = STAGES[index]
    escaping = index == 4
    image = wall_image(2 if index == 2 else 1, gap)
    draw = ImageDraw.Draw(image)
    lips(draw, 2 if index == 2 else 1, gap, foreground=False)

    # Back arm shows the direction of the pull before the character overlaps the opening.
    if not escaping:
        cord(draw, [(89, 61), (73, 60), (x - 8, 69), (x - 13, 77), (x - 7, 81)],
             5.5, 1.1, DUSK)
    place_player(image, name, POSES[name][index], x, feet, flip=escaping)

    if escaping:
        cord(draw, [(86, 92), (72, 96), (65, 94), (62, 87)], 6, 1, BITE)
        cord(draw, [(88, 70), (78, 69), (71, 63), (70, 57)], 4.5, .5, RIB)
        for xx, yy in [(61, 79), (55, 87), (67, 64)]:
            fleck(draw, xx, yy, "#bd6d63")
    else:
        # Clinging folds obscure the lower body more in the deeper two phases.
        bend = (0, 3, 5, 2)[index]
        cord(draw, [(90, 103), (79, 106), (x - 7, 101 - bend), (x - 13, 91 - bend),
                    (x - 7, 85 - bend), (x + 1, 88 - bend)], 7.5, .8, BITE)
        cord(draw, [(100, 76), (87, 78), (x + 3, 84 - bend), (x - 4, 79 - bend),
                    (x - 4, 73 - bend), (x + 2, 70 - bend)], 6, .75, RIB)
        if index >= 1:
            cord(draw, [(101, 100), (86, 93), (x - 2, 92), (x - 7, 98), (x - 12, 101)],
                 8, .6, BITE)
    lips(draw, 2 if index == 2 else 1, gap)
    return f"flesh_wall_{name}_{stage}", image


def build():
    OUT.mkdir(parents=True, exist_ok=True)
    for index, (breath, gap) in enumerate(((0, 0), (2, 2), (1, 1))):
        image = wall_image(breath, gap)
        draw = ImageDraw.Draw(image)
        lips(draw, breath, gap, foreground=False)
        lips(draw, breath, gap)
        image.save(OUT / f"flesh_wall_idle_f{index:02d}.png")
    image = wall_image(2, 6, reaching=True)
    draw = ImageDraw.Draw(image)
    lips(draw, 2, 6, foreground=False)
    lips(draw, 2, 6)
    image.save(OUT / "flesh_wall_reach_f00.png")

    for name in POSES:
        for index in range(len(STAGES)):
            stem, image = composite(name, index)
            image.save(OUT / f"{stem}.png")


if __name__ == "__main__":
    build()
