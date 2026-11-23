"""Review actual Unity camera captures without changing any game/source art."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[4]
SOURCE = ROOT / 'Logs/EnemyDefeat20261118/Screens'
DEST = Path(__file__).resolve().parent
KEYS = ['monk', 'executioner', 'tentacle', 'fleshwall']
LABELS = {'monk': 'Monk / both upper arms', 'executioner': 'Executioner / folded sideways',
          'tentacle': 'Floor / swallowed vertically', 'fleshwall': 'Wall / drawn sideways'}
FONT = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 17)
WIDTH, HEIGHT = 384, 280


def frames(character, key):
    result = []
    for name in [key + '_capture', key]:
        for path in sorted(SOURCE.glob(character + '_' + name + '_motion_*.png')):
            with Image.open(path) as source:
                result.append(source.convert('RGB'))
    if not result:
        raise FileNotFoundError(character + '/' + key)
    return result


def main():
    clips = {(ch, key): frames(ch, key) for ch in ['cecilia', 'adela'] for key in KEYS}
    for name, keys in [('stage-enemies', KEYS[:2]), ('stage-traps', KEYS[2:])]:
        selections = [(ch, key) for ch in ['cecilia', 'adela'] for key in keys]
        result = []
        for frame in range(max(len(clips[pair]) for pair in selections)):
            canvas = Image.new('RGB', (WIDTH * 2, HEIGHT * 2), (18, 18, 23))
            draw = ImageDraw.Draw(canvas)
            for index, (ch, key) in enumerate(selections):
                x, y = index % 2 * WIDTH, index // 2 * HEIGHT
                clip = clips[ch, key]
                sample = frame if frame < len(clip) else 8 + (frame - 8) % 10
                canvas.paste(clip[sample], (x, y + 24))
                draw.text((x + 6, y + 3), ch.capitalize() + ' / ' + LABELS[key], font=FONT, fill=(232, 230, 232))
            result.append(canvas)
        result[0].save(DEST / (name + '.gif'), save_all=True, append_images=result[1:],
                       duration=150, loop=0, optimize=True, disposal=1)
    review = Image.new('RGB', (WIDTH * 3, HEIGHT * 8), (18, 18, 23))
    draw = ImageDraw.Draw(review)
    for row, ((ch, key), clip) in enumerate(clips.items()):
        for col, frame in enumerate((0, len(clip) // 3, len(clip) - 1)):
            review.paste(clip[frame], (col * WIDTH, row * HEIGHT + 24))
        draw.text((6, row * HEIGHT + 3), ch.capitalize() + ' / ' + LABELS[key], font=FONT, fill=(232, 230, 232))
    review.save(DEST / 'actual-stage-review.png')
    print('Exported native stage enemies/traps from eight death and four live-capture cases')


if __name__ == '__main__':
    main()
