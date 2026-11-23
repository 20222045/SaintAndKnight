"""Draw the Day 10 floor tentacle and slime pool at native resolution (48 PPU)."""

from pathlib import Path

from PIL import Image, ImageDraw


OUT = Path(__file__).resolve().parents[3] / "Assets" / "SaintKnightDay10" / "Art"
TENTACLE_CELL = (32, 96)
SLIME_CELL = (64, 12)

INK = "#1e1018"
FLESH_SHADOW = "#452031"
FLESH = "#71313f"
FLESH_LIGHT = "#ad5860"
WET = "#dc8a7b"
WET_LIGHT = "#f7b7a0"

SLIME_INK = "#172019"
SLIME_SHADOW = "#313a22"
SLIME_BODY = "#4e5730"
SLIME_SURFACE = "#717a3b"
SLIME_LIGHT = "#9aab55"
SLIME_GLEAM = "#c5ce79"


def fissure(draw, open_wide):
    """The exposed lips and the split between the displaced floor chips."""
    top = 84 if open_wide else 88
    draw.polygon([(0, 95), (3, 93), (7, 93), (10, top + 4), (15, top), (20, top + 1),
                  (23, top + 4), (27, 92), (30, 93), (31, 95)], fill=INK)
    draw.polygon([(5, 94), (10, 91), (14, top + 2), (19, top + 2), (22, top + 5),
                  (26, 94), (20, 95), (12, 95)], fill=FLESH_SHADOW)
    draw.polygon([(8, 94), (12, top + 4), (15, top + 1), (18, top + 1), (21, top + 4),
                  (23, 94), (19, 92), (14, 92)], fill=FLESH)
    draw.line([(10, 93), (13, top + 4), (16, top + 2), (18, top + 2)], fill=FLESH_LIGHT)
    draw.line([(14, 94), (17, top + 5), (20, 93), (23, 94)], fill=INK)
    draw.line([(2, 95), (5, 94), (7, 94)], fill="#805858")
    draw.line([(25, 95), (28, 94), (30, 95)], fill="#805858")


def tell_frame(index):
    frame = Image.new("RGBA", TENTACLE_CELL)
    draw = ImageDraw.Draw(frame)
    fissure(draw, index == 1)
    if index == 0:
        draw.line([(13, 92), (16, 90), (18, 90), (20, 92)], fill=FLESH_LIGHT)
        draw.point((17, 90), fill=WET)
    else:
        draw.polygon([(9, 93), (11, 86), (15, 81), (19, 80), (22, 83), (23, 88), (25, 93),
                      (20, 92), (17, 87), (14, 90), (13, 93)], fill=FLESH, outline=INK)
        draw.line([(11, 91), (13, 85), (17, 82), (20, 82), (21, 84)], fill=FLESH_LIGHT)
        draw.line([(14, 85), (16, 83), (18, 83)], fill=WET)
        draw.point((22, 87), fill=FLESH_LIGHT)
        draw.point((8, 88), fill="#925b55")
        draw.line([(25, 89), (26, 90)], fill="#925b55")
    return frame


def ribbon(points, left, right):
    """Two edges in pixel coordinates around a hand-keyed centerline."""
    return [(round(x + radius * left), y) for x, y, radius in points] + [
        (round(x + radius * right), y) for x, y, radius in reversed(points)
    ]


def tentacle_frame(points, chips):
    frame = Image.new("RGBA", TENTACLE_CELL)
    draw = ImageDraw.Draw(frame)
    fissure(draw, True)

    draw.polygon(ribbon(points, -1, 1), fill=FLESH_SHADOW, outline=INK)
    draw.polygon(ribbon(points, -.75, .48), fill=FLESH)
    draw.polygon(ribbon(points, -.56, -.12), fill=FLESH_LIGHT)

    # Off-center sinews form tapered patches down the lit ridge.
    for start, end in ((1, 2), (3, 4), (5, 6)):
        draw.line([(round(x - radius * .47), y) for x, y, radius in points[start:end + 1]], fill=WET)
    for x, y, radius in points[1:5]:
        if radius > 2:
            center = round(x + radius * .34)
            draw.polygon([(center - 1, y), (center + 2, y - 1), (center + 2, y + 1),
                          (center, y + 2)], fill=FLESH_LIGHT)
            draw.line([(center, y), (center + 1, y)], fill=FLESH_SHADOW)

    tip_x, tip_y, _ = points[-1]
    neck_x, neck_y, neck_radius = points[-2]
    neck_left = round(neck_x - neck_radius)
    neck_right = round(neck_x + neck_radius)
    draw.polygon([(neck_left, neck_y + 1), (neck_left, neck_y - 1), (tip_x, tip_y),
                  (neck_right, neck_y), (neck_x, neck_y + 3)], fill=FLESH_LIGHT, outline=INK)
    draw.line([(neck_left + 1, neck_y), (tip_x, tip_y + 1)], fill=WET)
    draw.point((tip_x, tip_y + 1), fill=WET_LIGHT)

    # Dark rim in front of the base, with smaller reflected highlights than the barb.
    draw.polygon([(8, 94), (12, 92), (16, 94), (21, 91), (25, 94), (26, 95),
                  (20, 95), (15, 95), (12, 94)], fill=FLESH_SHADOW)
    draw.line([(9, 94), (12, 93), (15, 94)], fill=FLESH_LIGHT)
    draw.line([(18, 94), (22, 92), (24, 94)], fill=FLESH_LIGHT)
    for x, y, width in chips:
        draw.line([(x, y), (x + width - 1, y - width // 2)], fill=FLESH_LIGHT)
        draw.point((x, y + 1), fill=FLESH_SHADOW)
    return frame


def bubble(draw, x, surface, state, large):
    if state == 0:
        draw.line([(x - 2, surface), (x + 2, surface)], fill=SLIME_SHADOW)
        draw.line([(x - 1, surface - 1), (x + 1, surface - 1)], fill=SLIME_LIGHT)
    elif state == 1:
        height = 5 if large else 3
        draw.polygon([(x - 3, surface), (x - 3, surface - 2), (x - 1, surface - height),
                      (x + 1, surface - height), (x + 3, surface - 2), (x + 3, surface),
                      (x + 1, surface + 1), (x - 1, surface + 1)], fill=SLIME_BODY, outline=SLIME_INK)
        draw.line([(x - 2, surface - 2), (x - 1, surface - height + 1),
                   (x + 1, surface - height + 1)], fill=SLIME_GLEAM)
        draw.point((x + 2, surface - 1), fill=SLIME_SURFACE)
    else:
        draw.line([(x - 4, surface), (x - 2, surface - 1), (x, surface)], fill=SLIME_GLEAM)
        draw.line([(x + 2, surface), (x + 4, surface - 1)], fill=SLIME_LIGHT)
        draw.point((x + 2, surface - 4), fill=SLIME_LIGHT)
        if large:
            draw.point((x - 2, surface - 5), fill=SLIME_SURFACE)


def slime_frame(index):
    frame = Image.new("RGBA", SLIME_CELL)
    draw = ImageDraw.Draw(frame)
    draw.polygon([(-1, 11), (-1, 9), (6, 9), (9, 7), (17, 7), (21, 6), (28, 6),
                  (32, 7), (37, 7), (43, 6), (50, 7), (53, 7), (57, 9), (64, 9), (64, 11)],
                 fill=SLIME_SHADOW, outline=SLIME_INK)
    draw.polygon([(4, 10), (9, 8), (17, 8), (21, 7), (29, 7), (32, 8), (39, 8), (43, 7),
                  (50, 8), (54, 8), (57, 10), (60, 10), (55, 10), (51, 9), (44, 10), (38, 9),
                  (33, 10), (29, 10), (26, 9), (19, 10), (14, 9), (9, 10)], fill=SLIME_BODY)
    draw.line([(7, 10), (12, 9), (16, 9)], fill=SLIME_LIGHT)
    draw.line([(25, 9), (27, 8), (32, 8)], fill=SLIME_SURFACE)
    draw.line([(38, 9), (41, 9), (44, 8)], fill=SLIME_LIGHT)
    draw.line([(52, 9), (54, 9), (56, 10)], fill=SLIME_SURFACE)
    # This three-pixel shallows profile also joins cleanly when the stage tiles the sprite.
    draw.line([(60, 10), (63, 10)], fill=SLIME_BODY)
    draw.line([(0, 10), (4, 10)], fill=SLIME_BODY)

    for x, surface, offset, large in ((13, 8, 2, False), (24, 8, 0, True),
                                      (36, 9, 1, False), (47, 8, 2, True), (55, 9, 0, False)):
        bubble(draw, x, surface, (index + offset) % 3, large)
    return frame


def main():
    frames = {f"floor_tentacle_tell_f{i:02}": tell_frame(i) for i in range(2)}
    rise = (
        ([(16, 95, 7), (15, 84, 6), (14, 75, 5), (16, 66, 4), (20, 57, 3), (21, 46, 2), (20, 38, 0)],
         [(5, 86, 2), (9, 79, 1), (26, 82, 2), (28, 90, 1), (20, 30, 1)]),
        ([(16, 95, 7), (13, 82, 6), (14, 69, 5), (18, 57, 4), (19, 44, 3), (17, 34, 2), (15, 27, 0)],
         [(5, 90, 2), (8, 84, 1), (25, 74, 1), (24, 87, 2), (28, 91, 1)]),
        ([(16, 95, 7), (16, 82, 6), (18, 69, 5), (20, 57, 4), (17, 46, 3), (14, 36, 2), (14, 28, 0)],
         [(7, 93, 2), (9, 88, 1), (26, 89, 1)]),
    )
    retract = (
        ([(16, 95, 7), (18, 87, 6), (20, 80, 5), (20, 71, 4), (16, 64, 3), (12, 57, 2), (11, 51, 0)],
         [(6, 91, 2), (24, 85, 1)]),
        ([(16, 95, 7), (18, 92, 6), (20, 89, 5), (21, 85, 4), (20, 81, 3), (16, 78, 2), (12, 77, 0)],
         [(7, 91, 1), (26, 90, 1)]),
    )
    for name, sequence in (("rise", rise), ("retract", retract)):
        for index, (points, chips) in enumerate(sequence):
            frames[f"floor_tentacle_{name}_f{index:02}"] = tentacle_frame(points, chips)
    frames.update({f"slime_pool_f{i:02}": slime_frame(i) for i in range(3)})
    for name, frame in frames.items():
        frame.save(OUT / f"{name}.png", optimize=True)


if __name__ == "__main__":
    main()
