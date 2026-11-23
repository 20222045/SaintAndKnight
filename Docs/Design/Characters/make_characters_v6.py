# 세실리아·아델라 전투 도트 v6 — 손으로 찍은 거친 도트(사용자 피드백 2026-11-10: 참고 게임처럼 큰 도트·단순한 덩어리).
# 자동 축소로는 30px대에서 얼굴·윤곽이 뭉개져, 한 칸씩 직접 배치. 게임에서는 도트 1칸 = 화면 2칸(24 PPU)으로 표시 예정.
# 현재 대기(idle) 1프레임씩만. 실행: python Docs/Design/Characters/make_characters_v6.py → v6/hand_*.png
from PIL import Image, ImageDraw
import sys
from pathlib import Path
S = str(Path(__file__).parent / 'v6')
Path(S).mkdir(exist_ok=True)
PAL_C = {
    'o': '#1c1622', 'h': '#8f8790', 'H': '#c9c3c6', 'i': '#efebe6',
    'w': '#8e8ba0', 'W': '#cfcedc', 'X': '#f6f6fa',
    'g': '#7a5a26', 'G': '#d2a84e', 's': '#c98a7c', 'S': '#f3cdb9', 'e': '#3d6fb0',
    'k': '#3a3540', 'm': '#8a8490', 'b': '#4a3b40',
}
CEC = [
    "........ooooo...........",
    ".......oXXXXXo..........",
    "......oXXGGGXXo.........",
    ".....oWXGGXGGXXo........",
    ".....oWXXGGGXiHHo.......",
    "....oWWXiHHHHHSSo.......",
    "....oWXHiHHhSSSSSo......",
    "....oWXHHHhSeSSeSo......",
    "...oWWXHHHhSSSSSso......",
    "...oWXHHHHhsSSsSo.......",
    "...oWXHHHHhosSSo........",
    "..oWXXHHHoGXXXGo........",
    "..oWXHHHoWXXXXXWo.......",
    "..oWXHHoWXXGXXWWSo......",
    "..owXHHoWXXGXXWWSo......",
    "..owWHhoWWXGXXWwSSo.....",
    "..owWHoWWGGGGGWwoSo.....",
    ".owwWhoWWXXGXXWWwoSSo...",
    ".owWWhoWWXXGXXXWwo.H....",
    ".owWhoWWWXXGXXXWWwo.m...",
    ".owWhoWWXXXGXXXWWwo.H...",
    ".owwoWWWXXXGXXXXWwo..m..",
    "..ooWWWXXXXGXXXXWWwookko",
    "...oWWWXXXXGXXXXXWwokmGk",
    "...oWWXXXXXGXXXXXWwokmmk",
    "..oWWWXXXXXGXXXXXWWwokk.",
    "..oWWXXXXXXGXXXXXXWwo...",
    ".oWWWXXXXXXGXXXXXXWwo...",
    ".oGGGGGGGGGGGGGGGGGGo...",
    ".oggggggggggggggggggo...",
    "...oobbo...oobbbo.......",
    "...obbbo...obbbbbo......",
]


def render(grid, pal, name, scale=12):
    w = max(len(r) for r in grid)
    im = Image.new('RGBA', (w, len(grid)), (0, 0, 0, 0))
    for y, row in enumerate(grid):
        for x, ch in enumerate(row):
            if ch != '.':
                im.putpixel((x, y), Image.new('RGB', (1, 1), pal[ch]).getpixel((0, 0)) + (255,))
    im.save(S + f'/hand_{name}.png')
    bg = Image.new('RGBA', (w * scale, len(grid) * scale), (34, 28, 38, 255))
    bg.alpha_composite(im.resize((w * scale, len(grid) * scale), Image.NEAREST))
    bg.save(S + f'/hand_{name}_x{scale}.png')
    return im




PAL_A = {
    'o': '#170f16', 'r': '#6a1424', 'R': '#a8243a', 'q': '#d24a55',
    's': '#c98a7c', 'S': '#f3cdb9', 'a': '#e0a030',
    'k': '#24232c', 'A': '#4c4c5c', 'L': '#9294a8',
    'G': '#d2a84e', 'g': '#8a6a2e', 'c': '#55101c', 'C': '#8e1c2c',
    'n': '#2c2230', 'B': '#e2e6ee', 'b': '#8c90a0', 'P': '#3e3450', 'p': '#2c2438',
}


def adela():
    W, H = 28, 35
    g = [['.'] * W for _ in range(H)]

    def put(x, y, c):
        if 0 <= x < W and 0 <= y < H: g[y][x] = c

    def row(y, x0, text):
        for i, c in enumerate(text):
            if c != ' ': put(x0 + i, y, c)

    # 망토(뒤, 왼쪽으로 흩날림)
    cape = {12: (7, 'oCo'), 13: (6, 'oCCo'), 14: (5, 'oCCCo'), 15: (5, 'oCcCCo'), 16: (4, 'oCCcCCo'),
            17: (4, 'oCcCCCo'), 18: (3, 'oCCcCCCo'), 19: (3, 'oCcCCcCo'), 20: (2, 'oCCcCCcCo'),
            21: (2, 'oCcCCcCCo'), 22: (1, 'oCCcCCcCCo'), 23: (1, 'oCcCCcCCCo'), 24: (0, 'oCCcCCCcCo'),
            25: (0, 'oCcCoCCcCo'), 26: (0, 'oCo.oCCcCo'), 27: (0, 'oo..oCcCo'), 28: (4, 'oCCo'), 29: (5, 'oo')}
    for y, (x, t) in cape.items(): row(y, x, t)
    # 포니테일(머리 뒤에서 왼쪽 아래로)
    pony = {2: (8, 'ooo'), 3: (6, 'ooRRqo'), 4: (5, 'oRRRqo'), 5: (4, 'oRRrRo'), 6: (3, 'oRRro'),
            7: (3, 'oRRo'), 8: (2, 'oRro'), 9: (2, 'oRo'), 10: (1, 'oRro'), 11: (1, 'oro'), 12: (1, 'oo')}
    for y, (x, t) in pony.items(): row(y, x, t)
    # 머리(오른쪽 바라봄): 진홍 머리 + 얼굴, 호박색 눈
    head = {1: (11, 'oooo'), 2: (10, 'oRqqRo'), 3: (10, 'oRqqRRo'), 4: (9, 'oRRqRRRRo'), 5: (9, 'oRRRRRRRRo'),
            6: (9, 'oRRrRRSSSo'), 7: (9, 'oRRrRSSSSo'), 8: (9, 'oRrRSoSSoo'), 9: (9, 'oRrRSaSSaS'),
            10: (9, 'oRrrSSSSSo'), 11: (10, 'orRSSsSo'), 12: (11, 'orosSo')}
    for y, (x, t) in head.items(): row(y, x, t)
    # 갑옷 몸통·팔
    body = {13: (8, 'okLLAAALLo'), 14: (8, 'oLLLAkALLLo'), 15: (8, 'oALLAkAGLAo'), 16: (8, 'okAALkAAALo'),
            17: (8, 'okAAAkAAAko'), 18: (8, 'oknAAkAAnko'), 19: (8, 'oknnAGAnnko'), 20: (8, 'oGGGGGGGGo'),
            21: (8, 'okAAkkAALo'), 22: (8, 'okALk.kALo'), 23: (9, 'okAk.kALo')}
    for y, (x, t) in body.items(): row(y, x, t)
    # 다리: 뒷다리(왼쪽) 어둡게, 앞다리 밝게, 무릎·철신
    legs = {24: (9, 'oknko oALo'), 25: (9, 'oknko oALo'), 26: (9, 'oAko  oLLo'), 27: (9, 'oLAo  oALo'),
            28: (9, 'okno  oknLo'), 29: (9, 'okno  oknAo'), 30: (9, 'okAo  oAkAo'), 31: (9, 'oAko  oALAo'),
            32: (8, 'okAAo  oAAALo'), 33: (8, 'okkkoo oLAAAAo'), 34: (8, 'ooooo  oooooo')}
    for y, (x, t) in legs.items(): row(y, x, t)
    # 장검: 뒷손(허리 왼쪽)에서 앞-아래로 비스듬히
    row(19, 6, 'oGo'); row(20, 5, 'oGGGo'); row(21, 6, 'oBo')
    for i, y in enumerate(range(22, 33)):
        x = 6 - (i + 1) // 2
        row(y, x, 'Bb' if i % 2 else 'bB')
    # 연 모양 방패(앞, 오른쪽): 금 테두리·금 십자·위 장식
    row(9, 22, 'G'); row(10, 21, 'oGo'); row(11, 19, 'ooGGGoo')
    for y in range(12, 27):
        if y == 15: row(y, 18, 'oGGGGGGGo')
        else: row(y, 18, 'oGpPPGPPGo'[:9] if y % 3 else 'oGPPPGPPGo'[:9])
    for y in range(12, 27): put(22, y, 'G')
    row(27, 19, 'oGPPGPGo'[:7]); row(28, 20, 'oGGGo'); row(29, 21, 'oGo'); row(30, 22, 'o')
    return [''.join(r) for r in g]


if __name__ == '__main__':
    render(CEC, PAL_C, 'cecilia')
    render(adela(), PAL_A, 'adela')
