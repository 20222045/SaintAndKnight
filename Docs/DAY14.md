# Day 14 — 함정 반응 도트 연결 (임시 합성 그림)

**최신 전용 모션 후속(2026-11-12):** 기존 인물 도트 기반 6시트·138프레임을 실제 게임에 연결했다. 사진1 촉수 나선 감김·사진2 벽 쪽 수평 끌림·승인된 점액을 반영했다. [142/142 전체 검증과 실제138장 기록](TRAP_DEDICATED_MOTION.md)을 따른다. 아래 초기 그림·절차적 레이어·이전 검사 증빙은 당시 기록이다.

**후속 사용자 요청:** 기존 도트 위에 전신 감김·흡수 표현과 실제 포획/저항/완전 삼켜짐을 추가하는 작업은
[TRAP_CONSUMPTION.md](TRAP_CONSUMPTION.md)에 기록한다. 아래 본문과 123/123 증빙은 그 전의 Day 14 초기 연결 결과다.

작업일: 2026-11-12 (Asia/Seoul). 사용자 요청 “14일차 해줘” → 반응 도트가 아직 없어 문의 →
“임시로 해주고 어떤식으로 도트 찍어야 하는지 남겨놔”.

## 결과

- 바닥 촉수에 찔리면 일반 피격 대신 **찔림 그림**(3장, 0.16초), 점액 웅덩이 위에서는 달리기·대기 대신
  **점액 위 걷기**(4장)·**점액 위 대기**(2장)가 나온다. 세실리아·아델라 모두, 스테이지 회랑과 보스 예배당 공통.
- 그림은 **임시 합성**이다. 기존 피격·달리기·대기 칸에 점액색과 찌름 자국만 덧그렸고 캐릭터 외형은 그대로다.
  [제작 스크립트](Design/Characters/make_trap_reactions.py) · [미리보기](Design/Characters/trap_reactions_preview.png).
- 정식 도트 제작 방법은 [TRAP_REACTION_GUIDE.md](Design/Characters/TRAP_REACTION_GUIDE.md)에 남겼다.
  같은 칸에 덮어 그리면 코드 수정 없이 바뀐다.
- 판정·피해·경직·감속·시간은 바꾸지 않았다. 그림 선택만 추가했다.
- “2D 캐릭터 손상 표시 연결”은 Day 13에서 `SKHealth.DamageStage`로 이미 연결되어 있어 추가 변경이 없다.
  점액 위 대기는 손상 대기 그림보다 먼저 나온다.

## 변경한 파일

| 파일 | 내용 |
| --- | --- |
| `Assets/SaintKnightDay06/Resources/SaintKnightDay06/cecilia_motion.png`, `adela_motion.png` | 맨 아래에 한 줄(9칸) 추가. 세실리아 72~80, 아델라 84~92. 기존 칸은 그대로 |
| 같은 폴더 `*_motion_frames.json` | `SpikeHurt`·`SlimeWalk`·`SlimeIdle` 세 동작 추가 |
| [SKCharacterMotionLibrary.cs](../Assets/SaintKnightDay06/Runtime/SKCharacterMotionLibrary.cs) | `SKSpriteMotion`에 세 값 추가 |
| [SKCharacterSpriteView.cs](../Assets/SaintKnightDay06/Runtime/SKCharacterSpriteView.cs) | 피격 시작 때 촉수 여부를 한 번 판단, 땅 위 점액이면 걷기·대기 교체 |
| [SKFloorSpike.cs](../Assets/SaintKnightDay10/Runtime/SKFloorSpike.cs) | 표시용 조회 `AnyStrikingAt(point)` |
| [SKSlimePool.cs](../Assets/SaintKnightDay10/Runtime/SKSlimePool.cs) | 표시용 조회 `AnyCovers(player)` |
| [Day14TrapReactionTests.cs](../Assets/Tests/PlayMode/Day14TrapReactionTests.cs) | 신규 검사 2개 |

변경 전 사본은 `Logs/Day14/Before/`.

## 알려 둘 점

- 촉수 찔림은 “피격이 시작된 순간 솟아 있는 촉수 위에 있었는가”로 판단한다(피해 출처를 따로 기록하지 않음).
  솟은 촉수 위에서 다른 공격을 동시에 맞으면 찔림 그림이 나올 수 있다. 판정에는 영향이 없다.
- “점액 묻음”(웅덩이를 벗어난 뒤 남는 점액, 요청서에서 선택 항목)은 넣지 않았다.
- 점액 위에서 공격·점프·웅크리기·잡힘은 기존 그림 그대로다.

## 검증 기록

- Day 14 검사 **2/2 통과**, 종료 코드 0: [XML](../Logs/Day14/Targeted/playmode-results.xml) · [로그](../Logs/Day14/Targeted/playmode-tests.log).
  두 캐릭터의 점액 대기·걷기, 점프 중·웅덩이 밖에서는 기존 그림, 감속 ×0.5 유지,
  촉수 피격의 찔림 그림과 피해 15 유지, 다른 피격은 일반 피격 그림을 확인했다.
- 전체 Play Mode **123/123 통과**(전체 135, Explicit 12, 실패 0, 종료 코드 0):
  [XML](../Logs/Day14/Full/playmode-results.xml) · [로그](../Logs/Day14/Full/playmode-tests.log).
- 재실행: `Logs/Day14/Run-PlayMode.ps1 -RunName <새 이름> [-TestFilter Day14TrapReactionTests]`.
- 작업 중 다른 도구(Day 13)가 같은 프로젝트에서 Unity 검사를 돌리고 있었다. 그 실행
  `Logs/Day13/BossStabilized`(00:11:58 시작, 보스 검사만)가 도는 동안 이 작업의 코드·JSON·아틀라스가
  약 30초에 걸쳐 저장되었으므로, 그 실행은 Day 14 변경이 일부 섞인 상태였을 수 있다.
  위 Day 14 결과 두 건은 Unity가 비어 있을 때 단독으로 실행했다.
- 화면 캡처·Windows 빌드·수동 조작감 및 렌더링 검수는 하지 않았다. 임시 그림이 실제 화면에서 어떻게 보이는지는 미확인이다.

## 다음 작업

Day 15 정신 상태. 정식 반응 도트(사용자 제작)가 오면 아틀라스 칸만 교체한다. Day 17 HUD 상태 변형은 그림 대기.

## 14일차 종료 시점 정리 (2026-11-12 오후, 사용자 요청으로 기록)

14일차까지 구현과 자동 검사는 끝났다. 다음은 Day 15 정신 상태이며, 기능상 막히는 것은 없다.
사람이 직접 플레이해서 확인하는 검수는 Day 1부터 지금까지 한 번도 하지 않았다.

### 14일차에서 된 것

- 함정 반응: 촉수 찔림 `SpikeHurt`, 점액 위 걷기 `SlimeWalk`·대기 `SlimeIdle` 연결(임시 합성 그림, 이 문서 위쪽).
- 포획·흡수: 점액·촉수·살덩이 벽에 잡혀 감기고 4초간 저항하지 않으면 삼켜진다. 전용 시트 6장·138프레임
  ([TRAP_CONSUMPTION.md](TRAP_CONSUMPTION.md), [TRAP_DEDICATED_MOTION.md](TRAP_DEDICATED_MOTION.md)).
- 후반 단계 가림 정리: 점액·촉수 시트 4장의 칸 4·5·14~17 교체(TRAP_DEDICATED_MOTION.md 끝 절).
- 최신 전체 Play Mode **142/142 통과**(전체 155, Explicit 13, 종료 코드 0) [XML](../Logs/TrapClip/Full/playmode-results.xml).

### 남아 있는 것

| 항목 | 상태 |
| --- | --- |
| 수동 조작감·화면 검수 | 미실시. 자동 검사와 캡처 이미지 확인만 했다 |
| 정식 반응 도트(찔림·점액 걷기/대기) | 임시 합성 그림 사용 중. 사용자 그림이 오면 [제작 안내](Design/Characters/TRAP_REACTION_GUIDE.md)의 같은 칸에 덮어쓴다 |
| 최신 화면 캡처 | 기존 138장은 후반 단계 시트 교체 전 그림. 다시 찍지 않았다 |
| Windows 빌드 | Day 11(`Builds/Win64_Day11/`) 이후 새로 만들지 않았다 |
| 아델라 손상 단계별 HUD 일러스트 | 없음. 기존 초상 사용 |
| 점액 묻음(웅덩이를 벗어난 뒤 남는 점액) | 미구현(요청서의 선택 항목) |
| 전용 시트 제작 스크립트 | 후반 단계 가림 조건은 사본 `Design/Characters/trap-downscale-claude/builder-clip/`에만 있다. 원본 빌더로 다시 만들면 이전 상태로 돌아간다 |

### 시도했지만 적용하지 않은 것

- 코드로 직접 그린 촉수·점액 감김: 품질 미달(`Logs/Day14/wrap_sample_claude.png`).
- 촉수 소재 축소 방식 교체: 게임 시트와 차이가 작아 보류(`Design/Characters/trap-downscale-claude/staging-tentacle-vote/`).
- 생성 AI 인물 시안(`Design/Characters/trap-wrap-v2/`): Assets에 넣지 않았다.

15일차 전에 사용자가 한 번 직접 플레이해 함정 그림과 조작감을 확인하면, 그 결과에 맞춰 고친다.