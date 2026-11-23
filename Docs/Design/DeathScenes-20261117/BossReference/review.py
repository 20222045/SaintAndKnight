"""Assemble the Boss Chapel Play Mode camera captures at their recorded 8 fps."""
from pathlib import Path
import sys
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
SOURCE = Path(sys.argv[1]) if len(sys.argv) > 1 else HERE.parents[3] / 'Logs/BossReference20261118/Screens'
OUTPUT = Path(sys.argv[2]) if len(sys.argv) > 2 else HERE
CHARACTERS = [('cecilia', '세실리아'), ('adela', '아델라')]
PATTERNS = [('boss_spikes', '바닥 촉수', 18), ('boss_grab', '촉수 잡기', 32), ('boss_spit', '점액 뱉기', 18)]
COMMON = [('boss1', '뿌리 고치'), ('boss2', '진홍 꽃받침'), ('boss3', '장미와 사슬'), ('boss4', '겹고리')]
REGULAR = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 14)
BOLD = ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf', 17)
BG, TEXT, GOLD = '#17151d', '#ebe4df', '#d1b997'
WIDTH, HEIGHT, GAP = 264, 192, 6


def still(ch, key, frame):
    with Image.open(SOURCE / f'{ch}_{key}_motion_{frame:02d}.png') as src:
        return src.convert('RGB').crop((60, 48, 60 + WIDTH, 48 + HEIGHT))


def save_animation(path, frames):
    # Use a shared palette and alternating centiseconds to keep fine outlines stable and time exact.
    swatches = Image.new('RGB', (160, 80 * len(frames)))
    for i, frame in enumerate(frames):
        swatches.paste(frame.resize((160, 80), Image.Resampling.NEAREST), (0, 80 * i))
    palette = swatches.quantize(colors=256)
    indexed = [frame.quantize(palette=palette, dither=Image.Dither.NONE) for frame in frames]
    indexed[0].save(path, save_all=True, append_images=indexed[1:], loop=0,
                    duration=[120 if i % 2 == 0 else 130 for i in range(len(frames))],
                    disposal=1, optimize=False)


def patterns():
    frames = []
    for order, (key, label, count) in enumerate(PATTERNS, 1):
        for f in range(count):
            canvas = Image.new('RGB', (WIDTH*2 + 3*GAP, HEIGHT + 64), BG)
            draw = ImageDraw.Draw(canvas)
            draw.text((GAP + 4, 4), f'{order}   {label}', font=BOLD, fill=GOLD)
            for col, (ch, name) in enumerate(CHARACTERS):
                x = GAP + col * (WIDTH + GAP)
                canvas.paste(still(ch, key, f), (x, 34))
                draw.text((x + 4, HEIGHT + 38), name, font=REGULAR, fill=TEXT)
            frames.append(canvas)
    save_animation(OUTPUT / 'boss-patterns.gif', frames)


def finishes():
    frames = []
    for f in range(32):
        canvas = Image.new('RGB', (4*(WIDTH+GAP)+GAP, 2*(HEIGHT+28)+42), BG)
        draw = ImageDraw.Draw(canvas)
        for col, (key, label) in enumerate(COMMON):
            x = GAP + col*(WIDTH+GAP)
            draw.text((x+4, 5), f'{col+1}   {label}', font=BOLD, fill=GOLD)
            for row, (ch, name) in enumerate(CHARACTERS):
                y = 35 + row*(HEIGHT+28)
                canvas.paste(still(ch, key, f), (x, y))
                draw.text((x+4, y+HEIGHT+3), name, font=REGULAR, fill=TEXT)
        frames.append(canvas)
    save_animation(OUTPUT / 'boss-finishes.gif', frames)


if __name__ == '__main__':
    patterns()
    finishes()
