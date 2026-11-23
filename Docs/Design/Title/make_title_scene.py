"""시작 화면 도트 풍경 640x360 (화면에서 정확히 2배) — 스토리 A안 '의식의 밤'.

기존 게임 도트만 합친다: 보스 예배당 배경, 장미창의 성모 v4(1페이즈), 세실리아·아델라 대기 1칸.
새로 찍는 픽셀은 성모의 피눈물 자국, 제단에서 늘어진 끊어진 사슬 두 줄, 장미창 붉은 유리 강조, 위쪽 어둠뿐.
움직이는 것(피눈물 방울·불씨·장미창 맥동·사슬 섬광)은 SKTitleMenu 코드가 그린다.

실행: python Docs/Design/Title/make_title_scene.py  (프로젝트 루트에서)
출력: Assets/SaintKnightDay26/Resources/SaintKnightDay26/title_scene.png
"""
from pathlib import Path
from PIL import Image
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
CHAPEL = ROOT / "Assets/SaintKnightDay09/Art/boss_chapel.png"
MADONNA = ROOT / "Assets/SaintKnightDay09/Art/Boss/boss_body_p1_f00_NOT_FINAL.png"
CECILIA = ROOT / "Assets/SaintKnightDay06/Resources/SaintKnightDay06/cecilia_motion.png"
ADELA = ROOT / "Assets/SaintKnightDay06/Resources/SaintKnightDay06/adela_motion.png"
OUT = ROOT / "Assets/SaintKnightDay26/Resources/SaintKnightDay26/title_scene.png"

W, H = 640, 360
DY = 28                      # 예배당을 아래로 내려 위쪽 어둠에 로고 자리를 만든다
FLOOR = 310 + DY             # 바닥선 (예배당 원래 y310)
ROSE = (415, 115 + DY)       # 장미창 중심 — SKTitleMenu.RoseAt과 같아야 함
EYES = [(413, 201 + DY), (418, 201 + DY)]  # 성모 눈 — SKTitleMenu.TearAt과 같아야 함

DARK, MID, LIGHT = (24, 23, 30, 255), (96, 94, 104, 255), (168, 166, 176, 255)
BLOOD, BLOOD_HI = (112, 8, 14, 255), (176, 26, 30, 255)


def put(img, x, y, c):
    if 0 <= x < W and 0 <= y < H:
        img.putpixel((x, y), c)


def link_v(img, x, y, big):
    """세로 사슬 고리 하나(위쪽 y부터). big=정면 고리 3x4, 아니면 옆 고리 1x4."""
    if big:
        for yy in range(4):
            put(img, x - 1, y + yy, DARK if yy in (0, 3) else MID)
            put(img, x + 1, y + yy, DARK if yy in (0, 3) else LIGHT if yy == 1 else MID)
        put(img, x, y, MID)
        put(img, x, y + 3, MID)
    else:
        for yy in range(4):
            put(img, x, y + yy, LIGHT if yy == 1 else MID)
        put(img, x - 1, y + 1, DARK)
    return 3


def link_h(img, x, y, big):
    """가로 사슬 고리 하나(왼쪽 x부터), 바닥에 누운 것."""
    if big:
        for xx in range(4):
            put(img, x + xx, y - 1, DARK if xx in (0, 3) else MID)
            put(img, x + xx, y + 1, DARK if xx in (0, 3) else MID)
        put(img, x, y, MID)
        put(img, x + 3, y, MID)
        put(img, x + 1, y - 1, LIGHT)
    else:
        for xx in range(4):
            put(img, x + xx, y, LIGHT if xx == 1 else MID)
        put(img, x + 1, y + 1, DARK)
    return 3


def broken_link(img, x, y):
    """벌어진 고리(끊어진 끝): 위가 열린 C."""
    for yy in range(1, 4):
        put(img, x - 1, y + yy, MID)
    put(img, x + 1, y + 3, MID)
    put(img, x + 1, y + 2, LIGHT)
    put(img, x, y + 4, DARK)
    put(img, x + 2, y, LIGHT)      # 튕겨 나간 조각


def chain(img, top_x, top_y, drop_to, run, broken_at_end=True):
    """제단 끝에서 바닥까지 늘어지고(drop) 바닥을 따라 run만큼 눕는다(음수 = 왼쪽)."""
    y, big = top_y, True
    while y < drop_to - 3:
        y += link_v(img, top_x, y, big)
        big = not big
    step = -3 if run < 0 else 3
    x = top_x + (-4 if run < 0 else 1)
    for _ in range(abs(run) // 3):
        link_h(img, x, drop_to, big)
        big = not big
        x += step
    if broken_at_end:
        broken_link(img, x + (2 if run < 0 else 0), drop_to - 4)


BRIGHT = (206, 204, 214, 255)
OVAL = [".####.",
        "#+..+#",
        "#....#",
        ".#++#."]
SIDE = ["####",
        "#++#"]
OPEN = [".##...",      # 끊어져 벌어진 마지막 고리 + 튕겨 나간 조각
        "#..+..#",
        "#.....",
        ".#++#."]


def stamp(img, x, y, rows):
    for j, row in enumerate(rows):
        for i, ch in enumerate(row):
            if ch == "#":
                put(img, x + i, y + j, MID)
            elif ch == "+":
                put(img, x + i, y + j, BRIGHT)
    # 아래 그림자
    for i in range(len(rows[0])):
        put(img, x + i, y + len(rows), DARK)


def floor_chain(img, x_from, x_to, base):
    """바닥에 끌린 굵은 사슬: 오른쪽(살덩이)에서 왼쪽으로, 끝은 벌어진 고리."""
    x, big = x_from, True
    while x - 6 > x_to:
        if big:
            x -= 5
            stamp(img, x, base - 4, OVAL)
        else:
            x -= 3
            stamp(img, x, base - 3, SIDE)
        big = not big
    stamp(img, x - 7, base - 4, OPEN)


def cell(sheet, i, flip=False):
    c = sheet.crop((i * 128, 0, i * 128 + 128, 96))
    return c.transpose(Image.FLIP_LEFT_RIGHT) if flip else c


def main():
    img = Image.new("RGBA", (W, H), (0, 0, 0, 255))
    chapel = Image.open(CHAPEL).convert("RGBA")
    img.alpha_composite(chapel, (0, DY))

    # 장미창 붉은 유리를 조금 더 붉게 (빛은 코드가 맥동시킨다)
    a = np.array(img).astype(np.float32)
    yy, xx = np.mgrid[0:H, 0:W]
    inside = (xx - ROSE[0]) ** 2 + (yy - ROSE[1]) ** 2 < 78 ** 2
    red = inside & (a[..., 0] > a[..., 1] * 1.35) & (a[..., 0] > 50)
    a[red, 0] = np.minimum(255, a[red, 0] * 1.45)
    a[red, 1] *= .8
    a[red, 2] *= .8
    # 위쪽 어둠 (궁륭이 어둠 속으로 사라짐 — 로고 자리)
    fade = np.clip((yy - DY) / 70.0, 0, 1)[..., None]
    a[..., :3] *= fade * .85 + .15 * (yy >= DY)[..., None] * fade
    img = Image.fromarray(a.clip(0, 255).astype(np.uint8), "RGBA")

    img.alpha_composite(Image.open(MADONNA).convert("RGBA"), (ROSE[0] - 128, FLOOR - 300))

    # 끊어진 사슬(성모 앞): 오른쪽 제단 기둥에서 늘어진 줄 + 살덩이에서 세실리아 발치까지 바닥에 끌린 굵은 줄
    chain(img, 457, 268 + DY, FLOOR - 2, 0, broken_at_end=False)
    floor_chain(img, 352, 298, FLOOR - 1)

    # 피눈물 자국, 1픽셀 폭 (방울은 코드)
    for ex, ey in EYES:
        for d in range(7):
            put(img, ex, ey + d, BLOOD)

    # 제단 앞: 아델라는 성모를 향해 방패를 들고, 세실리아는 등을 돌려 문 쪽으로
    adela = Image.open(ADELA).convert("RGBA")
    cecilia = Image.open(CECILIA).convert("RGBA")
    img.alpha_composite(cell(adela, 0), (282, FLOOR - 88))
    img.alpha_composite(cell(cecilia, 0, flip=True), (236, FLOOR - 88))

    OUT.parent.mkdir(parents=True, exist_ok=True)
    img.save(OUT)
    print(OUT, img.size)


if __name__ == "__main__":
    main()
