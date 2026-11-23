# Day 12 — 캐릭터 능력 및 전환 검증

작업일: 2026-11-11 (Asia/Seoul). 범위는 기존 캐릭터 능력과 전환 규칙의 통합 검증이다.
실제 플레이 씬 `Day09_Stage1`과 `Day09_BossChapel`을 사용하며 기존 전투 수치·아트·씬은 유지한다.

## 추가한 검사

| 검사 파일 | 개수 | 검증 내용 |
| --- | --- | --- |
| [Day12AbilityTests.cs](../Assets/Tests/PlayMode/Day12AbilityTests.cs) | 3 | 실제 Tab/J 입력으로 세실리아→아델라→세실리아 전환 후 B 고유 아머 절단 여부, A 일반 예고 취소 후 회복 0.2/0.4초, 전환해도 B 반격 아머 유지 |
| [Day12SwitchTests.cs](../Assets/Tests/PlayMode/Day12SwitchTests.cs) | 7 | 두 씬의 Tab+J 동시 입력, 양 캐릭터 준비/타격 중 취소·원래 쿨타임, 공유 HP·손상 단계·HUD 초상/이름, 웅크림·공중 전환, 점액 감속, 피격·히트스톱·사망·비활성 중 차단, 실제 살덩이 벽 잡힘 및 히트스톱 중 R 복구 |

`CombatDecisionTests.EveryPlayableSceneUsesTheDay5AttackProfiles`에 보스 예배당도 추가했다.
공격 중 전환 취소 검사는 짧은 준비 단계의 정확한 시점에 `SwitchCharacter()`를 호출하고,
Tab 입력 통합 검사는 실제 키보드 이벤트를 사용한다. 표시 검사는 스프라이트와 HUD 참조를 확인한다.

## 검증 기록

- 최초 Unity 기동은 기존 Day 11 빌드가 같은 프로젝트를 점유해 검사 시작 전 종료했다. 기존 프로세스가 정상 종료할 때까지 기다린 뒤 실행했다. 로그: `Logs/Day12/Targeted/`.
- 1차 Day 12 검사: 9/10 통과. 점액 검사에서 바닥 마찰이 적용된 실제 속도를 설정 속도 1.8과 직접 비교한 것이 실패 원인이다. 실제 전환 전후 속도를 비교하고 감속 배율 0.5를 별도로 확인하도록 수정했다. 런타임 수정은 없다. [1차 XML](../Logs/Day12/TargetedHost/playmode-results.xml).
- 최종 전체 Play Mode: **113/113 통과**(전체 125, Explicit 12 건너뜀), 실패 0, 종료 코드 0. 기존 103개와 신규 10개가 모두 통과했다. [전체 XML](../Logs/Day12/Full/playmode-results.xml) · [로그](../Logs/Day12/Full/playmode-tests.log) · [종료 코드](../Logs/Day12/Full/exit-code.txt).
- Unity 6000.0.81f1, `-batchmode -nographics --burst-disable-compilation -runTests -testPlatform PlayMode`로 실행했다. Day 12만 다시 실행할 때는 `-testFilter Day12`를 추가한다. 새 클래스에는 Explicit 캡처가 없다. C# 컴파일 오류·경고는 없었다.

- 재검증(2026-11-11 23:45, Claude): 전체 **113/113**, Day 12만 **10/10** 재현. `Logs/ClaudeRuns/20261111_234543_day12_verify_full`, `Logs/ClaudeRuns/20261111_235532_day12_verify_only`. 2026-11-12 사용자가 현재 상태로 확정.

기존 파일 사본은 `Logs/Day12/Before/`에 있다. 작업 중 최신 Day 11 문 자동 입장 인계 기록을 확인했으며,
해당 기록을 보존하기 위해 문서 수정 직전 사본도 `Logs/Day12/BeforeLatest/`에 저장했다.

## 유지하는 규칙과 남은 확인

- HP는 두 캐릭터가 공유하는 현재 임시 정책(100)을 유지한다. 잡힘 중 Tab 탈출은 허용하지 않는다.
- 공격 전환은 준비/타격을 취소하고 원래 쿨타임을 보존한다. Tab+J 동시 입력은 새 공격이나 쿨타임을 발생시키지 않는다.
- 방패 가드·정신 상태·별도 캐릭터 HP·성모 플레이 캐릭터 등 추가 능력은 이번 범위가 아니다.
- 자동 검사는 수동 조작감·렌더링 검수를 대신하지 않는다. 이번에는 화면 캡처나 새 Windows 빌드를 수행하지 않았다.
- 다음 작업은 Day 13 외형 손상 0~4단계 판정이다. Day 14 함정 반응 도트와 Day 17 HUD 상태 변형은 그림 준비가 필요하다.
