# 로비(거점) v2 — 배경·소품만. 고밀도 다크 고딕 도트.
# 방식: 고유색(albedo)을 그림 → 빛 지도(환경광+촛불+달빛)를 곱함 → 발광체(유리·불꽃) 합성
#       → 제한 팔레트로 Bayer 디더 양자화. 원본 640x360, 미리보기 x3 = 1920x1080.
# 실행: python Docs/Design/Lobby/make_lobby_v2.py
import math, random
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

W, H, FLOOR = 640, 360, 282
OUT = Path(__file__).parent / 'v2'
(OUT / 'props').mkdir(parents=True, exist_ok=True)


def hexc(h):
    return tuple(int(h[i:i + 2], 16) for i in (1, 3, 5)) + (255,)


A = {k: hexc(v) for k, v in dict(
    mortar='#2a2430', brick0='#4b4256', brick1='#554b61', brick2='#5f546c', brick3='#6a5e78',
    hole='#14111a', crack='#1c1822', stoneL='#8a7f9c', stone='#6e6382', stoneD='#4a4258', stoneDD='#332d3e',
    moss0='#2f4426', moss1='#4a6236',
    wood0='#3a2216', wood1='#5a3622', wood2='#7a4c30', wood3='#9a6640', grain='#462a1b',
    iron0='#1a1820', iron1='#2e2b36', iron2='#4a4656', iron3='#6c6878',
    gold0='#7a5418', gold1='#b8862e', gold2='#e0b04a', gold3='#ffe08a',
    crim0='#4a0f18', crim1='#7a1828', crim2='#a82a3a', crim3='#d0505a',
    steel0='#4a4e60', steel1='#7a8098', steel2='#b0b8cc', steel3='#e4e8f2',
    parch0='#8c7652', parch1='#c0a87c', parch2='#e2d0a6', ink='#4a2a20', river='#3a6a9a', forest='#2e4a2a',
    wax0='#b8ac94', wax1='#ece2cc', flame0='#ff8a2a', flame1='#ffc04a', flame2='#fff0b8',
    glassD='#0e1c3c', glass1='#173264', glass2='#22508e', glass3='#3a74b6', glass4='#6aa2dc', glass5='#a8d0f4',
    night='#060a16', lead='#0a0a12', fgk='#06050a', fgr='#1e1a28',
).items()}

PALETTE = '''
#06050a #0a0a12 #0d0b13 #14111c #1c1826 #252032 #2f2940 #3b3350 #4a4162 #5c5276 #72688c #8c82a4
#1a100c #2a1912 #3d241a #553223 #6e432c #8a5836 #a8703f #c98d4c
#3a2c30 #4e3a3a #6a4e46 #8a6a58 #b08a6c
#2e3448 #45506a #66779a
#5a3d14 #8c6420 #b8862e #d9a840 #f0cc6a #fff0b0
#2a070d #45101a #6a1626 #8f1f32 #b8303f #d0505a
#060a16 #0b1428 #11234a #1b3a72 #2a5596 #3f76b8 #6a9fd8 #a6cdf2 #e2f1ff
#ff8a2a #ffb84a #ffd36e #fff4c8
#6d5a3e #9c8660 #cbb48a #e6d6b0
#2b2e3b #474c5e #6c7288 #a1a8bd #d6dbea
#10180f #1a2a17 #2a3f22 #3d5530
'''.split()

rng = random.Random(11)


class Layer:
    def __init__(s):
        s.im = Image.new('RGBA', (W, H), (0, 0, 0, 0))
        s.g = ImageDraw.Draw(s.im)

    def r(s, x0, y0, x1, y1, k):
        s.g.rectangle([x0, y0, x1, y1], fill=A[k])

    def p(s, x, y, k):
        if 0 <= x < W and 0 <= y < H:
            s.g.point((x, y), fill=A[k])

    def l(s, pts, k, w=1):
        s.g.line(pts, fill=A[k], width=w)

    def clear(s, x, y):
        s.g.point((x, y), fill=(0, 0, 0, 0))


def arch_y(x, x0, x1, spring, apex):
    hw = (x1 - x0) / 2
    t = min(1.0, abs(x - (x0 + x1) / 2) / hw)
    return spring - math.sqrt(max(0.0, 1 - t)) * (spring - apex)


def in_arch(x, y, x0, x1, spring, apex, bottom):
    return x0 <= x <= x1 and arch_y(x, x0, x1, spring, apex) <= y <= bottom


def crack_walk(L, x, y, n, k='crack', hi='stoneL', dx=(-1, 0, 1), dy=(1,)):
    for _ in range(n):
        L.p(x, y, k)
        if rng.random() < 0.5:
            L.p(x + 1, y + 1, hi)
        x += rng.choice(dx)
        y += rng.choice(dy)


EMI = Layer()   # 빛을 받지 않는 발광체 (유리, 불꽃, 열쇠구멍)
LIGHTS = []     # (x, y, r, 세기, 색)
WARM, MOON = (1.0, 0.62, 0.32), (0.55, 0.72, 1.0)


def flame(L, x, y, size=1):
    pts = [(x, y - 1, 'flame0'), (x, y - 2, 'flame1'), (x, y - 3, 'flame2')]
    if size > 1:
        pts += [(x - 1, y - 2, 'flame0'), (x + 1, y - 2, 'flame0'), (x - 1, y - 3, 'flame1'),
                (x, y - 4, 'flame1'), (x, y - 5, 'flame2'), (x + 1, y - 4, 'flame0')]
    for px_, py_, k in pts:
        EMI.p(px_, py_, k); L.p(px_, py_, k)


def candle(L, x, top, h, w=2):
    L.r(x, top, x + w - 1, top + h, 'wax1')
    L.r(x + w - 1, top, x + w - 1, top + h, 'wax0')
    L.p(x, top, 'wax0')
    if rng.random() < 0.7:
        for yy in range(top + 1, top + 1 + rng.randint(2, max(2, h))):
            L.p(x - 1, yy, 'wax1')
    flame(L, x + (w - 1) // 2, top, 2 if h > 8 else 1)


# =========================================================== 배경 (벽·천장·창·바닥)
def draw_background():
    B = Layer()
    B.r(0, 0, W, FLOOR, 'mortar')
    for row, y in enumerate(range(0, FLOOR, 8)):
        off = 8 if row % 2 else 0
        for x in range(-16 + off, W, 16):
            if rng.random() < 0.015:
                B.r(x + 1, y + 1, x + 15, y + 7, 'hole')
                B.l([x + 1, y + 1, x + 15, y + 1], 'crack')
                continue
            k = rng.choice(['brick0', 'brick1', 'brick1', 'brick2', 'brick2', 'brick3'])
            B.r(x + 1, y + 1, x + 15, y + 7, k)
            B.l([x + 1, y + 1, x + 14, y + 1], 'brick3' if k != 'brick3' else 'stone')
            B.l([x + 2, y + 7, x + 15, y + 7], 'brick0' if k != 'brick0' else 'stoneD')
            for _ in range(rng.randint(0, 3)):  # 표면 얼룩
                B.p(x + rng.randint(2, 14), y + rng.randint(2, 6), rng.choice(['brick0', 'brick3', 'stoneD']))
            if rng.random() < 0.18:  # 깨진 모서리
                cx, cy = rng.choice([(x + 1, y + 1), (x + 15, y + 1), (x + 1, y + 7), (x + 15, y + 7)])
                B.p(cx, cy, 'mortar'); B.p(cx + (1 if cx == x + 1 else -1), cy, 'stoneDD')
    for _ in range(9):  # 큰 균열
        crack_walk(B, rng.randint(10, W - 10), rng.randint(70, 200), rng.randint(18, 50))

    # 천장 볼트
    bays = [(-160, 160, 60, 18), (160, 480, 60, 4), (480, 800, 60, 18)]
    im = np.asarray(B.im).copy()
    for x in range(W):
        for x0, x1, sp, ap in bays:
            if x0 <= x <= x1:
                ya = int(arch_y(x, x0, x1, sp, ap))
                im[:max(0, ya), x, :3] = (im[:max(0, ya), x, :3] * 0.45).astype(np.uint8)
                for i, k in enumerate(['stoneL', 'stone', 'stone', 'stoneD', 'stoneDD', 'crack']):
                    if 0 <= ya + i < H:
                        im[ya + i, x] = A[k]
    B.im = Image.fromarray(im, 'RGBA'); B.g = ImageDraw.Draw(B.im)

    # 창 아래 물자국·이끼
    for x in range(268, 374):
        if rng.random() < 0.35:
            length = rng.randint(10, 60)
            for y in range(219, 219 + length):
                if rng.random() < 0.75:
                    r_, g_, b_, a_ = B.im.getpixel((x, y))
                    B.g.point((x, y), fill=(int(r_ * 0.78), int(g_ * 0.8), int(b_ * 0.8), 255))
    for x in range(W):
        top = FLOOR - int(abs(math.sin(x * 0.045) * 14 + math.sin(x * 0.13) * 6))
        for y in range(top, FLOOR):
            if rng.random() < 0.45 * (y - top) / max(1, FLOOR - top) + 0.1:
                B.p(x, y, 'moss0' if rng.random() < 0.7 else 'moss1')

    # 기둥
    roll = ['stoneD', 'stoneL', 'stoneL', 'stone', 'stone', 'stone', 'stoneD', 'stoneDD']
    for cx in (160, 480):
        for x in range(cx - 12, cx + 12):
            B.l([x, 0, x, FLOOR], roll[(x - cx + 12) % 8])
        B.r(cx - 19, 52, cx + 19, 55, 'stone'); B.l([cx - 19, 52, cx + 19, 52], 'stoneL'); B.l([cx - 19, 55, cx + 19, 55], 'stoneDD')
        for y in range(56, 67):
            w = 17 - (y - 56) * 5 // 10
            B.r(cx - w, y, cx + w, y, 'stone')
            B.p(cx - w, y, 'stoneL'); B.p(cx + w, y, 'stoneDD')
        for lx in range(cx - 14, cx + 15, 7):  # 잎 장식
            B.g.ellipse([lx - 3, 58, lx + 3, 64], fill=A['stoneL'], outline=A['stoneD'])
            B.p(lx, 61, 'stoneD')
        B.r(cx - 16, 262, cx + 16, 267, 'stone'); B.l([cx - 16, 262, cx + 16, 262], 'stoneL'); B.l([cx - 16, 267, cx + 16, 267], 'stoneDD')
        B.r(cx - 18, 268, cx + 18, FLOOR, 'stoneD'); B.l([cx - 18, 268, cx + 18, 268], 'stone')
        for _ in range(3):
            crack_walk(B, cx + rng.randint(-10, 10), rng.randint(80, 230), rng.randint(8, 22), dx=(-1, 0, 0, 1))
        for _ in range(6):
            B.p(cx + rng.randint(-12, 11), rng.randint(70, 250), 'mortar')
        for y in range(FLOOR - 26, FLOOR):  # 기둥 이끼
            for x in range(cx - 18, cx + 19):
                if rng.random() < 0.25 * (y - FLOOR + 26) / 26:
                    B.p(x, y, 'moss0' if rng.random() < 0.6 else 'moss1')

    # 스테인드글라스 창 (테두리·트레이서리는 배경, 유리는 발광)
    X0, X1, SP, AP, BOT = 268, 372, 104, 44, 212
    LL, LR = (280, 316), (324, 360)
    moon = (348, 96)
    for y in range(AP - 8, BOT + 1):
        for x in range(X0 - 7, X1 + 8):
            k = sum(in_arch(x, y, X0 - m, X1 + m, SP, AP - m, BOT) for m in (6, 4, 2, 0))
            if k == 0:
                continue
            if k < 4:
                B.p(x, y, {1: 'stone' if x < 320 else 'stoneD', 2: 'stoneL', 3: 'stoneDD'}[k])
                continue
            inL = in_arch(x, y, LL[0], LL[1], 128, 100, 206)
            inR = in_arch(x, y, LR[0], LR[1], 128, 100, 206)
            rose = math.hypot(x - 320, y - 72)
            if not (inL or inR or rose <= 21):
                B.p(x, y, 'stoneD')  # 트레이서리 석재 (역광)
                continue
            if rose <= 21:
                ang = (math.degrees(math.atan2(y - 72, x - 320)) + 360) % 360
                if rose > 19:
                    kk = 'gold1'
                elif rose < 3.5:
                    kk = 'gold3'
                elif min(ang % 45, 45 - ang % 45) < 3 or 6 < rose < 7:
                    kk = 'lead'
                else:
                    kk = 'crim2' if int(ang // 45) % 2 else ('glass4' if rose < 12 else 'glass3')
                EMI.p(x, y, kk); continue
            lo, hi = (LL if inL else LR)
            edge = not in_arch(x, y, lo + 1, hi - 1, 128, 101, 205)
            band = not in_arch(x, y, lo + 4, hi - 4, 128, 104, 202)
            dist = math.hypot(x - moon[0], (y - moon[1]) * 0.7) + ((x + y) % 2) * 4
            kk = ('glass5' if dist < 26 else 'glass4' if dist < 46 else 'glass3' if dist < 70
                  else 'glass2' if dist < 98 else 'glass1')
            if (x + y) % 7 == 0 or (x - y) % 7 == 0:
                kk = {'glass5': 'glass4', 'glass4': 'glass3', 'glass3': 'glass2', 'glass2': 'glass1', 'glass1': 'glassD'}[kk]
            if band:
                kk = 'crim2' if (y // 3) % 2 else 'crim1'
            if edge or y in (160, 182):
                kk = 'lead'
            EMI.p(x, y, kk)
    # 왼쪽 창: 장검 / 오른쪽 창: 사슬과 성호
    sx = 298
    E = EMI
    E.g.ellipse([sx - 3, 110, sx + 3, 116], fill=A['gold2'], outline=A['lead'])
    E.r(sx - 1, 117, sx + 1, 125, 'crim1')
    E.r(sx - 10, 126, sx + 10, 128, 'gold2'); E.p(sx - 10, 126, 'gold3'); E.p(sx + 10, 126, 'gold3'); E.l([sx - 10, 129, sx + 10, 129], 'lead')
    for y in range(130, 198):
        w = 2 if y < 190 else max(0, (197 - y) // 3)
        E.r(sx - w, y, sx + w, y, 'steel2'); E.p(sx, y, 'steel3')
        E.p(sx - w - 1, y, 'lead'); E.p(sx + w + 1, y, 'lead')
    cx = 342
    for i, y in enumerate(range(108, 172, 8)):
        if i % 2 == 0:
            E.g.ellipse([cx - 2, y, cx + 2, y + 7], outline=A['gold2'])
        else:
            E.g.ellipse([cx - 3, y + 2, cx + 3, y + 5], outline=A['gold1'])
    E.r(cx - 1, 176, cx + 1, 198, 'gold2'); E.r(cx - 6, 182, cx + 6, 184, 'gold2'); E.p(cx, 183, 'gold3')
    # 깨진 유리
    for y in range(188, 207):
        for x in range(344, 361):
            if in_arch(x, y, *LR, 128, 100, 206) and math.hypot(x - 357, y - 202) < 6 - rng.random() * 3:
                E.p(x, y, 'night')
    for x in (350, 353, 357, 361, 364):
        E.p(x, 213 + rng.randint(0, 1), 'glass5')
    B.r(262, 212, 378, 218, 'stone'); B.l([262, 212, 378, 212], 'stoneL'); B.l([262, 218, 378, 218], 'stoneDD')
    B.l([264, 219, 376, 219], 'crack')
    LIGHTS.append((320, 128, 170, 0.5, MOON))

    # 바닥
    B.r(0, FLOOR, W, H, 'stoneD')
    B.l([0, FLOOR, W, FLOOR], 'stoneL'); B.l([0, FLOOR + 1, W, FLOOR + 1], 'stone'); B.l([0, FLOOR + 2, W, FLOOR + 2], 'stoneDD')
    for (y0, y1, step) in ((285, 300, 30), (301, 324, 44), (325, 359, 62)):
        x = -rng.randint(0, step)
        while x < W:
            w = step + rng.randint(-6, 6)
            k = rng.choice(['stoneD', 'stoneD', 'stone', 'stoneDD'])
            B.r(x + 1, y0 + 1, x + w - 1, y1 - 1, k)
            B.l([x + 1, y0 + 1, x + w - 1, y0 + 1], 'stone' if k != 'stone' else 'stoneL')
            B.l([x, y0, x, y1], 'crack')
            for _ in range(rng.randint(2, 6)):
                B.p(x + rng.randint(2, w - 2), rng.randint(y0 + 2, y1 - 1), rng.choice(['stoneDD', 'stone', 'crack']))
            if rng.random() < 0.25:
                crack_walk(B, x + rng.randint(4, w - 4), y0 + 1, rng.randint(4, y1 - y0), hi='stone')
            x += w
        B.l([0, y1, W, y1], 'crack')
    for _ in range(40):  # 잔해
        x = rng.choice([rng.randint(130, 200), rng.randint(440, 520), rng.randint(250, 380), rng.randint(0, W)])
        y = rng.randint(FLOOR + 3, 330)
        s = rng.randint(1, 3)
        B.r(x, y, x + s, y + s - 1, rng.choice(['stone', 'stoneL', 'brick2']))
        B.l([x, y + s, x + s, y + s], 'crack')
    return B


# =========================================================== 소품
def prop_altar():
    L = Layer()
    L.r(40, 245, 128, FLOOR - 1, 'stone'); L.l([40, 245, 40, FLOOR - 1], 'stoneL'); L.r(124, 245, 128, FLOOR - 1, 'stoneD')
    for ax in (44, 112):  # 측면 블라인드 아치
        for y in range(252, 276):
            for x in range(ax, ax + 13):
                if in_arch(x, y, ax, ax + 12, 258, 250, 275):
                    L.p(x, y, 'stoneDD' if in_arch(x, y, ax + 1, ax + 11, 259, 252, 274) else 'stoneL')
    L.r(34, 238, 134, 245, 'stone'); L.l([34, 238, 134, 238], 'stoneL'); L.l([34, 245, 134, 245], 'stoneDD')
    L.l([36, 246, 132, 246], 'crack')
    L.r(36, FLOOR - 4, 132, FLOOR - 1, 'stoneD'); L.l([36, FLOOR - 4, 132, FLOOR - 4], 'stone')
    # 제단보
    for x in range(62, 107):
        f = math.sin((x - 62) * 0.55)
        k = 'crim3' if f > 0.7 else 'crim2' if f > 0 else 'crim1' if f > -0.7 else 'crim0'
        bottom = 274 + (1 if x % 3 == 0 else 0)
        L.l([x, 247, x, bottom], k)
        if x % 2 == 0:
            L.p(x, bottom + 1, 'gold1')
    L.r(62, 246, 106, 249, 'gold1'); L.l([62, 246, 106, 246], 'gold2'); L.l([62, 250, 106, 250], 'crim0')
    L.r(83, 254, 85, 268, 'gold2'); L.r(78, 258, 90, 260, 'gold2'); L.p(84, 259, 'gold3')
    L.p(83, 254, 'gold3'); L.p(78, 258, 'gold3')
    # 성유물함·책
    L.r(78, 228, 90, 237, 'gold1'); L.l([78, 228, 90, 228], 'gold3'); L.r(79, 230, 89, 236, 'gold0')
    L.r(82, 231, 86, 235, 'crim1'); L.p(84, 233, 'crim3'); L.r(83, 223, 85, 227, 'gold2'); L.r(81, 224, 87, 225, 'gold2')
    L.r(46, 234, 64, 237, 'parch1'); L.l([55, 233, 55, 237], 'parch0'); L.l([46, 237, 64, 237], 'wood1')
    for x in range(48, 54, 2):
        L.p(x, 235, 'ink'); L.p(x + 9, 235, 'ink')
    # 초 무더기
    for x, h, w in ((36, 12, 2), (39, 6, 3), (43, 16, 2), (47, 9, 2), (67, 5, 2), (96, 7, 2), (114, 10, 3), (118, 18, 2), (122, 7, 2), (126, 13, 2), (130, 5, 2)):
        candle(L, x, 237 - h, h, w)
    for x, h in ((138, 4), (143, 7)):
        candle(L, x, FLOOR - 1 - h, h)
    LIGHTS.extend([(44, 222, 120, 1.15, WARM), (122, 222, 120, 1.15, WARM)])
    return L


def prop_wall_cross():
    L = Layer()
    cx = 84
    L.r(cx - 3, 112, cx + 3, 206, 'gold1'); L.r(cx - 22, 134, cx + 22, 140, 'gold1')
    L.l([cx - 3, 112, cx - 3, 206], 'gold2'); L.l([cx - 22, 134, cx + 22, 134], 'gold2')
    L.l([cx + 3, 112, cx + 3, 206], 'gold0'); L.l([cx - 22, 140, cx + 22, 140], 'gold0')
    L.l([cx - 1, 116, cx - 1, 200], 'gold3')
    for ex, ey in ((cx, 109), (cx - 25, 137), (cx + 25, 137), (cx, 209)):  # 봉오리 끝
        L.g.ellipse([ex - 4, ey - 4, ex + 4, ey + 4], fill=A['gold1'], outline=A['gold0'])
        L.p(ex - 1, ey - 1, 'gold3')
    L.g.ellipse([cx - 9, 128, cx + 9, 146], outline=A['gold2'])
    L.r(cx - 2, 135, cx + 2, 139, 'crim2'); L.p(cx, 137, 'crim3')
    return L


def prop_candelabra():
    L = Layer()
    L.r(23, 214, 25, 274, 'iron1'); L.l([23, 214, 23, 274], 'iron2')
    for y in (232, 252):
        L.r(21, y, 27, y + 2, 'iron2')
    L.g.polygon([(15, FLOOR - 1), (24, 272), (33, FLOOR - 1)], fill=A['iron1']); L.l([16, FLOOR - 1, 32, FLOOR - 1], 'iron2')
    L.l([13, 212, 35, 212], 'iron2', 2); L.l([13, 208, 13, 212], 'iron2'); L.l([35, 208, 35, 212], 'iron2')
    for x, h in ((12, 8), (23, 12), (34, 6)):
        L.r(x - 2, 207 if x != 23 else 211, x + 2, 208 if x != 23 else 212, 'iron3')
        candle(L, x - 1 if x != 23 else 23, (207 if x != 23 else 211) - h, h)
    LIGHTS.append((24, 200, 85, 0.85, WARM))
    return L


def prop_sconce(cx):
    L = Layer()
    L.r(cx - 1, 168, cx + 1, 178, 'iron1'); L.p(cx, 179, 'iron2')
    L.g.polygon([(cx - 6, 162), (cx + 6, 162), (cx + 3, 168), (cx - 3, 168)], fill=A['iron2'])
    L.l([cx - 6, 162, cx + 6, 162], 'iron3')
    flame(L, cx, 162, 2); flame(L, cx - 2, 162); flame(L, cx + 2, 162)
    EMI.p(cx, 156, 'flame1'); EMI.p(cx, 155, 'flame2')
    LIGHTS.append((cx, 156, 100, 1.0, WARM))
    return L


def prop_table():
    L = Layer()
    for lx in (320, 391):
        for y in range(256, FLOOR):
            w = 3 if (y - 256) % 10 in (3, 4, 5) else 2
            L.r(lx + 2 - w, y, lx + 2 + w, y, 'wood1')
            L.p(lx + 2 - w, y, 'wood2')
        L.r(lx - 1, FLOOR - 3, lx + 5, FLOOR - 1, 'wood0')
    L.r(324, 268, 392, 270, 'wood0'); L.l([324, 268, 392, 268], 'wood1')
    L.r(314, 241, 402, 244, 'wood3'); L.l([314, 241, 402, 241], 'parch0')
    L.r(314, 245, 402, 249, 'wood2'); L.r(316, 250, 400, 255, 'wood1'); L.l([316, 255, 400, 255], 'wood0')
    for x in range(318, 400, 9):
        L.l([x, 246, x + 4, 246], 'grain'); L.l([x + 3, 252, x + 6, 252], 'wood0')
    # 지도
    L.r(330, 237, 384, 241, 'parch1'); L.l([330, 237, 384, 237], 'parch2'); L.l([330, 241, 384, 241], 'parch0')
    L.r(368, 242, 384, 254, 'parch1'); L.l([384, 242, 384, 254], 'parch0'); L.r(368, 254, 384, 255, 'parch0')
    L.p(368, 255, 'parch2'); L.p(369, 255, 'parch2')
    for x in range(334, 380, 3):
        L.p(x, 239 + (x // 3) % 2, 'river')
    for x, y in ((336, 238), (338, 239), (340, 238), (372, 245), (375, 247), (378, 245), (374, 249)):
        L.p(x, y, 'forest')
    L.p(352, 238, 'stoneD'); L.p(352, 239, 'stoneD'); L.p(353, 238, 'stoneD')
    L.p(376, 251, 'crim2'); L.p(377, 252, 'crim2'); L.p(377, 251, 'crim2'); L.p(376, 252, 'crim2')
    # 꽂힌 단검
    L.r(360, 230, 361, 238, 'steel2'); L.p(361, 230, 'steel3')
    L.r(357, 228, 364, 229, 'gold1'); L.r(359, 222, 362, 227, 'wood1'); L.r(359, 220, 362, 221, 'gold2')
    # 두루마리, 잉크병, 깃펜
    for i, x in enumerate((318, 324)):
        L.r(x, 236 - i, x + 5, 240, 'parch1'); L.l([x + 5, 236 - i, x + 5, 240], 'parch0'); L.p(x, 236 - i, 'parch2')
    L.r(386, 236, 390, 240, 'iron0'); L.l([386, 236, 390, 236], 'iron2')
    for i in range(9):
        L.p(390 + i // 2, 235 - i, 'wax1' if i % 3 else 'wax0')
    # 촛대
    L.r(393, 238, 399, 240, 'gold1'); L.l([393, 238, 399, 238], 'gold2')
    candle(L, 395, 226, 11, 3)
    LIGHTS.append((396, 218, 105, 1.05, WARM))
    return L


def prop_rack():
    L = Layer()
    for px_ in (500, 553):
        L.r(px_, 214, px_ + 3, FLOOR - 1, 'wood1'); L.l([px_, 214, px_, FLOOR - 1], 'wood2')
        L.r(px_ - 3, FLOOR - 3, px_ + 6, FLOOR - 1, 'wood0')
    L.r(496, 218, 560, 221, 'wood2'); L.l([496, 218, 560, 218], 'wood3'); L.l([496, 221, 560, 221], 'wood0')
    L.r(500, 256, 556, 258, 'wood1')
    for x in (506, 534, 548):
        L.r(x, 214, x + 1, 217, 'iron2')
    for x in (501, 554):
        L.r(x, 220, x + 1, 222, 'iron3')
    # 장검
    sx = 512
    L.g.ellipse([sx - 2, 200, sx + 2, 204], fill=A['gold2'], outline=A['gold0'])
    L.r(sx - 1, 205, sx + 1, 213, 'wood1'); L.l([sx - 1, 205, sx - 1, 213], 'wood2')
    for y in range(206, 213, 2):
        L.l([sx - 1, y, sx + 1, y], 'grain')
    L.r(sx - 7, 214, sx + 7, 216, 'gold1'); L.l([sx - 7, 214, sx + 7, 214], 'gold2'); L.p(sx - 8, 215, 'gold0'); L.p(sx + 8, 215, 'gold0')
    for y in range(217, 266):
        w = 2 if y < 258 else max(0, (265 - y) // 3)
        L.r(sx - w, y, sx + w, y, 'steel1'); L.p(sx - w, y, 'steel2'); L.p(sx, y, 'steel3')
    # 연 방패 (흠집·찌그러짐)
    shield = [(522, 224), (548, 224), (548, 246), (535, 266), (522, 246)]
    L.g.polygon(shield, fill=A['steel0'])
    L.g.polygon([(524, 226), (546, 226), (546, 245), (535, 262), (524, 245)], fill=A['steel1'])
    L.g.line(shield + [shield[0]], fill=A['gold1'], width=2)
    L.l([524, 226, 546, 226], 'gold2')
    L.r(533, 229, 537, 255, 'crim1'); L.r(527, 236, 543, 240, 'crim1')
    L.l([533, 229, 533, 255], 'crim2'); L.l([527, 236, 543, 236], 'crim2')
    for x, y in ((529, 230), (541, 249), (538, 232), (528, 244)):
        L.p(x, y, 'steel0'); L.p(x + 1, y + 1, 'steel2')
    L.l([540, 228, 544, 236], 'steel2'); L.l([526, 248, 530, 252], 'steel2')
    L.l([525, 227, 525, 244], 'steel2')
    # 성유물 사슬
    for i, y in enumerate(range(222, 266, 2)):
        x = 558 + int(math.sin(i * 0.28) * 2)
        L.p(x, y, 'steel2' if i % 2 else 'steel1'); L.p(x + (1 if i % 2 else 0), y + 1, 'steel0')
    L.r(558, 266, 559, 274, 'gold2'); L.r(555, 268, 562, 269, 'gold2'); L.p(558, 266, 'gold3')
    return L


def prop_door():
    L = Layer()
    X0, X1, CX, CY, R = 578, 628, 603, 214, 25
    for y in range(CY - R - 7, FLOOR):
        for x in range(X0 - 6, X1 + 7):
            rr = math.hypot(x - CX, y - CY)
            inner = X0 <= x <= X1 and (y >= CY or rr <= R)
            frame = X0 - 6 <= x <= X1 + 6 and (y >= CY or rr <= R + 6)
            if inner:
                col = (x - X0) % 7
                k = 'wood0' if col == 0 else 'wood3' if col == 1 else 'wood2' if col < 4 else 'wood1'
                if rng.random() < 0.08:
                    k = 'grain'
                if y in (232, 233, 264, 265) or (y >= CY and abs(rr - R) < 1) or (y < CY and abs(rr - (R - 1)) < 1):
                    k = 'iron1'
                L.p(x, y, k)
            elif frame:
                band = (rr - R) if y < CY else (X0 - x if x < X0 else x - X1)
                L.p(x, y, 'stoneL' if band < 2 else 'stone' if band < 5 else 'stoneDD')
    L.r(CX - 4, CY - R - 8, CX + 4, CY - R - 1, 'stoneL'); L.l([CX - 4, CY - R - 1, CX + 4, CY - R - 1], 'stoneDD')  # 쐐기돌
    for y in (232, 264):
        for x in range(X0 + 2, X1, 6):
            L.p(x, y, 'iron3')
    L.g.ellipse([586, 247, 593, 254], outline=A['gold0']); L.p(589, 247, 'gold2')
    L.r(588, 244, 590, 246, 'iron2')
    EMI.p(591, 259, 'flame0'); EMI.p(591, 260, 'flame1'); L.p(591, 259, 'flame0'); L.p(591, 260, 'flame1')
    for x in range(X0 + 1, X1):
        EMI.p(x, FLOOR - 1, 'flame0' if x % 3 else 'flame1')
    LIGHTS.append((603, FLOOR + 2, 50, 0.9, (1.0, 0.55, 0.25)))
    return L


def prop_banner(x0, x1, y0, length, emblem):
    L = Layer()
    L.r(x0 - 4, y0 - 3, x1 + 4, y0 - 1, 'iron2'); L.l([x0 - 4, y0 - 3, x1 + 4, y0 - 3], 'iron3')
    L.g.ellipse([x0 - 7, y0 - 4, x0 - 3, y0], fill=A['gold1']); L.g.ellipse([x1 + 3, y0 - 4, x1 + 7, y0], fill=A['gold1'])
    off = 0
    for x in range(x0, x1 + 1):
        off = max(-8, min(8, off + rng.choice([-2, -1, 0, 1, 2])))
        notch = 10 - abs(x - (x0 + x1) // 2) if abs(x - (x0 + x1) // 2) < 10 else 0
        bottom = y0 + length + off - notch
        for y in range(y0, bottom):
            f = math.sin((x - x0) / (x1 - x0) * math.pi * 3 + y * 0.02)
            k = 'crim3' if f > 0.75 else 'crim2' if f > 0.1 else 'crim1' if f > -0.6 else 'crim0'
            if x in (x0 + 2, x1 - 2) or y in (y0 + 3, y0 + 4):
                k = 'gold1'
            if x in (x0, x1):
                k = 'crim0'
            L.p(x, y, k)
        L.p(x, bottom, 'crim0')
    for _ in range(7):
        hx, hy = rng.randint(x0 + 3, x1 - 3), rng.randint(y0 + 40, y0 + length - 6)
        L.clear(hx, hy); L.clear(hx + 1, hy)
    cx, cy = (x0 + x1) // 2, y0 + 30
    L.g.ellipse([cx - 9, cy - 9, cx + 9, cy + 9], outline=A['gold2'])
    if emblem == 'chain':
        for y in range(cy - 7, cy + 2, 3):
            L.p(cx, y, 'gold2'); L.p(cx, y + 1, 'gold1')
        L.r(cx - 1, cy + 1, cx + 1, cy + 7, 'gold2'); L.r(cx - 4, cy + 3, cx + 4, cy + 4, 'gold2')
    else:
        L.r(cx, cy - 7, cx, cy + 6, 'steel3'); L.r(cx - 4, cy - 3, cx + 4, cy - 3, 'gold2'); L.p(cx, cy - 8, 'gold2')
    return L


def foreground():
    F = Layer()
    F.g.polygon([(0, 304), (8, 298), (14, 303), (22, 292), (30, 300), (40, 296), (46, 360), (0, 360)], fill=A['fgk'])
    F.l([(0, 304), (8, 298), (14, 303), (22, 292), (30, 300), (40, 296)], 'fgr')
    for x, y in ((50, 352), (58, 356), (64, 350)):
        F.g.polygon([(x, y), (x + 5, y - 3), (x + 9, y + 1), (x + 9, 360), (x, 360)], fill=A['fgk'])
    F.g.polygon([(560, 360), (566, 318), (572, 318), (568, 360)], fill=A['fgk'])  # 부서진 긴 의자
    F.g.polygon([(560, 336), (640, 330), (640, 338), (560, 344)], fill=A['fgk'])
    F.g.polygon([(600, 300), (640, 294), (640, 330), (604, 334)], fill=A['fgk'])
    F.l([(560, 336), (640, 330)], 'fgr'); F.l([(600, 300), (640, 294)], 'fgr'); F.l([(566, 318), (572, 318)], 'fgr')
    for x, ln in ((34, 56), (616, 80)):
        for i, y in enumerate(range(0, ln, 3)):
            F.r(x - (1 if i % 2 else 0), y, x + (1 if i % 2 else 0), y + 2, 'fgk' if i % 2 else 'fgr')
        F.g.arc([x - 4, ln - 2, x + 4, ln + 6], 0, 200, fill=A['fgr'], width=2)
    return F


# =========================================================== 조명 → 양자화
BAYER = (np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16 - 0.47)
PAL = np.array([[int(h[i:i + 2], 16) for i in (1, 3, 5)] for h in PALETTE], np.float32)


def quantize(rgb, spread=22):
    hh, ww = rgb.shape[:2]
    noise = np.tile(BAYER, (hh // 4 + 1, ww // 4 + 1))[:hh, :ww, None] * spread
    flat = (rgb + noise).reshape(-1, 3)
    out = np.empty_like(flat)
    for i in range(0, len(flat), 40000):
        ch = flat[i:i + 40000]
        idx = ((ch[:, None, :] - PAL[None]) ** 2 * (0.3, 0.59, 0.11)).sum(-1).argmin(1)
        out[i:i + 40000] = PAL[idx]
    return out.reshape(hh, ww, 3)


def render(with_props):
    base = BG.im.copy()
    if with_props:
        for L in PROPS.values():
            base.alpha_composite(L.im)
    alb = np.asarray(base)[..., :3].astype(np.float32)
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    light = np.empty((H, W, 3), np.float32); light[:] = (0.27, 0.28, 0.40)
    for x, y, r, inten, col in (LIGHTS if with_props else LIGHTS[:1]) + SCONCE_LIGHTS:
        f = np.clip(1 - np.hypot(xx - x, (yy - y) * 1.15) / r, 0, 1) ** 1.6 * inten
        light += f[..., None] * np.array(col, np.float32)
    beam = Image.new('L', (W, H), 0)
    bg = ImageDraw.Draw(beam)
    bg.polygon([(284, 150), (362, 150), (318, FLOOR + 4), (196, FLOOR + 4)], fill=150)
    bg.polygon([(300, 160), (346, 160), (296, FLOOR + 2), (224, FLOOR + 2)], fill=110)
    bg.ellipse([186, FLOOR - 2, 330, FLOOR + 22], fill=255)
    beam = np.asarray(beam.filter(ImageFilter.GaussianBlur(5)), np.float32)[..., None] / 255
    light += beam * np.array(MOON) * 0.9
    out = alb * np.minimum(light, 1.7)
    out += beam * np.array([16, 24, 40], np.float32)  # 공기 중 달빛
    for (ex, ey, er, col) in ((252, 292, 14, (60, 8, 14)), (284, 296, 12, (50, 36, 6))):  # 색유리 빛 조각
        f = np.clip(1 - np.hypot(xx - ex, (yy - ey) * 2.2) / er, 0, 1)
        out += f[..., None] * np.array(col, np.float32)
    emi = np.asarray(EMI.im).astype(np.float32)
    a = emi[..., 3:4] / 255
    if not with_props:  # 소품의 불꽃 빼기: 창 발광만 남김
        a = a * WINDOW_MASK
    out = out * (1 - a) + emi[..., :3] * a
    haze = np.clip((yy - 230) / 52, 0, 1)[..., None] * np.array([6, 8, 16], np.float32)
    out += haze * (yy < FLOOR)[..., None]
    r = np.hypot((xx - 320) / 360, (yy - 190) / 230)
    out *= (1 - 0.55 * np.clip((r - 0.45) / 0.6, 0, 1))[..., None]
    for x, y in DUST:
        out[y, x] += 70
    q = quantize(np.clip(out, 0, 255))
    img = Image.fromarray(q.astype(np.uint8), 'RGB').convert('RGBA')
    img.alpha_composite(FG.im)
    return img


BG = draw_background()
WINDOW_MASK = (np.asarray(EMI.im)[..., 3:4] > 0).astype(np.float32)
PROPS = {
    'altar': prop_altar(),
    'wall_cross': prop_wall_cross(),
    'candelabra': prop_candelabra(),
    'map_table': prop_table(),
    'weapon_rack': prop_rack(),
    'door_training': prop_door(),
    'banner_chain': prop_banner(198, 226, 72, 96, 'chain'),
    'banner_sword': prop_banner(414, 442, 72, 92, 'sword'),
}
PROPS['sconce_left'] = prop_sconce(160)
PROPS['sconce_right'] = prop_sconce(480)
PROPS['sconce_door'] = prop_sconce(566)
SCONCE_LIGHTS = []  # 횃대 빛은 LIGHTS에 이미 들어감
FG = foreground()
DUST = []
for _ in range(60):
    y = rng.randint(150, FLOOR - 3)
    t = (y - 150) / (FLOOR - 150)
    DUST.append((rng.randint(int(284 + (196 - 284) * t), int(362 + (318 - 362) * t)), y))

full = render(True)
bg_only = render(False)
full.save(OUT / 'lobby_v2_native.png')
full.resize((W * 3, H * 3), Image.NEAREST).save(OUT / 'lobby_v2.png')
bg_only.save(OUT / 'lobby_v2_background_only_native.png')
FG.im.save(OUT / 'foreground_native.png')

# 소품 개별 PNG (조명이 구워진 원본 크기, 투명 배경)
sheet_items = []
for name, L in PROPS.items():
    mask = L.im.getchannel('A')
    box = mask.getbbox()
    piece = full.copy(); piece.putalpha(mask)
    piece = piece.crop(box)
    piece.save(OUT / 'props' / f'{name}.png')
    sheet_items.append((name, piece))

# 소품 시트 미리보기
S = 3
font = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 26)
cells = [(n_, p.resize((p.width * S, p.height * S), Image.NEAREST)) for n_, p in sheet_items]
cols, pad = 4, 30
cw = max(c.width for _, c in cells) + pad * 2
rh = max(c.height for _, c in cells) + 80
sheet = Image.new('RGBA', (cw * cols, rh * math.ceil(len(cells) / cols)), (16, 13, 22, 255))
sd = ImageDraw.Draw(sheet)
for i, (n_, c) in enumerate(cells):
    x, y = (i % cols) * cw, (i // cols) * rh
    sheet.alpha_composite(c, (x + (cw - c.width) // 2, y + rh - 60 - c.height))
    sd.text((x + cw // 2, y + rh - 30), n_, font=font, fill=(234, 223, 196), anchor='mm')
sheet.save(OUT / 'props_sheet.png')
print('ok', len(PROPS), 'props')
