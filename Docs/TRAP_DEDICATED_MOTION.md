# 함정 전용 도트 모션 — 제작·검증 기록

작성일: 2026-11-12 (Asia/Seoul). **최종 v3 전용 시트 6개를 게임에 연결했고 전체 Play Mode 142/142, 전용 기능·그래픽 실행 22/22가 통과했다. 실제 씬 캡처 138장과 6개 시트 프리뷰의 이미지 QA를 완료했다.**
앞선 절차적 감김 레이어 구현의 전체 **136/136**은 [이전 작업 증빙](../Logs/TrapConsumption/Full/playmode-results.xml)이며,
이번 전용 모션의 통과 결과로 사용하지 않는다. 이 문서는 제작 기준·실제 확인한 결과·남은 검증을 구분한다.

## 사용자가 요청한 모습

기존 캐릭터 도트가 몸을 움직이며 저항하고, 함정이 점점 집어삼키는 **전용 도트 모션**이 필요하다.
대상은 세실리아·아델라 × 점액·촉수·살덩이 벽 **6조합 전부**다. 생성한 새 인물 시안은 재사용하지 않는다.
인물은 기존 원본 도트의 복장·얼굴·신체 비율과 픽셀을 유지한다.

| 함정 | 확정된 표현 방향 | 단계가 진행될 때 읽혀야 할 변화 |
| --- | --- | --- |
| 점액 | 사용자가 승인한 점액 재질 방향 | 발목에서 시작해 다리·허리·몸통을 잠기게 하고, 몸이 아래로 끌리며 끝까지 팔을 뻗어 버팀 |
| 촉수 | 사용자 사진 1: 팔·몸통·다리에 나선 코일이 감김 | 바닥에 연결된 촉수가 여러 신체 부위를 붙든 뒤 감김·당김이 깊어짐. 팔·다리 실루엣과 코일 앞뒤가 구분됨 |
| 살덩이 벽 | 사용자 사진 2: 기존 벽 왼쪽 틈으로 캐릭터가 오른쪽으로 끌림, 좌우 접촉 시 반전 | 벽 질량과 몸의 연결을 유지하며 벽 안쪽으로 수평 이동·파묻힘. 몸이 바닥으로만 내려가는 점액 모션과 방향을 구별함 |

첨부 사진은 감김·끌림 방향의 참고다. 원작 인물·복장·이미지·코드는 복사하지 않는다.
새 함정 재질은 캐릭터 없는 6상태 소재로 준비하고, 인물은 native 원본 포즈로 합성한다.

## 23프레임 × 6조합 = 138프레임

각 조합의 시트는 **8열 × 3행**이며 23셀을 사용한다. 마지막 여분 셀은 동작 프레임으로 세지 않는다.
실제 manifest와 `SKTrapAnimationLibrary`는 다음 프레임 순서를 공유한다.

| 프레임 구간 | 수 | 내용 |
| --- | --- | --- |
| 0~5 | 6 | 집어삼킴 단계 0~5: 접촉 → 다리 → 허리 → 몸통 → 머리/손끝 → 완료 직전 |
| 6~17 | 12 | 각 단계별 저항 2장. `6 + 단계×2`, `7 + 단계×2` |
| 18~20 | 3 | 탈출: 당겨 버팀 → 끊고 빠져나옴 → 원래 움직임으로 연결 |
| 21~22 | 2 | 완전 흡수: 인물이 없는 닫힌 덩어리 2장 |

단계별 목표:

| 단계 | 시각 목표 | 인물 확인 기준 |
| --- | --- | --- |
| 0 접촉 | 함정이 발·다리 또는 벽 쪽 몸에 붙기 시작 | 얼굴·복장·몸의 방향이 즉시 읽힘 |
| 1 다리 | 다리 코일/점액 침수 또는 벽 쪽 다리 당김 | 무릎·허리·상체가 접촉 단계와 다른 힘을 받음 |
| 2 허리 | 하체 가림이 진행되고 허리가 잡힘 | 상체가 당기는 방향의 반대로 버티며 팔의 위치가 바뀜 |
| 3 몸통 | 몸통까지 감기거나 벽 안에 들어감 | 얼굴과 저항하는 팔을 읽을 수 있음 |
| 4 머리/손끝 | 머리와 팔의 마지막 노출이 남음 | 새 얼굴을 덧그리지 않고 원본의 마지막 실루엣을 보존 |
| 5 직전 | 대부분 잠겨 마지막 머리/손끝만 남거나 이미 질량 안에 들어감 | 인물이 전부 가려져도 4초 종료 전까지 내부 저항 가능. 마지막 저항 2장과 완료 2장은 재질 움직임으로 구분 |
| 완료 2장 | 닫힌 점액·촉수·벽 질량 유지 | 얼굴·머리카락·손·무기·방패·몸체 잔존 및 이중 그림 없음 |

몸이 보이는 구간의 저항 2장은 팔·어깨·무릎·상체 기울기 중 실제 변화가 읽혀야 한다.
몸이 남아 있는데 재질의 명암만 바뀌고 인물 포즈가 고정되어 있으면 몸부림의 증거로 계산하지 않는다.
단계 5에서 이미 전신이 가려진 조합은 내부 저항에 따른 재질의 당김/움찔거림 2장으로 구분한다.
아주 작은 포즈 차이는 정수 픽셀 이동과 함께 읽을 수 있지만, 캐릭터 전체를 번갈아 흔들어 접지점이 튀지 않도록 한다.

## 확인한 native 원본 포즈

기존 [세실리아 시간표](../Assets/SaintKnightDay06/Resources/SaintKnightDay06/cecilia_motion_frames.json)와
[아델라 시간표](../Assets/SaintKnightDay06/Resources/SaintKnightDay06/adela_motion_frames.json), 두 원본 PNG를 읽었다.
다음은 **0부터 시작하는 원본 셀 번호**이며 전용 아틀라스의 새 프레임 번호와 구분한다.

| 동작 | 세실리아 원본 셀 | 아델라 원본 셀 | 용도 |
| --- | --- | --- | --- |
| Pounced | 43~49 | 53~59 | 서 있음→기울어짐→무릎이 접히는 초기 포획 변화 |
| Held | 39~42 | 48~52 | 힘을 받으며 낮게 버티는 몸체 |
| Resist | 50~58 | 60~68 | 팔·어깨·몸통을 틀며 버티는 저항 변화 |
| Escape | 59~67 | 69~77 | 움츠림→빠져나옴→기본 자세 연결 |

최종 `build-report.json`에서 확인한 **실제 사용 원본 셀**이다. 같은 캐릭터의 세 함정은 이 포즈 원본을 공유하고 재질·가림·위치 이동을 다르게 적용한다.

| 구간 | 세실리아 실제 원본 셀 | 아델라 실제 원본 셀 |
| --- | --- | --- |
| 집어삼킴 0~5 | 43, 45, 47, 39, 53, 56 | 53, 55, 57, 48, 63, 66 |
| 단계 0 저항 2장 | 44, 45 | 54, 55 |
| 단계 1 저항 2장 | 50, 51 | 60, 61 |
| 단계 2 저항 2장 | 52, 53 | 62, 63 |
| 단계 3 저항 2장 | 54, 55 | 64, 65 |
| 단계 4 저항 2장 | 56, 57 | 66, 67 |
| 단계 5 저항 2장 | 57, 58 | 67, 68 |
| 탈출 3장 | 60, 63, 67 | 70, 73, 77 |

원본에 없는 팔·얼굴을 생성하거나 몸통을 세로로 압축/늘이지 않았다.
아래로 잠김·벽 쪽 끌림은 원본 포즈 선택, 정수 픽셀 위치 이동, 함정의 앞/뒤 가림으로 표현한다.
팔을 뻗는 목표는 원본의 실제 팔 실루엣이 있는 프레임을 선택해 충족한다. 부족하면 해당 한계를 기록하고 원본 외형을 임의로 재구성하지 않는다.

원본 기준은 셀 **128×96px**, **48 PPU**, Point·무압축이다. 기존 지상 발 기준과 피벗을 유지한다.
선 자세 기준 키는 세실리아 약 55px, 아델라 약 62px이며 무기 여백으로 몸체 크기를 늘리지 않는다.
최종 전용 시트는 바닥형 셀 128×96px, 벽형 셀 160×112px이다. 벽의 캔버스만 넓히며 원본 몸체는 1:1 정수 이동으로 유지한다.
manifest의 PPU는 48이며, 바닥형은 기존 피벗, 벽형은 하단 피벗 `(0.5, 0)`이다.

## 유지할 게임 규칙

전용 시트는 표시만 담당한다. 피해·포획·저항·탈출·흡수 시간은 기존 런타임이 소유한다.
점액의 연속 접지 1.25초와 걷기 ×0.5, 촉수의 승인된 15 피해 직후 포획, 마지막 유효 저항 뒤 무저항 **4초**,
기존 드레인·연타 탈출·R·자동 재시도 **1.6초** 정책은 바꾸지 않는다.
회복·재시도·비활성화로 복구하고 HP 0의 `INCAPACITATED`와 별도 정신 상태 정책을 유지한다.

히트스톱 중에는 게임과 전용 애니메이션이 함께 멈춰야 한다. 전용 프레임이 피해·포획을 추가 발동하지 않는다.
탈출의 도약·관성은 실제 물리를 읽고, 아트 프레임이 캐릭터 위치를 중복 이동시키지 않는다.
원본 몸체 PNG·기존 씬·기존 `.meta` GUID를 보존하고 전용 에셋을 [새 Resources 경로](../Assets/SaintKnightDay10/Resources/SaintKnightDay10/TrapMotion/)에 추가했다.
6개 PNG와 각 manifest·`.meta`의 적용 기록은 [applied-assets.json](../Logs/TrapDedicatedMotion/applied-assets.json),
기존 자산 보존은 [asset-preservation.json](../Logs/TrapDedicatedMotion/asset-preservation.json)에 기록했다.

## 프레임·게임 QA 기준

| 확인 대상 | 합격 기준 | 현재 결과 |
| --- | --- | --- |
| 6개 시트와 138프레임 | 전 조합에 6진행·12저항·3탈출·2완료가 실제 존재 | 최종 v3 보고서 6×23 및 각 23개 고유 프레임 확인 |
| 몸부림 | 단계별 원본 포즈 변화, 전신이 가려진 마지막 구간은 재질 저항 차이 | 최종 시트·실제 씬 138장 확인 |
| 삼켜짐 진행 | 포즈별 원본 대비 가림이 진행되고 힘의 방향이 읽힘 | 최종 시트·실제 씬 확인; 포즈가 달라 절대 노출 픽셀 수는 단조 감소하지 않을 수 있음 |
| 함정별 차이 | 점액은 침수, 촉수는 팔다리 나선 코일, 벽은 벽 쪽 수평 끌림 | 최종 시트·실제 씬 확인 |
| 원본 인물 | 얼굴·복장·머리/몸 길이가 원본과 일관되고 생성 인물 혼입 없음 | 최종 시트·실제 씬 및 1:1 원본 배치 보고 확인 |
| 초반·직전·완료 | 초반 얼굴 식별, 후반 머리/손끝 또는 내부 진입, 완료에 인물 픽셀 없음 | 이미지 확인; 최종 v3 완료 12프레임의 VisibleBodyPixels=0 |
| 앵커·반전 | 지면/벽 고정, 좌우 반전 때 인물·함정 방향/접점 일치 | 세실리아 왼쪽 접근·아델라 오른쪽 접근의 벽 반전 및 실제 바닥 접점 확인 |
| 중복 그림 | 단독 인물과 전용 합성이 겹치지 않고 완료 뒤 잔존 없음 | 실제 씬 138장 확인·전용 리소스 소유/레이어 단정 통과 |
| 탈출·R·재시도 | 현재 전용 모션이 해제되고 잔여 감김/숨김/타이머 없음 | 전용·전체 실행 통과·탈출 18장과 완료 12장 이미지 확인 |
| 히트스톱 | 게임 타이머와 선택 프레임이 함께 멈춤·재개 | `HitStopFreezesTheSelectedFrameAndTheAuthoritativeProgressTogether` 통과 |
| 기능 회귀 | 신규 전용 검사와 기존 전체 회귀의 XML·종료 코드 확인 | 전용·그래픽 22/22 및 전체 142/142 통과, 모두 종료 코드 0 |

6개 프리뷰를 읽어 모든 프레임의 포즈·가림·방향·접점을 검수했다.
이후 실제 씬에서 진행률 **0 / 0.18 / 0.35 / 0.52 / 0.70 / 0.88**와 완료를 캡처했다.
J 입력은 무저항 진행률을 초기화하므로 진행 샘플과 저항/탈출 샘플은 별도 초기화한 실행으로 구분한다.
저항 2프레임과 탈출 3프레임은 별도 시트 샘플과 실제 동작 연결을 함께 확인한다.

기존 Explicit 그래픽 검사 [TrapConsumptionVisualTests.cs](../Assets/Tests/PlayMode/TrapConsumptionVisualTests.cs)의
`CaptureNativeDotCaptureResistanceAndConsumption` 메서드 내부를 보강했다. Explicit 검사 수는 1개를 유지하며 전용 그래픽 실행에서 통과했다.
파일명은 `<character>_<trap>_stage0~5.png`, `_consumed_frame0~1.png`, `_resist_stageN_frame0~1.png`, `_escape_frame0~2.png`다.
실제 씬에서 23장 × 6조합 = **138장**을 찍고 `SKTrapAnimationLibrary.Owns` 및 `Sample` 참조로 실제 전용 리소스 사용을 단정한다.
출력은 `Logs/TrapDedicatedMotion/Captures/<UTC시각_고유값>/`의 새 폴더이며 파일 중복 시 실패해 이전 증빙을 보존한다.
초기 버전 검사와 `.meta` 사본은 `Logs/TrapDedicatedMotion/BeforeVisualEvidence/`에 보존했다.

## 제작·검증 기록

| 조합 | 아틀라스/프리뷰 | 이미지 QA | 런타임·캡처 |
| --- | --- | --- | --- |
| 세실리아 × 점액 | [v3 최종 프리뷰](Design/Characters/trap-dedicated-motion/staging-v3/cecilia_slime_contact.png) | v2 전체·v3 최종 재확인, 탈출 재질 끝·침수 발끝 보완 확인 | [실제 씬 23장 QA](../Logs/TrapDedicatedMotion/QA/cecilia_world_354f8c84/cecilia_slime_world_contact.png) 완료 |
| 세실리아 × 촉수 | [v3 최종 프리뷰](Design/Characters/trap-dedicated-motion/staging-v3/cecilia_tentacle_contact.png) | v2 23장 이미지 확인·탈출 3포즈 정상, 최종 v3는 루트 검수 | [실제 씬 23장 QA](../Logs/TrapDedicatedMotion/QA/cecilia_world_354f8c84/cecilia_tentacle_world_contact.png) 완료 |
| 세실리아 × 벽 | [v3 최종 프리뷰](Design/Characters/trap-dedicated-motion/staging-v3/cecilia_wall_contact.png) | v2 23장 이미지 확인·마지막 재질 중복 보완 확인, 최종 v3는 루트 검수 | [실제 씬 23장 QA](../Logs/TrapDedicatedMotion/QA/cecilia_world_354f8c84/cecilia_wall_world_contact.png) 완료 |
| 아델라 × 점액 | [v3 최종 프리뷰](Design/Characters/trap-dedicated-motion/staging-v3/adela_slime_contact.png) | v2 전체·v3 최종 재확인, 탈출 재질 끝·침수 발끝 보완 확인 | [실제 씬 23장 QA](../Logs/TrapDedicatedMotion/QA/adela_slime_actual-contact.png) 완료 |
| 아델라 × 촉수 | [v3 최종 프리뷰](Design/Characters/trap-dedicated-motion/staging-v3/adela_tentacle_contact.png) | v2 23장 이미지 확인·탈출 3포즈 정상, 최종 v3는 루트 검수 | [실제 씬 23장 QA](../Logs/TrapDedicatedMotion/QA/adela_tentacle_actual-contact.png) 완료 |
| 아델라 × 벽 | [v3 최종 프리뷰](Design/Characters/trap-dedicated-motion/staging-v3/adela_wall_contact.png) | v2 23장 이미지 확인·수평 끌림·탈출 확인, 최종 v3는 루트 검수 | [실제 씬 23장 QA](../Logs/TrapDedicatedMotion/QA/adela_wall_actual-contact.png) 완료 |

v1 세실리아 벽의 20/23 고유 프레임은 v2에서 23/23으로 보완됐다. 촉수 탈출은 v2 재검수에서 두 캐릭터 모두 정상 3포즈를 확인했다.
v2에서 전달한 두 점액 시트 탈출 18/19셀의 직사각형 재질 절단은 v3에서 몸에 이어지는 가는 곡선 꼬리로 보완됐고,
침수 때 지면 아래로 발끝이 드러나는 부분도 정리됐다. 최종 점액 시트 2개를 직접 다시 읽어 두 보완을 확인했다.
최종 v3 전체 프리뷰는 루트도 검수했다. 미해결 아트 결함으로 기록한 항목은 현재 없다.

최종 스테이징 [build-report.json](Design/Characters/trap-dedicated-motion/staging-v3/build-report.json)은 각 시트의 원본 셀·이동·노출·가림 픽셀과 해시를 기록한다.
6개 시트 모두 23개 고유 프레임이며, 12개 완료 프레임의 인물 노출은 0이다. 전용 리소스 연결과 실제 씬 촬영·전체 회귀는 아래 기록으로 확인했다.
수동 조작감 검수와 새 Windows 빌드는 수행하지 않았다.

## 실제 씬 그래픽 실행과 양 캐릭터 138장 QA

- [전용·그래픽 XML](../Logs/TrapDedicatedMotion/TargetedVisual/playmode-results.xml): **22/22 통과**, 실패·건너뜀 0, [종료 코드 0](../Logs/TrapDedicatedMotion/TargetedVisual/exit-code.txt). 이 수치를 전체 회귀 결과로 쓰지 않는다.
- [실제 캡처 목록](../Logs/TrapDedicatedMotion/Captures/20261112_044531_553_0b3e2027/index.md): 양 캐릭터 × 세 함정 × 23장 = **138장**이 생성됐다.
- 세실리아의 점액·촉수·벽 **69장 전부**를 QA용 contact 3장으로 직접 읽었다. 진행 6단계·저항 단계별 2장·탈출 3장·완료 2장, 바닥/벽 접점, 원본 얼굴·몸 길이, 완료 뒤 몸체 잔존·중복 없음이 읽힌다. 심각한 시각 오류는 관찰하지 않았다.
- 아델라 점액·촉수·벽 **69장 전부**도 별도 담당자가 이미지 검수했다. [아델라 QA 기록](../Logs/TrapDedicatedMotion/QA/adela_actual-review.md)과 [원본/크롭 대응표](../Logs/TrapDedicatedMotion/QA/adela_actual-frames.json)에 결과를 남겼다. 세실리아와 합쳐 실제 캡처 138장 모두 검수 완료다.
- 세실리아 벽은 왼쪽 접근에서 오른쪽으로 들어가고, 아델라 벽은 오른쪽 접근에서 반전된 그림의 중심으로 들어가는 방향을 확인했다. 탈출도 각 접근 방향의 한 캐릭터만 복원되며 단독 그림 중복은 관찰되지 않았다.
- 촉수 초기·첫 저항 일부의 붉은 피격 틴트와 탈출 일부의 면역 깜빡임은 기존 `SKHealth` 피격·무적 표시와 함께 캡처됐다. 전용 아틀라스의 인물 색 변경으로 기록하지 않는다.
- QA용 crop/contact만 `Logs/TrapDedicatedMotion/QA/cecilia_world_354f8c84/`에 새로 만들었다. [qa-index.json](../Logs/TrapDedicatedMotion/QA/cecilia_world_354f8c84/qa-index.json)에 원본 69개 경로·SHA256·crop 좌표를 남겼으며 생성 전후 원본 해시가 같음을 확인했다. 게임 PNG는 수정하지 않았다.

## 최종 전체 회귀

- 전체 Play Mode **142/142 통과**(전체 **155**, Explicit **13** 건너뜀, 실패 **0**, 종료 코드 **0**).
  [전체 XML](../Logs/TrapDedicatedMotion/Full/playmode-results.xml) · [로그](../Logs/TrapDedicatedMotion/Full/playmode-tests.log) · [종료 코드](../Logs/TrapDedicatedMotion/Full/exit-code.txt).
- 실제 실행 시각은 **2026-11-12 13:46:25~13:49:18 KST**다. 기존 136개와 전용 모션 신규 기능 검사 6개가 통과했으며,
  Explicit 그래픽 검사는 위 22/22 실행에서 별도로 통과했다.
- 신규 검사는 6개 시트의 원래 배율·23개 고유 프레임, 저항 중 시각 깊이 보존 후 복귀, 실제 6조합 진행·저항·탈출·완료,
  히트스톱 동기화, R/비활성/캐릭터 전환 뒤 전용 프레임 잔류 없음을 검증한다.
- 최종 기록 전 문서 사본은 `Logs/TrapDedicatedMotion/BeforeFinalDedicatedDoc/`에 보존했다.
  이 문서 마무리에서는 코드·아트·씬을 수정하거나 Unity를 재실행하지 않았다.

## 후속: 후반 단계 가림 정리 (2026-11-12, Claude)

위 v3 시트 적용 뒤, 점액·촉수 4장의 칸 4·5·14~17(삼켜짐·저항의 5·6단계)을 교체했다.
v3는 이 단계에서 자세의 윗부분 몇 줄을 그대로 남겨 머리카락·팔 조각이 덩어리 밖이나 한가운데에 보였다.
교체본은 **소재의 실제 틈으로만 몸이 보이게** 했다(그 줄의 소재 좌우 범위 안쪽 투명 픽셀. 5단계는 소재 위로 나온 머리 유지).
인물 픽셀·벽 시트·JSON·`.meta`·코드는 그대로다. 위 절의 “후반 머리/손끝” 표현은 이 교체로 약해졌다.

- 제작: `Design/Characters/trap-downscale-claude/builder-clip/TrapMotionFrameBuilder.cs`(이 폴더 원본의 사본 + 조건 한 블록), 산출 `staging-clip/`.
  원본 `TrapMotionFrameBuilder.cs`로 다시 만들면 v3로 돌아가므로, 이후 재제작은 사본의 조건을 원본에 옮긴 뒤에 한다.
- 검증: 전용 모션 4/4, 전체 Play Mode **142/142 통과**(전체 155, Explicit 13, 종료 코드 0) [XML](../Logs/TrapClip/Full/playmode-results.xml).
  위 실제 씬 캡처 138장은 v3 기준이며 다시 찍지 않았다. 변경 전 시트 `Logs/TrapClip/Before/`.