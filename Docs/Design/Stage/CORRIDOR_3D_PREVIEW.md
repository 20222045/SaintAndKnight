# 대성당 회랑 3D 배경 실험

2026-11-19. 1스테이지 회랑의 석조 아치 한 칸과 양쪽 기둥을 실시간 3D 메시로 구성했다. 원래 벽과 안뜰 그림 위에 밝기가 다른 돌 블록, 줄눈, 모서리, 표면의 작은 흠집을 합성한다.

## 플레이

- Unity에서 **Tools → SaintKnight → Preview → Open Corridor 3D Preview**로 [실험 씬](../../../Assets/SaintKnightDay09/Scenes/Corridor3DPreview.unity)을 열고 Play를 누른다. 세실리아가 회랑에 서 있다. D 또는 오른쪽 방향키로 아치를 지나간다.
- 같은 메뉴의 **Toggle 2D / 3D Corridor**를 누르면 플레이 중 같은 자리에서 기존 도트와 3D 아치를 바꿔 볼 수 있다. Inspector의 `Corridor 3D Preview` 오브젝트에서도 **Show Geometry**로 전환한다.
- 원본 스테이지, 배경 PNG, 원본 씬 GUID는 해시가 같다. 실험 씬은 Unity Editor에서 연다.

## 구현과 결과

- [아치와 기둥 메시](../../../Assets/SaintKnightDay09/Runtime/SKCorridorStoneMesh.cs)는 닫힌 돌 블록과 모서리를 생성한다. [미리보기 렌더러](../../../Assets/SaintKnightDay09/Runtime/SKCorridorPreview.cs)는 48 PPU, 640×360, Point 샘플링, MSAA 1로 렌더링한다. 실험 씬의 횡스크롤 카메라와 아치 카메라를 같은 픽셀 격자에 맞춘다.
- [Unity 그래픽 검사](../../../Logs/Corridor3D/graphics-final.xml) 1/1. 촬영에서 적과 바닥 촉수를 끄고 세실리아를 오른쪽으로 움직여 32개 위치에서 같은 프레임의 기존 화면과 3D 화면을 각각 촬영했다. 연속 31구간에서 아치 마스크를 카메라 이동만큼 옮겼을 때 달라진 외곽선이 각 구간 3픽셀 이내였다. 변경된 픽셀은 아치와 기둥 범위에 들어왔다.
- [걷기 비교 GIF](../../../Logs/Corridor3D/corridor-walk-comparison.gif) · [정지 비교 PNG](../../../Logs/Corridor3D/corridor-comparison.png). 캡처는 월드 카메라 기준이다. 석재 표면은 기존 손도트보다 균일해 보인다.
- 재촬영은 그래픽 배치에서 `-testFilter CorridorPreviewTests.WalkingCeciliaKeepsTheArchOnThePixelGrid`를 쓴다.
- 씬을 다시 생성하려면 Unity 배치에서 `-executeMethod SaintKnight.Day09.SKCorridorPreviewBuilder.Build`를 실행한다.
- [전체 회귀 결과](../../../Logs/Corridor3D/full-regression.xml). Play Mode 191 통과, 실패 0, 전체 213, Explicit 22.
