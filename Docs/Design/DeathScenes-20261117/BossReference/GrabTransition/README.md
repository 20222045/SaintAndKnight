# 보스 촉수 잡기 · 첫 1초의 2D 동작

세실리아와 아델라의 4초 사망 연출은 초당 8칸이다. 0번은 기존 피격 도트다. 1~2번은 다리가 풀리는 모습, 3~4번은 내려앉으며 팔을 드는 모습, 5~6번은 뒷무릎이 닿고 손은 아직 얼굴 앞에 있는 모습이다. 7번에서 손이 이마에 닿고 8번부터 기존 후반 연기로 이어진다. [예배당에서 촬영한 이전·이번 비교](grab-comparison.gif)

| 제작 단계 | 이번 작업에서 실제 사용한 도구 |
| --- | --- |
| 원화 | 2dimg2motion으로 기존 무릎 포즈를 표준화하고 기준 그림과 손 방향을 고정했다. 내장 ImageGen으로 캐릭터마다 중간 자세 네 장을 그렸다. [세실리아](cecilia.png) · [아델라](adela.png) |
| 게임 도트 | PixelOE의 2배 축소와 윤곽 처리 후, Codixel로 각 128×96칸의 투명 시트를 묶었다. 발을 바닥 높이에 맞추고 장화, 양팔, 얼굴 위치를 칸마다 표시했다. |
| 그림 검사 | Pixelbolt로 전후 자세를 겹쳐 봤다. Agent Sprite Forge 검사에서 칸을 넘는 조각·가장자리 잘림은 0개였다. 아래 여백은 6px로 검사 기본값인 15%보다 좁다. |
| 동작 검사 | sprite-gen에서 양쪽 모두 중복 자세 0개, 잘린 프레임 0개, 네 포즈의 하단 좌표 90px로 확인했다. 보행 속도 추정은 이 제자리 무릎 동작에서 성립하지 않았다. |

프로젝트의 기존 도구 시험과 같은 **Agent Sprite Forge**를 agentsprite로 해석했다. 이번 작업에는 SSD 모델을 사용하지 않았다.

[Claude 인계서](../CLAUDE_2D_HANDOFF.md) · [사용한 프롬프트와 기준 PNG](../PROMPTS_2D.md) · [그림이 들어 있는 PDF](../SaintAndKnight-2D-Claude.pdf)

`pack.py <Codixel의 core/pixel.js 경로>`를 PixelOE가 설치된 Python에서 실행하면 에디터용 두 시트와 접점 좌표를 재생성한다. 이후 Unity 메뉴 `Tools / SaintKnight / Art / Bake Native Defeat Motions`를 실행하면 게임용 두 사망 시트에 반영된다. `review.py`는 이전과 이번의 실제 Unity 촬영 폴더에서 동일한 32프레임을 읽어 비교 GIF를 만든다.

## 최종 확인

Play Mode 198개 통과·26개 제외·실패 0개, 보스방 그래픽 17/17. Windows 빌드·15초 기동 확인. 검사 XML과 빌드 로그는 `Logs/Grab2D20261119/`에 있다.
