"""Split the Day 9 enemy sheets (Day09/, one row per motion) into one PNG per frame for Unity.

Day09/<set>/<motion>.png  ->  Assets/SaintKnightDay09/Art/Enemies/<set>/<motion>_fNN.png
Cell sizes and frame counts come from Day09/frames.json. Pixels are copied untouched.
"""
import json
from pathlib import Path

from PIL import Image

SRC = Path(__file__).parent / 'Day09'
DST = Path(__file__).parents[3] / 'Assets' / 'SaintKnightDay09' / 'Art' / 'Enemies'
spec = json.loads((SRC / 'frames.json').read_text(encoding='utf-8'))
# Overwrite in place (never delete the folder) so Unity keeps each frame's .meta GUID.
for name, s in spec['sets'].items():
    w, h = s['cell']
    for file, count in s['frames'].items():
        sheet = Image.open(SRC / name / file).convert('RGBA')
        assert sheet.size == (w * count, h), (name, file, sheet.size)
        (DST / name).mkdir(parents=True, exist_ok=True)
        for i in range(count):
            sheet.crop((i * w, 0, (i + 1) * w, h)).save(DST / name / f'{Path(file).stem}_f{i:02d}.png')
print('frames:', sum(1 for _ in DST.rglob('*.png')))
