"""Pack the reference-based boss art into the existing Unity sprite cells."""

from pathlib import Path

from PIL import Image


HERE = Path(__file__).resolve().parent
OUT = HERE.parents[3] / "Assets" / "SaintKnightDay09" / "Art" / "Boss"
CELL_SIZE = (256, 300)


def load_visible_art(name):
    with Image.open(HERE / name) as source:
        art = source.convert("RGBA")
    bounds = art.getchannel("A").point(lambda alpha: 255 if alpha >= 96 else 0).getbbox()
    if bounds is None:
        raise ValueError(f"No visible artwork in {name}")
    return art.crop(bounds)


def save_cell(art, name, height, max_width=246, palette=None):
    scale = min(height / art.height, max_width / art.width)
    size = round(art.width * scale), round(art.height * scale)
    sprite = art.resize(size, Image.Resampling.LANCZOS)
    alpha = sprite.getchannel("A").point(lambda value: 255 if value >= 112 else 0)
    sprite = sprite.convert("RGB").quantize(colors=96, palette=palette, dither=Image.Dither.NONE)

    cell = Image.new("RGBA", CELL_SIZE)
    position = (CELL_SIZE[0] - sprite.width) // 2, CELL_SIZE[1] - sprite.height
    cell.paste(sprite.convert("RGBA"), position, alpha)
    cell.save(OUT / f"boss_body_{name}_NOT_FINAL.png")
    return sprite


def main():
    for phase in (1, 2):
        art = load_visible_art(f"source_phase{phase}.png")
        # Both frames share a palette and rest on the same floor line.
        palette = save_cell(art, f"p{phase}_f00", 240)
        save_cell(art, f"p{phase}_f01", 242, palette=palette)

    defeated = load_visible_art("source_defeated.png")
    save_cell(defeated, "dead_f00", 124, max_width=190)


if __name__ == "__main__":
    main()
