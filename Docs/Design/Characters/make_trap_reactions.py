"""Day 14 임시 함정 반응 도트 (2026-11-12).

기존 동작 칸(피격·달리기·대기)에 점액·찌름 자국을 덧그려 아틀라스 맨 아래에 한 줄(9칸)을 붙인다.
캐릭터 그림 자체는 건드리지 않는다. 정식 도트가 오면 같은 칸 번호에 덮어 그리면 된다
(TRAP_REACTION_GUIDE.md). 다시 실행해도 기존 줄만 남기고 새로 붙이므로 결과가 같다.

    python make_trap_reactions.py            # 게임 아틀라스에 적용 + 미리보기
    python make_trap_reactions.py --out DIR  # DIR에만 출력 (게임 파일 그대로)
"""
import sys
from pathlib import Path
from PIL import Image

HERE = Path(__file__).resolve().parent
RES = HERE.parents[2] / "Assets/SaintKnightDay06/Resources/SaintKnightDay06"
CW, CH, COLS = 128, 96, 12
FLOOR = 93  # 발 아래 첫 빈 줄 (아래 여백 3px)
SLIME = [(49, 58, 34), (78, 87, 48), (113, 122, 59), (154, 171, 85), (197, 206, 121)]  # slime_pool 팔레트, 어두움→밝음
JAB = [(113, 49, 63), (173, 88, 96), (220, 138, 123)]  # floor_tentacle 팔레트
LIFT = [2, 5, 2]  # 찔림 3장: 몸이 위로 들리는 높이(px)

# 이름: (원래 줄 수, 달리기 4칸, 대기 2칸, 피격 3칸)
CHARS = {
    "cecilia": (6, [4, 7, 10, 13], [0, 2], [27, 28, 29]),
    "adela": (7, [6, 9, 13, 16], [0, 3], [33, 34, 35]),
}


def cell(atlas, index):
    x, y = index % COLS * CW, index // COLS * CH
    return atlas.crop((x, y, x + CW, y + CH))


def feet(img):
    """발 근처(아래 5줄)의 좌우 범위."""
    box = img.crop((0, FLOOR - 5, CW, FLOOR)).getbbox()
    return (box[0], box[2] - 1) if box else (CW // 2 - 6, CW // 2 + 6)


def slimed(src, phase):
    """발목까지 점액에 잠긴 모습: 아래 7줄을 점액색으로, 발밑에 출렁이는 웅덩이."""
    img = src.copy()
    px = img.load()
    for y in range(FLOOR - 7, FLOOR):
        ragged = y < FLOOR - 5  # 윗 경계는 들쭉날쭉하게
        for x in range(CW):
            r, g, b, a = px[x, y]
            if a == 0 or (ragged and (x + y + phase) % 2):
                continue
            px[x, y] = SLIME[min(4, (r * 3 + g * 6 + b) // 10 * 5 // 256 + 1)] + (255,)
    x0, x1 = feet(src)
    wobble = phase % 2
    for x in range(x0 - 3 - wobble, x1 + 4 + wobble):
        if 0 <= x < CW:
            px[x, FLOOR - 1] = SLIME[4 if (x + phase) % 5 == 0 else 2] + (255,)
            if x0 - 2 <= x <= x1 + 2:
                px[x, FLOOR] = SLIME[0] + (255,)
    return img


def jabbed(src, lift):
    """아래에서 찔려 들린 모습: 몸을 위로 올리고 발밑에 찌름 자국."""
    img = Image.new("RGBA", (CW, CH))
    img.paste(src.crop((0, lift, CW, CH)), (0, 0))
    px = img.load()
    x0, x1 = feet(src)
    mid = (x0 + x1) // 2
    for i, x in enumerate((mid - 5, mid, mid + 5)):
        height = lift + 3 - (i != 1)
        for h in range(height):
            y = FLOOR - 1 - h
            if px[x, y][3] == 0:  # 몸 뒤로만
                px[x, y] = JAB[min(2, h * 3 // height)] + (255,)
    return img


def build(name):
    rows, run, idle, hurt = CHARS[name]
    atlas = Image.open(RES / f"{name}_motion.png").convert("RGBA")
    base = atlas.crop((0, 0, COLS * CW, rows * CH))
    cells = [jabbed(cell(base, c), LIFT[i]) for i, c in enumerate(hurt)]
    cells += [slimed(cell(base, c), i) for i, c in enumerate(run)]
    cells += [slimed(cell(base, c), i) for i, c in enumerate(idle)]
    out = Image.new("RGBA", (COLS * CW, (rows + 1) * CH))
    out.paste(base, (0, 0))
    for i, img in enumerate(cells):
        out.paste(img, (i * CW, rows * CH))
    first = rows * COLS
    print(f"{name}: SpikeHurt {first}..{first + 2}, SlimeWalk {first + 3}..{first + 6}, SlimeIdle {first + 7}..{first + 8}")
    return out, cells


def main():
    out_dir = Path(sys.argv[sys.argv.index("--out") + 1]) if "--out" in sys.argv else RES
    strips = []
    for name in CHARS:
        atlas, cells = build(name)
        atlas.save(out_dir / f"{name}_motion.png")
        strip = Image.new("RGBA", (CW * len(cells), CH), (40, 40, 48, 255))
        for i, img in enumerate(cells):
            strip.alpha_composite(img, (i * CW, 0))
        strips.append(strip)
    preview = Image.new("RGBA", (strips[0].width, CH * len(strips)))
    for i, strip in enumerate(strips):
        preview.paste(strip, (0, i * CH))
    preview = preview.resize((preview.width * 3, preview.height * 3), Image.NEAREST)
    preview_dir = HERE if out_dir == RES else out_dir
    preview.save(preview_dir / "trap_reactions_preview.png")


if __name__ == "__main__":
    main()
