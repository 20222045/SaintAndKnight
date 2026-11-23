"""
Turn a high-resolution anime illustration into a full-screen pixel CG (2026-11-19). The reference game's
CGs are illustrations shrunk into big pixels; generating straight at 256x144 loses faces and figures.

    python -I pixelize_cg.py SOURCE.png OUT.png [--width 256] [--height 144] [--colors 96]

Crops the source to the target aspect (centre), downscales with Lanczos, sharpens the line work a little,
reduces to one palette without dithering. Output is the native pixel size (Unity shows it point-filtered).
"""
import argparse
from PIL import Image, ImageFilter


def pixelize(src, w=256, h=144, colors=96):
    im = src.convert("RGB")
    sw, sh = im.size
    if sw / sh > w / h:                      # too wide: trim the sides
        cw = round(sh * w / h)
        im = im.crop(((sw - cw) // 2, 0, (sw - cw) // 2 + cw, sh))
    else:                                    # too tall: trim top and bottom evenly
        ch = round(sw * h / w)
        im = im.crop((0, (sh - ch) // 2, sw, (sh - ch) // 2 + ch))
    small = im.resize((w, h), Image.LANCZOS).filter(ImageFilter.UnsharpMask(radius=1, percent=70, threshold=2))
    return small.quantize(colors, method=Image.Quantize.MAXCOVERAGE, dither=Image.Dither.NONE).convert("RGB")


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("source")
    ap.add_argument("out")
    ap.add_argument("--width", type=int, default=256)
    ap.add_argument("--height", type=int, default=144)
    ap.add_argument("--colors", type=int, default=96)
    a = ap.parse_args()
    out = pixelize(Image.open(a.source), a.width, a.height, a.colors)
    out.save(a.out)
    assert out.size == (a.width, a.height) and len(out.getcolors(1 << 16)) <= a.colors
    print(a.out, out.size)
