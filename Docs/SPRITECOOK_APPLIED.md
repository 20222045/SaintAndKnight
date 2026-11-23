# 요청 도트 7장 + 보스 점액 시트 게임 화면 연결

작업일: 2026-11-13 (Asia/Seoul), Claude. 사용자 지시 “이거 다 디자인 했거든 적용해줘”. 앞 단계는 [제작·규격 기록](SPRITECOOK_DRAFTS_COMPLETED.md).

> 2026-11-14 변경: 사용자 검수(“캐릭터 도트가 제일 어색하다”)에 따라 **함정 반응(찔림·점액 걷기/대기)은 새 시트 연결을 빼고 Day 6 아틀라스의 기존 칸으로 되돌렸다.** 적용본 `*_trap_reaction.png` 2장도 Resources에서 뺐다(Drafts는 유지). 아래 표의 첫 줄은 현재 적용되지 않는다. 되돌린 뒤 전체 Play Mode 151/151 통과(전체164, Explicit13) `Logs/SpriteCookApply/Full2/`. 사망·들림·쓰러짐 도트는 연결된 채이며 PixelLab으로 다시 만들 예정이다.

## 무엇이 화면에 나오나

| 상황 | 그림 | 칸 |
| --- | --- | --- |
| 바닥 촉수에 찔림 / 점액 위 걷기 / 점액 위 대기 | `<캐릭터>_trap_reaction` | 0~2 / 3~6 / 7~8 |
| 수도사에게 사망 / 처형인에게 사망 | `<캐릭터>_enemy_death` | 0~3 / 4~7 (초당 6칸, 마지막 칸 유지) |
| 보스 공격에 맞음(휩쓸기·내던지기) | `<캐릭터>_boss_pattern` | 0~3 (0.08초씩) |
| 보스 잡기: 들림 / 연타 저항 / 탈출 | `<캐릭터>_boss_pattern` | 4 / 5~7 / 8 |
| 보스에게 사망 | `<캐릭터>_boss_pattern` | 9~10 |
| 보스가 뱉은(시간제) 점액에 잡힘 | `TrapMotion/<캐릭터>_bossslime` | 23칸, 기존 함정 시트와 같은 재생 |
| HUD HP 구슬 | `hp_orb_ornaments` | 0 유리, 1 세실리아 장식, 2 아델라 장식 |

HP 구슬은 장식(색 없음) → 색이 바뀌는 채움(기존 코드 그림, 지름 38px) → 유리 순서로 겹친다. 파랑·노랑·빨강 규칙은 그대로다.

## 파일

- 적용본(복사, 새 GUID): `Assets/SaintKnightSpriteCook/Resources/SaintKnightSpriteCook/` 7장, `Assets/SaintKnightDay10/Resources/SaintKnightDay10/TrapMotion/<캐릭터>_bossslime.png/.json`. `Drafts`의 검수본과 분할 Sprite 59개는 그대로 둔다.
- 새 코드: `Assets/SaintKnightSpriteCook/Runtime/SKAppliedSheets.cs` (시트 → 128×96 칸 Sprite, 없으면 null).
- 고친 코드: `SKCharacterSpriteView`(위 표의 선택), `SKTrapAnimationLibrary`(`BossSlime` 종류, `For(캐릭터, capture)`), `SKTrapEnvelopeView`(같은 선택 사용), `SKSlimePool`(`Permanent` 읽기), `SKDay01HUD`(구슬 3겹).
- 표시만 바꿨다. 피해·판정·잡힘·시간·씬·기존 그림·기존 GUID는 그대로이고, 시트가 없으면 이전 그림으로 돌아간다. Day 6 아틀라스의 임시 함정 반응 칸은 대체 그림으로 남아 있다.
- 변경 전 코드 5개: `Logs/SpriteCookApply/Before/`.

## 검사

- 새 검사 `SpriteCookAppliedTests` 2개: 대성당·보스방에서 두 캐릭터로 위 상황을 실제로 일으켜 그림 이름을 확인한다(점액은 실제 1.25초 체류로 묶임). 그래픽 장치가 있으면 HUD 포함 화면을 `Logs/SpriteCookApply/Screens/`에 쓴다.
- 고친 검사: `Day14TrapReactionTests`(새 반응 칸), `Day15HealthTests`(새 장식·유리), `TrapDedicatedAnimationTests`(`BossSlime`은 시간제 웅덩이로 진행).
- 전체 Play Mode **151/151 통과**(전체 164, Explicit 13, 실패 0, 종료 0): [XML](../Logs/SpriteCookApply/Full/playmode-results.xml). 이 실행 뒤 `SpriteCookAppliedTests`에 점액 묶임 장면과 사망 위치만 추가했고, 그 두 검사는 그래픽 배치로 2/2 통과: [XML](../Logs/SpriteCookApply/Applied/results.xml).
- 게임 화면 28장: `Logs/SpriteCookApply/Screens/`, 모음 `Logs/SpriteCookApply/applied_game_screens.png`.

## 남은 것 / 알려진 점

- 수동 플레이·조작감 검수, 새 Windows 빌드는 하지 않았다. 재생 속도(사망 초당 6칸, 쓰러짐 0.08초)는 임시값이다.
- 보스 공격에 맞아 쓰러진 뒤 피격 시간이 끝나면 누운 자세에서 바로 서 있는 자세로 바뀐다(일어나는 칸 없음).
- 피격 순간의 빨간 색 입힘이 쓰러짐·점액 시트 위에도 그대로 덮인다(기존 동작).
- 보스방 점액은 올리브·장미빛, 대성당 점액은 기존 녹색 시트다.
- 1번 점액 합성 시트의 다듬기(줄기·아랫단 등)는 Codex v2 기준이며 추가 손질은 하지 않았다.
