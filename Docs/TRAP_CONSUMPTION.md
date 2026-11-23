# 함정 포획·전신 흡수

**최신 전용 모션 후속(2026-11-12):** 기존 인물 도트 기반 6시트·138프레임을 실제 게임에 연결했다. 사진1 촉수 나선 감김·사진2 벽 쪽 수평 끌림·승인된 점액을 반영했다. [142/142 전체 검증과 실제138장 기록](TRAP_DEDICATED_MOTION.md)을 따른다. 아래 초기 그림·절차적 레이어·이전 검사 증빙은 당시 기록이다.

작업일: 2026-11-12 (Asia/Seoul).

사용자 요청: 기존 게임 도트에 점액·촉수가 전신을 감싸 잡아먹는 모습을 적용하고,
살덩이 벽은 일정 시간 저항하지 않으면 완전히 삼키도록 한다. 세부 규칙은 기존 문서를 바탕으로 선택하도록 위임받았다.
첨부 이미지는 감김·흡수의 진행 방향만 참고한다. 기존 복장과 도트·원작 에셋 비재사용 원칙을 유지한다.

## 선택한 규칙

| 함정 | 포획 전 | 포획 뒤 |
| --- | --- | --- |
| 점액 | 기존 걷기 ×0.5. 땅에서 연속 1.25초 머무르면 포획. 나가거나 점프하면 대기 시간 초기화 | 기존 연타 입력으로 저항/탈출. 마지막 저항 후 4초가 지나면 완전 흡수 |
| 바닥 촉수 | 기존 예고·피해 15 유지. 해당 촉수의 실제 승인된 타격 뒤에만 포획 | 위와 같은 저항/탈출·무저항 흡수. 무적으로 피해가 거부되면 포획하지 않음 |
| 살덩이 벽 | 기존 접근 예고와 잡기 유지 | 기존 4초 후 내던지기 대신 마지막 저항 후 4초가 지나면 완전히 삼킴 |

J·Space·새 A/D/방향키 입력은 기존 `SKDay01Player`가 `SKGrabState`에 전달한다.
저항 한 번마다 무저항 시간이 다시 계산되고, 충분히 연타하면 탈출한다. 잡힘 중 이동·공격·Tab 전환은 기존 계약대로 차단된다.
점액·촉수는 0.75초마다 HP 4, 벽은 기존 0.5초마다 HP 4를 소모한다. 탈출 후 면역은 0.6초다.
완전히 흡수되거나 잡힘 중 HP가 먼저 0이 되면 전투 불능이며, 닫힌 함정이 캐릭터를 가린다.
R 또는 기존 1.6초 자동 재시도로 방과 표시를 복구한다. 정신 상태와 별도이며 HP 0 문구는 `INCAPACITATED`를 유지한다.

## 원본 도트의 표시

- 생성한 새 인물 시안은 사용하지 않았다. `trap-wrap-v2/README.md`에 미적용 시안으로 보관했다.
- `SKCharacterSpriteView`는 기존 잡기 성공·버팀·저항·탈출 포즈를 읽는다. 촉수에 잡힌 첫 0.16초는 기존 촉수 피격 포즈를 보여 준다.
- `SKTrapEnvelopeView`는 원본 스프라이트 위에 같은 해상도의 별도 픽셀 레이어를 그린다. 초반부터 몸통·팔·다리를 붙들고, 저항이 없을수록 아래에서 위로 차올라 전신을 덮는다.
- 벽은 기존 합성 도트의 캐릭터 위치에 레이어를 맞추고, 완료 시 기존 닫힌 벽 그림만 남긴다. 그림은 피해·포획·흡수 시간을 소유하지 않는다.
- 원본 캐릭터 PNG·기존 씬·기존 `.meta` GUID를 보존한다. 레이어는 플레이 중 생성되며 Assets의 캐릭터 픽셀을 덮어쓰지 않는다.

## 변경과 보존

런타임: `SKEnvironmentalCapture`, `SKSlimePool`, `SKFloorSpike`, `SKFleshWall`, `SKTrapEnvelopeView`, `SKCharacterSpriteView`.
`SKHitbox.HitAccepted`는 해당 타격의 피해가 승인된 때만 통지하며, 기존 일반 피해·아군 제외·중복 타격 방지·히트스톱을 유지한다.
`SKGrabState.TryBegin`의 기본 면역 규칙은 유지하고, 촉수의 승인된 직후 경로에서만 방금 부여된 면역을 허용하는 선택 인수를 쓴다.
변경 전 사본: `Logs/TrapConsumption/Before/`. 앞선 시안 작업 시작 시의 아트·가이드는 `Logs/TrapWrapV2/Before/`에도 있다.

## 검증

- 첫 전용 실행은 15개 중 13개 통과했다. 실패 2개는 히트스톱이 끝나기 전 탈출 입력을 보내던 표시 검사와,
  벽 앞에서 직접 HP를 회복한 뒤 정상적으로 새 예고(`Tell`)가 시작되는 것을 실패로 보던 검사였다.
  입력 대기·복구 단정을 보완했고 해당 수정에서 런타임 규칙은 바꾸지 않았다. `Logs/TrapConsumption/Targeted/`.
- 보완 후 **16/16 통과**, 실패·건너뜀 0, 종료 코드 0: 신규 기능 13개 + 기존 표시 2개 + Explicit 그래픽 캡처 1개.
  [XML](../Logs/TrapConsumption/TargetedVisual/playmode-results.xml) · [로그](../Logs/TrapConsumption/TargetedVisual/playmode-tests.log).
- 캡처에서 초반 가림이 과한 것을 확인해 원본 자세의 머리 위치를 읽고 얼굴을 마지막 단계까지 보존하도록 조정했다.
  최종 그래픽 캡처 검사 **1/1 통과**, 종료 코드 0: [XML](../Logs/TrapConsumption/VisualFinal/playmode-results.xml).
  [최종 캡처 목록](../Logs/TrapConsumption/Captures/20261112_000151/index.md)의 18장은 두 캐릭터 × 점액/촉수/벽 × 저항/진행/완료다.
  원본 얼굴·자세, 바닥·벽 위치, 완료 후 몸체 잔존/중복 없음은 이미지로 확인했다. 반대쪽 접근 반전은 이 캡처만으로 별도 실증하지 않았다.
- 최종 전체 Play Mode **136/136 통과**(전체 149, Explicit 13 건너뜀, 실패 0, 종료 코드 0).
  기존 123개와 신규 기능 13개가 모두 통과했다. Explicit 시각 캡처 1개는 위 그래픽 실행에서 별도로 통과했다.
  [전체 XML](../Logs/TrapConsumption/Full/playmode-results.xml) · [로그](../Logs/TrapConsumption/Full/playmode-tests.log) · [종료 코드](../Logs/TrapConsumption/Full/exit-code.txt).
  C# 컴파일 오류·경고는 없었다. 관련 16개 소스 해시는 [validation-inputs.json](../Logs/TrapConsumption/validation-inputs.json)에 기록했다.

원본 캐릭터 아틀라스 2개·기존 `.meta`·애니메이션 시간표·기존 씬 등 21개 보존 항목의 해시가 모두 일치했다.
[보존 검사](../Logs/TrapConsumption/asset-preservation.json).
수동 조작감 검수와 새 Windows 빌드는 수행하지 않았다. 캡처 검사는 그래픽을 켜고 정확한 메서드 필터로 실행한다:
`Logs/TrapConsumption/Run-PlayMode.ps1 -RunName <새 이름> -Graphics -TestFilter TrapConsumptionVisualTests.CaptureNativeDotCaptureResistanceAndConsumption`.
일반 전체 회귀에는 `-Graphics`와 `-TestFilter`를 생략한다. Explicit 화면 캡처는 그래픽 없는 실행에서 선택하지 않는다.
