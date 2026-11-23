# Day 15 — HP 회복, 무저항 HP 손실, 원인별 게임 오버

기준: 2026-11-12 (Asia/Seoul). 사용자 결정으로 기획이 바뀌었다.

## 바뀐 기획 (2026-11-12 사용자 결정)

- “정신 붕괴”는 별도 수치가 아니다. **HP가 깎이는 모습을 형상화한 이름**이다. 별도 정신 상태 시스템은 만들지 않는다.
- HP는 **일정 시간이 지나면 계속 회복**된다.
- 함정에 잡혀 **저항하지 않으면 피해가 계속 쌓이고**, HP가 0이 되면 완전히 삼켜져 **게임 오버**다.
- 게임 오버에는 **몬스터마다 다른 사망 컷씬**이 있다(그림은 사용자 제작 대기).
- HP의 구체적인 규칙은 Claude에게 맡겼다. 아래 수치는 모두 임시 프로토타입 값이다.

`MASTER_PLAN.md`의 “정신 붕괴는 체력과 별도 상태 시스템”과 이전 문서의 “별도 정신 상태 유지”는 이 결정으로 대체됐다.

## 구현한 규칙

### 1. HP 자동 회복 (`SKHealth`)

| 항목 | 값 |
| --- | --- |
| 회복 시작 | 마지막 피해·흡수로부터 3초 뒤 (`regenDelaySeconds`) |
| 회복 속도 | 초당 5 (`regenPerSecond`), 최대 HP까지 |
| 회복하지 않는 때 | HP 0, 잡혀 있는 동안, 새 피해를 받은 뒤 3초 |
| 적용 대상 | `Day09_Stage1`·`Day09_BossChapel`의 플레이어만. 적·보스·다른 씬은 0(회복 없음) |

코드 기본값은 0이라 기존 씬의 동작은 그대로다. 외형 손상 0~4단계는 같은 HP를 읽으므로 회복하면 단계도 되돌아간다.

### 2. 함정에 잡힌 동안의 HP 손실 (`SKEnvironmentalCapture`, `SKFleshWall`)

이전: 일정 간격의 작은 피해 + 무저항 4초가 되면 HP와 상관없이 즉시 흡수.
지금: **저항하지 않는 동안 HP가 계속 깎이고, HP 0이 흡수다.**

| 항목 | 값 |
| --- | --- |
| 무저항 손실 속도 | 최대 HP ÷ 4초 = 초당 25 (가득 찬 HP가 4초에 0) |
| 저항 | 연타 입력 한 번마다 0.35초 동안 손실이 멈춘다. 잃은 HP는 돌아오지 않는다 |
| 삼켜진 정도(그림 단계) | 잃은 HP 비율. HP가 낮은 채 잡히면 이미 깊이 삼켜진 그림에서 시작한다 |
| 탈출 | 기존 연타 탈출 그대로. 계속 연타하면 피해 없이 빠져나온다 |

점액 포획(연속 접지 1.25초), 촉수 포획(15 피해 직후), 살덩이 벽 예고, 탈출 후 무적·재포획 대기는 그대로다.
적 A(수도사) 잡기와 보스 촉수 잡기는 바꾸지 않았다(일정 간격 피해 후 내던지기).

### 3. 원인별 게임 오버 (`SKGameOverScreen`)

- `SKHealth.LastCause`가 마지막 피해·흡수를 준 대상을 기억한다.
- HP 0이 되면 **문구 없이 전투 화면만 살짝 어두워진다**(검정 50%, 0.5초에 걸쳐. 2026-11-12 사용자 결정). 어두운 막은 월드 카메라 앞의 검은 스프라이트 `GameOver_Dim`이라 측면 HUD는 어두워지지 않는다. 다시 시작하면 바로 밝아진다.
- 원인 이름은 화면에 나오지 않고 컷씬·HUD 그림을 고르는 데만 쓴다. 이름은 아래와 같다.

| 원인 | 이름(파일 이름) |
| --- | --- |
| 수도사(적 A, 소환 포함) | `MONK` |
| 처형인(적 B) | `EXECUTIONER` |
| 장미창의 성모 | `BOSS` |
| 점액 | `SLIME` |
| 바닥 촉수(회랑) | `TENTACLE` |
| 보스방 바닥 촉수 | `BOSS`(보스 소속) |
| 살덩이 벽 | `FLESHWALL` |

- 컷씬 그림을 `Assets/SaintKnightDay15/Resources/SaintKnightDay15/DeathCutscenes/<이름>.png`(스프라이트)로 넣으면 그 원인으로 죽었을 때 화면 전체에 표시되고, **R을 누를 때까지** 자동 재시도가 멈춘다.
- 그림이 없으면 어두워지기만 하고 기존 1.6초 자동 재시도가 그대로 돈다. 지금은 그림이 하나도 없다.
- `SKEnemyTrainingSession`이 시작할 때 이 화면을 스스로 붙이므로 씬은 수정하지 않았다.

### 4. 캐릭터별 HP 구슬 (`SKDay01HUD`, 2026-11-12 사용자 요청)

참고작처럼 HP 상태를 색으로 보여 주는 구슬을 측면 HUD 일러스트의 왼쪽 위에 둔다. 원작 그림은 쓰지 않고 코드로 그린 임시 도트다(구슬 24px + 주변 장식 80×32px, 화면에서는 2배). [미리보기](Design/HUD/hp_orb_placeholder_preview.png).

| HP | 색 | 외형 손상 단계 |
| --- | --- | --- |
| 75% 초과 | 파랑 | 0 |
| 25% 초과 ~ 75% | 노랑 | 1~2 |
| 0 초과 ~ 25% | 빨강 | 3 |
| 0 | 꺼진 회색 | 4 |

- 캐릭터마다 구슬과 주변 장식이 다르다. 세실리아는 십자 문양 구슬에 은빛 후광, 오른쪽으로 사슬과 작은 십자가. 아델라는 방패 문양 구슬이 징 박힌 금빛 칼자루 끝이 되고, 오른쪽으로 손잡이·가드·칼날이 이어지는 장검. Tab으로 전환하면 함께 바뀐다. 색이 바뀌는 것은 구슬뿐이고 장식은 그대로다.
- 표시 전용이다. HP는 지금 두 캐릭터가 공유하므로 색은 같고 그림만 다르다. 빨강에서 맞아도 남은 HP보다 피해가 작으면 죽지 않는다(“빨강에서 맞으면 즉사”는 2026-11-12 사용자 결정으로 넣지 않는다).
- 임시 그림이다. 구슬과 주변 디자인은 사용자가 정식으로 만들 예정이며, 그림이 오면 `SKDay01HUD.MakeOrb`·`MakeOrbFrame`을 그림 불러오기로 바꾼다.

## 바꾼 파일

- `Assets/SaintKnightDay01/Runtime/SKDay01HUD.cs`(구슬)

- `Assets/SaintKnightDay02/Runtime/SKHealth.cs`, `SKHitbox.cs`
- `Assets/SaintKnightDay10/Runtime/SKEnvironmentalCapture.cs`, `SKFleshWall.cs`
- `Assets/SaintKnightDay04/Runtime/SKEnemyTrainingSession.cs`, `SKEnemyAgent.cs`
- `Assets/SaintKnightDay09/Runtime/SKBossAgent.cs`
- `Assets/SaintKnightDay09/Scenes/Day09_Stage1.unity`, `Day09_BossChapel.unity`(플레이어 `SKHealth`에 회복 값 두 줄)
- 신규 `Assets/SaintKnightDay15/Runtime/SKGameOverScreen.cs`, `Assets/Tests/PlayMode/Day15HealthTests.cs`
- 기존 검사 `EnvironmentalCaptureTests.cs`, `FleshWallAbsorptionTests.cs`(바뀐 규칙에 맞춰 수정)

변경 전 사본: `Logs/Day15/Before/`.

## 검증

- 전체 Play Mode **146/146 통과**(전체 159, Explicit 13, 실패 0, 종료 코드 0) [결과 XML](../Logs/Day15/Full/playmode-results.xml) · [로그](../Logs/Day15/Full/playmode-tests.log). 기존 142 + 신규 4.
- 신규 `Day15HealthTests` 4개: 3초 뒤 회복·새 피해 시 대기 재시작·적은 회복 없음 / 저항 중에는 손실도 회복도 없고 포기하면 HP 0까지 깎여 삼켜짐·원인 `SLIME`·자동 재시도 / 원인별 이름과 컷씬 중 자동 재시도 정지·R 복구 / 캐릭터별 구슬의 파랑·노랑·빨강.
- 바뀐 규칙에 맞춰 고친 기존 검사 7개: `EnvironmentalCaptureTests` 3, `FleshWallAbsorptionTests` 1, `Day09BossTests` 1, `Day10TrapTests` 1, `Day13DamageStageTests` 1, `TrapDedicatedAnimationTests` 2(이전의 “4 피해 간격”과 “저항 시 진행도 0으로” 단정을 새 규칙으로 교체).
- Explicit 13개(화면 캡처·측정용)는 실행하지 않았다. `TrapConsumptionVisualTests`는 이전 규칙 기준일 수 있다.

## 필요한 그림 (2026-11-12 사용자 지시로 기록)

그리는 사람에게 넘기는 요청서는 [ART_REQUEST_DAY15.md](Design/ART_REQUEST_DAY15.md)다. 원인별 측면 HUD 이미지의 방향성이 들어 있고, **성적인 묘사의 내용과 수위는 사용자와 대화로 정하기로 해 비워 두었다.**

보스에게 죽었을 때와 각 패턴·함정에 맞았을 때는 **경우마다** 아래 세 가지가 필요하다. 지금은 셋 다 없거나 임시다.

1. **캐릭터 도트** — 그 공격에 맞거나 당하는 세실리아·아델라의 전투 화면 도트(캐릭터별로 따로).
2. **패턴이 플레이어에게 적용되는 도트** — 그 공격·함정이 캐릭터 몸에 닿아 감기거나 덮는 모습의 도트.
3. **그에 맞는 컷씬** — 그 원인으로 HP 0이 되었을 때 나오는 사망 컷씬(캐릭터별).

| 경우 | 1. 캐릭터 도트 | 2. 적용 도트 | 3. 컷씬 |
| --- | --- | --- | --- |
| 수도사(적 A) 공격·잡기 | 기존 피격·잡힘 도트 사용 | 잡힘 합성 그림(임시) | 없음 `MONK` |
| 처형인(적 B) 큰 공격 | 기존 피격 도트 사용 | 없음 | 없음 `EXECUTIONER` |
| 점액 | 전용 시트 있음 | 전용 시트 있음 | 없음 `SLIME` |
| 회랑 바닥 촉수 | 전용 시트 있음, 찔림은 임시 합성 | 전용 시트 있음 | 없음 `TENTACLE` |
| 살덩이 벽 | 전용 시트 있음 | 전용 시트 있음 | 없음 `FLESHWALL` |
| 보스 — 휩쓸기(`Sweep`) | 없음(기존 피격 도트) | 없음 | 없음 |
| 보스 — 바닥 촉수(`Spikes`) | 회랑 촉수 시트 공유 | 회랑 촉수 시트 공유 | 없음 |
| 보스 — 촉수 잡기(`Grab`) | 없음(잡힘 자세 위에 감는 촉수) | 없음 | 없음 |
| 보스 — 점액 뱉기(`Spit`) | 점액 시트 공유 | 점액 시트 공유 | 없음 |
| 보스에게 사망(패턴 공통) | — | — | 없음 `BOSS` |

- 컷씬은 캐릭터 2명 × 원인별로 필요하다. 지금 틀은 **원인당 그림 한 장**이고 캐릭터와 보스 패턴을 구분하지 않는다(`BOSS` 하나). 그림이 정해지면 파일 이름을 `<원인>_<패턴>_<캐릭터>` 식으로 나누도록 `SKGameOverScreen.KeyFor`를 넓힌다.
- 캐릭터 도트와 적용 도트는 기존 아틀라스 칸·전용 시트 규격([TRAP_REACTION_GUIDE.md](Design/Characters/TRAP_REACTION_GUIDE.md), [TRAP_DEDICATED_MOTION.md](TRAP_DEDICATED_MOTION.md))을 따른다. 보스 패턴용 칸은 아직 정하지 않았다.
- HP 구슬과 주변 장식도 임시 그림이다(위 4번). 정식 디자인은 사용자 제작 예정.

## 하지 않은 것

- 사망 컷씬 그림(사용자 제작 대기). 컷씬은 정지 그림 한 장을 띄우는 틀만 있다.
- 적 A 잡기·보스 촉수 잡기를 함정과 같은 “무저항 손실” 규칙으로 바꾸는 것.
- 로비·Day 5·Day 8 씬의 HP 회복.
- 화면 캡처, Windows 빌드, 수동 조작감 검수.
