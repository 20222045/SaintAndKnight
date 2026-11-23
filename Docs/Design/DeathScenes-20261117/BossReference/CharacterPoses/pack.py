"""Pack the seven defeat drawings into Unity's native grid and convert their attachment marks."""
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

HERE = Path(__file__).resolve().parent
EDITOR = HERE.parents[4] / 'Assets/SaintKnightSpriteCook/Editor'
# Top-left sheet coordinates: crop, game height, near arm, far wrist, boot, face.
MARKS = {
    'cecilia': [
        ((357,150,792,490),40,(454,295),(676,449),(477,455),(645,229)),
        ((800,112,1135,490),43,(1066,214),(1006,302),(1058,452),(1022,194)),
        ((1170,18,1490,488),56,(1220,260),(1358,70),(1224,450),(1336,87)),
        ((24,685,363,980),34,(98,810),(174,805),(251,935),(133,781)),
        ((354,510,762,970),50,(393,681),(687,681),(670,925),(525,566)),
        ((787,510,1154,978),49,(850,790),(1037,717),(1104,940),(924,570)),
        ((1170,570,1485,980),42,(1326,717),(1381,735),(1421,943),(1356,631)),
    ],
    'adela': [
        ((352,152,776,485),42,(538,446),(729,251),(736,450),(628,274)),
        ((789,80,1148,482),49,(1053,232),(958,368),(1089,447),(1016,210)),
        ((1175,12,1487,483),60,(1236,246),(1360,64),(1380,449),(1328,82)),
        ((12,682,330,988),36,(283,807),(261,823),(162,946),(264,803)),
        ((344,502,757,966),53,(398,665),(703,666),(619,909),(537,558)),
        ((777,518,1153,994),51,(840,824),(1064,744),(1106,938),(966,600)),
        ((1185,562,1517,977),47,(1418,724),(1381,749),(1331,939),(1396,631)),
    ],
}


def pack(name, marks):
    source = Image.open(HERE / (name + '.png')).convert('RGB')
    atlas = Image.new('RGBA', (128 * len(marks), 96))
    bg = np.median(np.asarray(source)[:12, :64], axis=(0, 1))
    poses = []
    for i, (box, height, *points) in enumerate(marks):
        rgb = source.crop(box)
        # The plate is almost flat; preserve the dark cloak and armour on both sides of its value.
        distance = np.max(np.abs(np.asarray(rgb, dtype=float) - bg), axis=2)
        mask = Image.fromarray(np.uint8(distance > 14) * 255).filter(ImageFilter.MedianFilter(3))
        bounds = mask.getbbox()
        rgba = rgb.convert('RGBA')
        rgba.putalpha(mask)
        rgba = rgba.crop(bounds)
        size = (round(rgba.width * height / rgba.height), height)
        small = rgba.resize(size, Image.Resampling.LANCZOS)
        alpha = small.getchannel('A').point(lambda a: 255 if a >= 136 else 0)
        colours = small.convert('RGB').filter(ImageFilter.UnsharpMask(1, 115, 3))
        small = colours.quantize(colors=64, dither=Image.Dither.NONE).convert('RGBA')
        small.putalpha(alpha)
        x0, y0 = 64 - size[0] // 2, 90 - height
        atlas.paste(small, (128 * i + x0, y0), alpha)
        def pixel(point):
            return dict(x=round(x0+(point[0]-box[0]-bounds[0])*size[0]/rgba.width),
                        y=round(y0+(point[1]-box[1]-bounds[1])*height/rgba.height))
        poses.append(dict(zip(('left','right','boot','face'),map(pixel,points)), height=height))
    atlas.save(EDITOR / (name + '_boss_pose_keys.png'))
    return poses


if __name__ == '__main__':
    data = {name: pack(name, marks) for name, marks in MARKS.items()}
    (EDITOR / 'BossPoseMarks.json').write_text(json.dumps(data, indent=2)+'\n', encoding='utf-8')
