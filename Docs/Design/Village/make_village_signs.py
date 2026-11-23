"""Village door bubble "[W] 들어가기" (2026-11-19, user: "make the lettering cute"; the hanging name boards
drawn here first were removed the same day at the user's request).

Pixel art at the village's own scale (1 px = 1 px of the 640x360 art): a cream speech bubble with a key cap,
Gulim 12 px (crisp bitmap Hangul). Output: Assets/SaintKnightLobby/Art/Signs/hint_enter.png; SKLobby pops it up
(pivot at its tail) over the door she stands at.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

OUT = Path(__file__).resolve().parents[3] / "Assets/SaintKnightLobby/Art/Signs"
FONT = ImageFont.truetype("C:/Windows/Fonts/gulim.ttc", 12)
INK, CREAM = (59, 33, 24), (255, 243, 214)


def text(d, xy, s, fill, outline=None):
    d.fontmode = "1"
    x, y = xy
    if outline:
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                if dx or dy:
                    d.text((x + dx, y + dy), s, font=FONT, fill=outline)
    d.text((x, y), s, font=FONT, fill=fill)


def width(s):
    return int(FONT.getlength(s))


GLYPHS = {"W": ["#...#", "#...#", "#.#.#", "#.#.#", ".#.#."]}


def bubble(verb, key="W"):
    tw = width(verb)
    w, h = tw + 26, 17
    im = Image.new("RGBA", (w + 2, h + 6), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    x0, y0, x1, y1 = 1, 1, w, h
    d.rectangle([x0 + 2, y0, x1 - 2, y1], fill=INK)
    d.rectangle([x0, y0 + 2, x1, y1 - 2], fill=INK)
    d.rectangle([x0 + 1, y0 + 1, x1 - 1, y1 - 1], fill=INK)
    d.rectangle([x0 + 2, y0 + 1, x1 - 2, y1 - 1], fill=CREAM)
    d.rectangle([x0 + 1, y0 + 2, x1 - 1, y1 - 2], fill=CREAM)
    d.line([(x0 + 2, y1 - 1), (x1 - 2, y1 - 1)], fill=(238, 214, 176))
    # tail
    mx = (x0 + x1) // 2
    for k in range(4):
        d.line([(mx - 3 + k, y1 + k), (mx + 3 - k, y1 + k)], fill=INK)
        if k < 3:
            d.line([(mx - 2 + k, y1 - 1 + k), (mx + 2 - k, y1 - 1 + k)], fill=CREAM)
    # key cap
    kx, ky = x0 + 4, y0 + 3
    d.rectangle([kx, ky, kx + 11, ky + 11], fill=INK)
    d.rectangle([kx + 1, ky + 1, kx + 10, ky + 10], fill=(150, 142, 168))
    d.rectangle([kx + 1, ky + 1, kx + 10, ky + 8], fill=(236, 232, 246))
    for gy, row in enumerate(GLYPHS[key]):      # hand-set 5x5 letter: Gulim's W does not fit the cap
        for gx, c in enumerate(row):
            if c == "#":
                d.point((kx + 3 + gx, ky + 2 + gy), fill=INK)
    text(d, (kx + 15, y0 + 2), verb, INK)
    return im


OUT.mkdir(parents=True, exist_ok=True)
bubble("들어가기").save(OUT / "hint_enter.png")
preview = Image.new("RGBA", (90, 30), (70, 52, 70, 255))
preview.alpha_composite(Image.open(OUT / "hint_enter.png"), (4, 4))
preview.resize((360, 120), Image.NEAREST).save(Path(__file__).resolve().parent / "signs_preview.png")
print("wrote", OUT)
