"""
Turn the HUD damage-stage illustrations into 2D pixel art (2026-11-17, user request: the side
illustration should read as dot art like the reference game's portraits).

Source: the original illustrations backed up in Docs/Design/Characters/reference/portraits/original_png/
(git-ignored, like the illustrations themselves). Output overwrites
Assets/SaintKnightDay06/Art/Portraits/<who>_portrait_stage<0-4>.png at the same size, so every scene,
GUID and HUD layout stays as it is.

    python -I make_pixel_portraits.py [--preview DIR] [--factor N] [--colors N]

Steps: downscale x2 (392x584 art pixels), one shared palette for all five stages, hard alpha,
a darkened rim on the silhouette edge, then nearest-neighbour back up x2 onto the original canvas size.
"""
import argparse
import os
from PIL import Image, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
SRC = os.path.join(ROOT, "Docs", "Design", "Characters", "reference", "portraits", "original_png")
DST = os.path.join(ROOT, "Assets", "SaintKnightDay06", "Art", "Portraits")
FACTOR, COLORS = 2, 128


def pixelate(images, FACTOR=FACTOR, COLORS=COLORS):
    sizes = [im.size for im in images]
    small = []
    for im in images:
        w, h = im.size
        s = im.resize((w // FACTOR, h // FACTOR), Image.LANCZOS)
        s = s.filter(ImageFilter.UnsharpMask(radius=1, percent=60, threshold=2))   # keep eyes and mouth lines
        small.append(s)
    # one palette from the opaque pixels of every stage, so colours do not shift between stages
    sheet = Image.new("RGB", (small[0].width * len(small), small[0].height), (0, 0, 0))
    for i, s in enumerate(small):
        bg = Image.new("RGB", s.size, tuple(s.convert("RGB").resize((1, 1)).getpixel((0, 0))))
        bg.paste(s.convert("RGB"), mask=s.split()[3])
        sheet.paste(bg, (i * s.width, 0))
    palette = sheet.quantize(COLORS, method=Image.Quantize.MAXCOVERAGE)
    out = []
    for s in small:
        alpha = s.split()[3].point(lambda a: 255 if a >= 128 else 0)
        rgb = s.convert("RGB").quantize(palette=palette, dither=Image.Dither.NONE).convert("RGB")
        px, ap = rgb.load(), alpha.load()
        w, h = rgb.size
        rim = []
        for y in range(h):
            for x in range(w):
                if ap[x, y] == 0:
                    continue
                if any(not (0 <= x + dx < w and 0 <= y + dy < h) or ap[x + dx, y + dy] == 0
                       for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                    rim.append((x, y))
        for x, y in rim:
            r, g, b = px[x, y]
            px[x, y] = (int(r * .45), int(g * .42), int(b * .5))
        im = rgb.convert("RGBA")
        im.putalpha(alpha)
        big = Image.new("RGBA", sizes[len(out)], (0, 0, 0, 0))
        big.paste(im.resize((w * FACTOR, h * FACTOR), Image.NEAREST), (0, 0))
        out.append(big)
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--who", default="cecilia")
    ap.add_argument("--preview", help="write here instead of the Assets folder")
    ap.add_argument("--factor", type=int, default=FACTOR)
    ap.add_argument("--colors", type=int, default=COLORS)
    a = ap.parse_args()
    names = ["%s_portrait_stage%d.png" % (a.who, i) for i in range(5)]
    images = [Image.open(os.path.join(SRC, n)).convert("RGBA") for n in names]
    dst = a.preview or DST
    os.makedirs(dst, exist_ok=True)
    for n, im in zip(names, pixelate(images, a.factor, a.colors)):
        im.save(os.path.join(dst, n))
        print("wrote", os.path.join(dst, n))


if __name__ == "__main__":
    main()
