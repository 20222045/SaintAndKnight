# Day 13 — 외형 손상 0~4단계 판정

작업일: 2026-11-11~12 (Asia/Seoul). 기존 HP 기반 외형 손상 규칙을 체력 코드의 공용 판정으로 정리했다.
HUD와 전투 도트가 같은 현재 HP와 최대 HP를 읽으며, 캐릭터 디자인·씬·전투 수치는 유지한다.

## 판정 규칙

| 단계 | 현재 HP / 최대 HP | 최대 HP 100일 때 |
| --- | --- | --- |
| 0 | 75% 초과 | 75 초과 |
| 1 | 50% 초과, 75% 이하 | 50 초과~75 이하 |
| 2 | 25% 초과, 50% 이하 | 25 초과~50 이하 |
| 3 | 0 초과, 25% 이하 | 0 초과~25 이하 |
| 4 | 0 이하 | 0 (`INCAPACITATED`) |

판정은 반올림하지 않은 실제 HP를 사용한다. 최대 HP는 기존 체력 컴포넌트의 양수 설정값을 사용하며,
100이 아닌 경우에도 각 최대 HP의 25%·50%·75%와 비교한다. HUD 숫자의 반올림은 판정에 영향을 주지 않는다.
손상 단계는 현재 HP에서 계산하므로 회복하면 역으로 낮아지고, 체력 초기화 후에는 0단계로 돌아간다.
영구 손상이나 정신 붕괴를 뜻하지 않으며, 별도 정신 상태 시스템은 Day 15 범위다.

## 변경한 코드

| 파일 | 변경 내용 |
| --- | --- |
| [SKHealth.cs](../Assets/SaintKnightDay02/Runtime/SKHealth.cs) | 읽기 속성 `DamageStage`와 공용 함수 `DamageStageFor(currentHp, maximumHp)` 추가. HP·피해 승인·회복·드레인의 기존 처리는 유지 |
| [SKDay01HUD.cs](../Assets/SaintKnightDay01/Runtime/SKDay01HUD.cs) | 연결된 체력의 `DamageStage`를 읽도록 변경. 기존 `StageFor(percent)`는 최대 HP 100 기준 공용 함수를 호출하는 호환 메서드로 유지 |
| [SKCharacterSpriteView.cs](../Assets/SaintKnightDay06/Runtime/SKCharacterSpriteView.cs) | HUD의 정적 함수 대신 체력의 `DamageStage`를 읽어 기존 손상 대기 그림 선택 |
| [Day09BossTests.cs](../Assets/Tests/PlayMode/Day09BossTests.cs) | 전체 회귀에서 드러난 기존 점프 회피 검사의 프레임 의존성 보완. 테스트에서만 일정한 시간 간격을 사용하고 실제 상승·타격 순간 발높이·공격 종료·착지를 관측. 시간 간격은 `finally`에서 복구하며 대기 제한은 게임 시간과 실제 시간 상한을 함께 사용 |

단계는 별도 저장·누적하지 않는다. 일반 피격, 잡힘 중 `Drain`, 회복, R 초기화로 HP가 바뀌면 같은 현재 값을 읽는다.
기존 `Changed` 통지는 HUD 표시 갱신에 계속 사용한다. 전환 후 공유 HP를 유지하는 현재 정책도 같다.

사망·잡힘·피격·공격과 이동 모션의 표시 우선순위, 공격 판정·쿨타임·히트스톱은 바꾸지 않았다.
세실리아 HUD 0~4단계 일러스트와 두 캐릭터의 기존 손상 대기·쓰러짐 그림을 그대로 사용한다.
아델라는 단계별 HUD 일러스트가 없어 기존 초상을 유지한다. 손상 단계 판정은 초상 자료 유무와 무관하다.
변경 전 사본은 `Logs/Day13/Before/`에 보관했다. 작업 중 추가된 Day 11·12 확정 기록을 포함한 문서는 `Logs/Day13/BeforeLatest/`에도 보존했다.

## 검증 기록

- 신규 [Day13DamageStageTests.cs](../Assets/Tests/PlayMode/Day13DamageStageTests.cs): 8개 모두 통과, 실패·건너뜀 0, 종료 코드 0. [전용 XML](../Logs/Day13/TargetedHost/playmode-results.xml) · [로그](../Logs/Day13/TargetedHost/playmode-tests.log) · [종료 코드](../Logs/Day13/TargetedHost/exit-code.txt).
- 최대 HP 100·200·360의 각 경계 직전/정확값/직후, 극소 양수 HP, 피해 승인/무적 중 거부/무효 피해, 무적 중에도 진행되는 `Drain`, 회복 역전이·초기화를 검사했다.
- 최신 두 씬에서 양 캐릭터의 단계 0~4, 세실리아 단계별 초상·아델라 기존 초상, 손상 대기·쓰러짐, 최대 HP 200에서의 회복 역전이, 실제 살덩이 벽 드레인으로 경계 통과와 R 복구를 검사했다. 손상 단계와 잡힘·행동 가능 상태의 독립성도 확인했다.
- 최초 배치 실행은 기존 Day 12 검사 프로세스가 프로젝트를 점유해 검사 시작 전에 종료했다(XML 없음, `Logs/Day13/Targeted/`). 해당 프로세스들의 정상 종료를 기다린 뒤 `TargetedHost`에서 성공했다. 편집기를 강제 종료하거나 잠금 파일을 삭제하지 않았다.
- 첫 전체 Play Mode 회귀: 120개 통과, 1개 실패, Explicit 12 건너뜀(전체 133, 종료 코드 2). 새 Day 13 검사 8개는 모두 통과했고, 기존 `Day09BossTests.TentacleGrabHoldsUntilMashedOutAndAJumpAvoidsIt`의 점프 회피 단정이 실패했다. [첫 전체 XML](../Logs/Day13/Full/playmode-results.xml).
- 해당 검사는 수정 전 단독 재실행에서 1/1 통과했다. [재현 XML](../Logs/Day13/BossRepro/playmode-results.xml). 기존 고정 대기 뒤 Space를 다음 입력 프레임에 전달하는 순서가 긴 프레임의 누적 물리 처리보다 늦어질 수 있었다. 보스 런타임·점프·잡기 수치를 바꾸지 않고 검사에서 실제 점프와 공격 종료를 확인하도록 보완했다.
- 보스 클래스 필터 `Day09BossTests`로 실행한 `Logs/Day13/BossStabilized/`는 Explicit 화면 캡처까지 선택해, `-nographics`의 `Camera.Render`에서 Unity가 중단됐다(XML 없음, 종료 코드 -1073741819). 기능 검사 실패나 수동 검수로 계산하지 않는다. 기존 보스 캡처 PNG의 수정 시각은 바뀌지 않았다.
- 최종 통합 전체 Play Mode: **123/123 통과**(전체 135, Explicit 12 건너뜀), 실패 0, 종료 코드 0. 기존 113개 + Day 13 신규 8개 + 별도 Day 14 신규 2개가 모두 통과했으며, 보완한 `Day09BossTests.TentacleGrabHoldsUntilMashedOutAndAJumpAvoidsIt`도 통과했다. [통합 전체 XML](../Logs/Day14/Full/playmode-results.xml) · [로그](../Logs/Day14/Full/playmode-tests.log) · [종료 코드](../Logs/Day14/Full/exit-code.txt).
- 이 전체 실행은 별도 Day 14 작업에서 시작한 것으로, Day 13 공용 판정·검사·보스 검사 보완과 동시 Day 14 변경을 포함한 현재 어셈블리를 검증했다. Day 13만의 별도 최종 전체 실행으로 기록하지 않는다. 실행 시각은 **2026-11-12 00:18:30~00:24:09 KST**(약 339초)이며, 최종 로그에 C# 컴파일 오류·경고는 없었다.
- 수정 소스·어셈블리의 시각을 확인했고 검사 대상 소스 해시는 `Logs/Day13/IntegratedValidationInputs.json`에 기록했다. 실행 종료 후 소스 5개(`SKHealth`, `SKDay01HUD`, `SKCharacterSpriteView`, Day 13 검사, 보스 검사)의 SHA256가 기록과 모두 일치함을 확인했다. 기존 Day 11·12 확정 기록은 유지했으며 최종 기록 갱신 직전 이 문서 사본은 `Logs/Day13/BeforeLatest2/Docs/DAY13.md`에 추가 보존했다.

Unity 6000.0.81f1, `-batchmode -nographics --burst-disable-compilation -runTests -testPlatform PlayMode`를 사용했다.
[실행 스크립트](../Logs/Day13/Run-PlayMode.ps1)에 `-RunName`과 선택적 `-TestFilter Day13`을 전달해 재실행할 수 있다(기존 결과 폴더는 덮어쓰지 않음).
자동 검사는 수동 조작감·렌더링 검수를 대신하지 않는다. 새 화면 캡처와 Windows 빌드는 수행하지 않았다.

## 다음 작업

별도 Day 14 작업(2D 캐릭터 손상 표시와 함정 반응 도트 연결)은 임시 합성 그림 연결을 완료했다. 후속 구현·정식 아트 상태는 [Day 14 기록](DAY14.md)과 [최신 구현 상태](IMPLEMENTATION_STATUS.md)를 따른다. 위 통합 검증에는 해당 Day 14 신규 검사 2개도 포함됐다. 다음 로드맵은 Day 15 정신 상태다.
사용자가 추가로 요청한 전신 포박 비주얼은 Day 13 판정 작업 밖의 후속 요청이다. 이 Day 13 작업에서는 새 그림이나 모델을 제작하지 않았다.
Day 17 HUD 상태 변형과 아델라 단계별 HUD 일러스트는 별도 자료가 필요하다. 수동 조작감·렌더링 검수는 남아 있다.
