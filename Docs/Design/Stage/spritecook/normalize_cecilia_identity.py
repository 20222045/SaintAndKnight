"""Normalize the identity-corrected Cecilia draft without modifying canonical art.

The raw v3 edit stays intact. This creates a 4x2 sheet of 128x96 cells,
uses only the canonical 32 colours, preserves aspect ratio with nearest-neighbour
sampling and shares one scale across all eight poses.
Run with --sync-drafts to copy the result to the Unity Drafts folder.
"""
import argparse
import hashlib
import json
import shutil
import uuid
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
CANONICAL = ROOT / "Assets/SaintKnightDay06/Resources/SaintKnightDay06/cecilia_motion.png"
SOURCE = HERE / "4_enemy_death_cecilia_v3.png"
OUTPUT = HERE / "identity-fixed-v3"
NAME = "4_enemy_death_cecilia_v3"
WIDTH, HEIGHT = 128, 96


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def on_background(image, color):
    background = Image.new("RGBA", image.size, color)
    background.alpha_composite(image)
    return background


def source_rectangles(image):
    """Find actual sprite bounds; generated rows do not form an exact grid."""
    alpha = image.getchannel("A").point(lambda value: 255 if value >= 128 else 0)
    px = alpha.load()
    seen, bodies = set(), []
    width, height = image.size
    for y in range(height):
        for x in range(width):
            if not px[x, y] or (x, y) in seen:
                continue
            todo = [(x, y)]; seen.add((x, y)); left = right = x; top = bottom = y; count = 0; points = []
            while todo:
                xx, yy = todo.pop(); count += 1; points.append((xx, yy))
                left, right = min(left, xx), max(right, xx)
                top, bottom = min(top, yy), max(bottom, yy)
                for point in ((xx-1, yy), (xx+1, yy), (xx, yy-1), (xx, yy+1)):
                    nx, ny = point
                    if 0 <= nx < width and 0 <= ny < height and px[nx, ny] and point not in seen:
                        seen.add(point); todo.append(point)
            if count >= 1000:
                bodies.append({"bounds": (left, top, right + 1, bottom + 1), "pixels": points})
    assert len(bodies) == 8, ("Expected eight separate full sprites", bodies)
    bodies.sort(key=lambda body: body["bounds"][1])
    assert max(body["bounds"][3] for body in bodies[:4]) < min(body["bounds"][1] for body in bodies[4:])
    bodies = sorted(bodies[:4], key=lambda body: body["bounds"][0]) + sorted(bodies[4:], key=lambda body: body["bounds"][0])
    return [((max(0, body["bounds"][0]-3), max(0, body["bounds"][1]-3),
              min(width, body["bounds"][2]+3), min(height, body["bounds"][3]+3)), body["pixels"]) for body in bodies]


def build(destination):
    assert sha(CANONICAL).startswith("9ab32982cb56"), "Canonical design changed; inspect before proceeding."
    with Image.open(CANONICAL) as image:
        atlas = image.convert("RGBA")
    original = atlas.crop((0, 0, WIDTH, HEIGHT))
    original_box = original.getbbox()
    body_height = original_box[3] - original_box[1]
    baseline = original_box[3] - 1
    assert body_height == 55
    palette = sorted({color[:3] for color in atlas.get_flattened_data() if color[3]})
    assert len(palette) == 32
    palette_image = Image.new("P", (1, 1))
    palette_image.putpalette([v for color in palette + [palette[-1]] * (256 - len(palette)) for v in color])
    with Image.open(SOURCE) as image:
        raw = image.convert("RGBA")
    sources, rectangles = [], []
    for index, (rectangle, body_pixels) in enumerate(source_rectangles(raw)):
        frame = raw.crop(rectangle)
        # Keep the eight complete bodies, excluding detached impact specks/fringe.
        mask = Image.new("L", frame.size)
        for x, y in body_pixels:
            mask.putpixel((x - rectangle[0], y - rectangle[1]), 255)
        frame.putalpha(mask)
        bounds = frame.getbbox()
        assert bounds, ("Empty frame", index)
        assert bounds[0] > 0 and bounds[1] > 0 and bounds[2] < frame.width and bounds[3] < frame.height, ("Source cut at a cell edge", index)
        sources.append(frame.crop(bounds))
        rectangles.append(list(rectangle))
    scale = body_height / sources[0].height
    frames, entries = [], []
    for index, source in enumerate(sources):
        image = source.resize((round(source.width * scale), round(source.height * scale)), Image.Resampling.NEAREST)
        assert image.width < WIDTH - 4 and image.height <= baseline + 1, ("Normalized crop would clip", index)
        alpha = image.getchannel("A")
        image = image.convert("RGB").quantize(palette=palette_image, dither=Image.Dither.NONE).convert("RGBA")
        image.putalpha(alpha)
        frame = Image.new("RGBA", (WIDTH, HEIGHT))
        frame.alpha_composite(image, ((WIDTH - image.width) // 2, baseline - image.height + 1))
        frames.append(frame)
        bounds = frame.getbbox()
        assert bounds[3] == baseline + 1
        assert set(frame.getchannel("A").get_flattened_data()) <= {0, 255}
        assert all(color[:3] in palette for color in frame.get_flattened_data() if color[3])
        entries.append({"index": index, "sourceRect": rectangles[index], "bounds": list(bounds), "opaquePixels": sum(c[3] > 0 for c in frame.get_flattened_data())})
    assert len({frame.tobytes() for frame in frames}) == 8
    first_box = frames[0].getbbox()
    assert first_box[3] - first_box[1] == 55
    sheet = Image.new("RGBA", (WIDTH * 4, HEIGHT * 2))
    for index, frame in enumerate(frames):
        sheet.alpha_composite(frame, (index % 4 * WIDTH, index // 4 * HEIGHT))
    destination.mkdir(parents=True, exist_ok=True)
    sheet.save(destination / (NAME + ".png"))
    metadata = {"schemaVersion": 1, "character": "cecilia", "frameWidth": WIDTH, "frameHeight": HEIGHT,
                "columns": 4, "rows": 2, "usedFrames": 8, "ppu": 48, "pivotX": .5, "pivotY": .26625,
                "groundPixelY": baseline, "standingHeight": body_height,
                "heldCollapse": {"start": 0, "count": 4}, "executionerKnockdown": {"start": 4, "count": 4}}
    (destination / (NAME + ".json")).write_text(json.dumps(metadata, indent=2) + "\n", encoding="utf-8")
    on_background(sheet, (40, 40, 48, 255)).resize((WIDTH * 12, HEIGHT * 6), Image.Resampling.NEAREST).save(destination / "cecilia_identity_fixed_contact.png")
    animations = []
    for index in range(4):
        preview = Image.new("RGBA", (WIDTH * 2, HEIGHT), (40, 40, 48, 255))
        preview.alpha_composite(frames[index], (0, 0))
        preview.alpha_composite(frames[index + 4], (WIDTH, 0))
        animations.append(preview.resize((WIDTH * 8, HEIGHT * 4), Image.Resampling.NEAREST).convert("RGB"))
    animations[0].save(destination / "cecilia_identity_fixed_motion.gif", save_all=True,
                       append_images=animations[1:], duration=[220, 180, 180, 700], loop=0, disposal=2)
    rejected_path = HERE / "4_enemy_death_cecilia_v2.png"
    with Image.open(rejected_path) as image:
        rejected = image.convert("RGBA")
    rejected = rejected.crop((0, 0, round(rejected.width / 4), round(rejected.height / 2)))
    rejected = rejected.crop(rejected.getbbox())
    rejected = rejected.resize((round(rejected.width * body_height / rejected.height), body_height), Image.Resampling.NEAREST)
    rejected_frame = Image.new("RGBA", (WIDTH, HEIGHT))
    rejected_frame.alpha_composite(rejected, ((WIDTH - rejected.width) // 2, baseline - body_height + 1))
    comparison = Image.new("RGB", (WIDTH * 12, HEIGHT * 8 + 56), (40, 40, 48))
    draw = ImageDraw.Draw(comparison)
    for row, color in enumerate(((40, 40, 48, 255), (230, 230, 230, 255))):
        for col, (label, frame) in enumerate(zip(("Original canonical", "Rejected v2", "Corrected v3"), (original, rejected_frame, frames[0]))):
            x, y = col * WIDTH * 4, row * (HEIGHT * 4 + 28)
            draw.text((x + 10, y + 8), label, fill=(235, 235, 235))
            comparison.paste(on_background(frame, color).resize((WIDTH * 4, HEIGHT * 4), Image.Resampling.NEAREST).convert("RGB"), (x, y + 26))
    comparison.save(destination / "cecilia_design_comparison.png")
    audit = {"canonicalSha256": sha(CANONICAL), "sourceSha256": sha(SOURCE), "outputSha256": sha(destination / (NAME + ".png")),
             "sourceSize": list(raw.size), "outputSize": list(sheet.size), "uniformScale": scale,
             "paletteColors": len(palette), "standingHeight": 55, "frames": entries,
             "checks": {"eightDistinctFrames": True, "noClippedCells": True, "canonicalPaletteOnly": True,
                        "hardAlpha": True, "feetAligned": True, "canonicalUnchanged": True}}
    (destination / "validation.json").write_text(json.dumps(audit, indent=2) + "\n", encoding="utf-8")
    return audit


def sync(destination):
    drafts = ROOT / "Assets/SaintKnightSpriteCook/Drafts"
    for extension in (".png", ".json"):
        path = drafts / (NAME + extension)
        if path.exists() and path.read_bytes() != (destination / path.name).read_bytes():
            raise RuntimeError("Draft already exists with different content; back it up first: " + str(path))
        shutil.copyfile(destination / path.name, path)
    metadata = drafts / (NAME + ".png.meta")
    if not metadata.exists():
        template = (drafts / "4_enemy_death_cecilia_v1.png.meta").read_text(encoding="utf-8")
        old_guid = next(line for line in template.splitlines() if line.startswith("guid: "))
        metadata.write_text(template.replace(old_guid, "guid: " + uuid.uuid4().hex, 1), encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out", type=Path, default=OUTPUT)
    parser.add_argument("--sync-drafts", action="store_true")
    args = parser.parse_args()
    audit = build(args.out)
    if args.sync_drafts:
        sync(args.out)
    print(json.dumps({"outputSize": audit["outputSize"], "standingHeight": audit["standingHeight"], "checks": audit["checks"]}, indent=2))


if __name__ == "__main__":
    main()
