"""Assemble actual Unity play-mode captures into review GIFs; never writes game/source art."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / "Logs/BossCarry20261118/Screens"
DELIVERY = ROOT / "Logs/BossCarry20261118/Delivery"
DEST = Path(__file__).resolve().parent
KEYS = ["monk", "executioner", "tentacle", "fleshwall", "boss_spikes", "boss_grab", "boss_spit", "boss1", "boss2", "boss3", "boss4", "boss5", "boss6"]
LABELS = {"monk": "Monk", "executioner": "Executioner", "tentacle": "Floor tentacles", "fleshwall": "Flesh wall", "boss_spikes": "Boss floor roots", "boss_grab": "Boss grab", "boss_spit": "Boss coating", **{"boss" + str(i): "Common kneel " + str(i) for i in range(1, 7)}}
font_path = Path("C:/Windows/Fonts/arial.ttf")
FONT = ImageFont.truetype(str(font_path), 18) if font_path.exists() else ImageFont.load_default()


def frames(character, key):
    result = []
    for suffix in (key + "_capture", key):
        for path in sorted(SOURCE.glob(character + "_" + suffix + "_motion_*.png")):
            with Image.open(path) as image:
                # Focus the review on the actual play-field actor, outside the existing HUD illustration.
                result.append(image.convert("RGB").crop((32, 8, 352, 248)))
    if not result:
        raise FileNotFoundError(character + "/" + key)
    return result


def save_gif(path, images):
    images[0].save(path, save_all=True, append_images=images[1:], duration=150, loop=0, optimize=False, disposal=2)


def export_delivery():
    selections = [(character, side) for character in ("cecilia", "adela") for side in ("left", "right")]
    clips = []
    for character, side in selections:
        clip = []
        for frame in range(48):
            with Image.open(DELIVERY / f"{character}_{side}_{frame:02d}.png") as image:
                # Keep the fixed game-camera view of both participants and remove the unrelated HUD edge.
                clip.append(image.convert("RGB").crop((0, 76, 640, 400)))
        clips.append(clip)
    result = []
    for frame in range(48):
        canvas = Image.new("RGB", (1280, 696), (17, 17, 22))
        draw = ImageDraw.Draw(canvas)
        for index, ((character, side), clip) in enumerate(zip(selections, clips)):
            x, y = index % 2 * 640, index // 2 * 348
            canvas.paste(clip[frame], (x, y + 24))
            draw.text((x + 8, y + 2), character.capitalize() + " / From " + side,
                      fill=(225, 224, 225), font=FONT)
        result.append(canvas)
    save_gif(DEST / "boss-delivery.gif", result)
    review = Image.new("RGB", (1920, 1392), (17, 17, 22))
    draw = ImageDraw.Draw(review)
    for row, ((character, side), clip) in enumerate(zip(selections, clips)):
        for col, frame in enumerate((0, 16, 47)):
            review.paste(clip[frame], (col * 640, row * 348 + 24))
            draw.text((col * 640 + 8, row * 348 + 2),
                      f"{character.capitalize()} / {side} / frame {frame}",
                      fill=(225, 224, 225), font=FONT)
    review.save(DEST / "boss-delivery-review.png")


def main():
    DEST.mkdir(parents=True, exist_ok=True)
    for character in ("cecilia", "adela"):
        for group in range(4):
            keys = KEYS[group * 4:(group + 1) * 4]
            sheet = Image.new("RGB", (320 * 3, 264 * len(keys)), (17, 17, 22))
            draw = ImageDraw.Draw(sheet)
            for row, key in enumerate(keys):
                images = frames(character, key)
                for col, index in enumerate((0, len(images) // 3, len(images) - 1)):
                    sheet.paste(images[index], (col * 320, row * 264 + 24))
                draw.text((8, row * 264 + 2), character.capitalize() + " / " + LABELS[key], fill=(225, 224, 225), font=FONT)
            sheet.save(DEST / (character + "_actual_review_" + str(group) + ".png"))

    selections = [(character, key) for character in ("cecilia", "adela") for key in ("boss_spikes", "boss_grab", "boss_spit", "boss1")]
    clips = [frames(character, key) for character, key in selections]
    montage = []
    for frame in range(max(map(len, clips))):
        image = Image.new("RGB", (1280, 528), (17, 17, 22))
        draw = ImageDraw.Draw(image)
        for index, ((character, key), clip) in enumerate(zip(selections, clips)):
            x, y = index % 4 * 320, index // 4 * 264
            sample = frame if frame < len(clip) else 8 + (frame - 8) % 10
            image.paste(clip[sample], (x, y + 24))
            draw.text((x + 8, y + 2), character.capitalize() + " / " + LABELS[key], fill=(225, 224, 225), font=FONT)
        montage.append(image)
    save_gif(DEST / "actual-game-preview.gif", montage)
    common = [frames(ch, "boss1") for ch in ("cecilia", "adela")]
    result = []
    for frame in range(len(common[0])):
        canvas = Image.new("RGB", (640, 264), (17, 17, 22))
        draw = ImageDraw.Draw(canvas)
        for index, (ch, clip) in enumerate(zip(("Cecilia", "Adela"), common)):
            canvas.paste(clip[frame], (320 * index, 24))
            draw.text((320 * index + 8, 2), ch + " / Common kneel", fill=(225, 224, 225), font=FONT)
        result.append(canvas)
    save_gif(DEST / "boss-common-kneeling.gif", result)
    export_delivery()
    print("Exported actual Unity scene reviews, common kneeling and four boss-delivery clips")


if __name__ == "__main__":
    main()
