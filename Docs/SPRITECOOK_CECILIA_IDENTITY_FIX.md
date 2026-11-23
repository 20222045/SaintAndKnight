# 세실리아 디자인 보존 수정

**최신 후속:** 요청7장 제작·규격 통일·59Sprite 분할/임포트를 완료했다. Play Mode149/149 통과. 게임 연결은 남아 있다. [최신 완료 기록](SPRITECOOK_DRAFTS_COMPLETED.md)을 따른다. 아래는 이전 단계의 이력이다.

작업일: 2026-11-13 (Asia/Seoul).

## 사용자 지시 및 처리

사용자 지시: **“모습 제대로 해서 다시 해 변형하지 말고.”**

SpriteCook Nano Banana Pro의 세실리아 v2는 긴 머리가 짧아지고 얼굴·상의가 단순한 로브 형태로 바뀌었다. 이 결과는 **디자인 불일치로 기각**했다. 기존 Resources 캐릭터나 게임 화면에는 적용하지 않았다. 해당 원격 에셋과 로컬 v2 파일은 이력용으로 보존하되 후속 작업의 디자인 기준으로 사용하지 않는다.

디자인 기준은 `Assets/SaintKnightDay06/Resources/SaintKnightDay06/cecilia_motion.png`이다. 첫 칸을 최근접 확대해 얼굴·긴 은발·어깨/몸통 장식·긴 치마 패널·사슬 무기를 확인했다. 기존 v1 적 쓰러짐 시트를 편집 대상으로 삼고, 승인된 원본 도트를 디자인 기준으로 직접 참조해 v3를 편집했다. 새 인물 디자인을 제안하거나 게임 원본을 덮어쓰지 않았다.

## 생성 이력 및 비용

- SpriteCook 도구 연결은 이제 정상이다. 기존 세실리아/아델라 참조 에셋을 복구했고, 내려받은 이미지의 픽셀이 로컬 원본과 일치함을 확인했다. 재업로드하지 않았다.
- 당시 잔액 32크레딧. GPT-Image-2 `high`·1K는 36/장이며, 2K의 28/장 옵션은 무료 계정에서 사용 불가였다. 초기 2K 가격 안내는 이 계정의 사용 가능 비용으로 정정했다.
- 사용자가 “2번으로 하다가 1번으로 해줘”라고 해, 마지막 선택인 Nano Banana Pro·1K로 적 쓰러짐 2장을 생성했다. 아델라/세실리아 각각 16, 총 32크레딧 사용. Pro에는 별도 `high` 옵션이 없으며 API의 `quality=medium` 저장값을 GPT 고품질 성공으로 해석하지 않는다.
- 세실리아 v2 기각 후 SpriteCook 잔액은 **0**이었다. 현재 v3는 ImageGen 스킬의 **내장 이미지 편집 도구**로 수정한 결과다. SpriteCook의 추가 생성·업로드·결제는 하지 않았다.
- 남은 SpriteCook 재생성 대상은 보스 패턴 2장·함정 반응 2장·HP 장식 1장이다. 아델라 v2는 원본 결과 저장·검수까지만 했고 정규화/게임 연결은 아직이다.

원격 에셋 ID와 해시는 `Design/Stage/spritecook/spritecook-assets.json`, 당시 작업 ID·전체 프롬프트는 `Logs/SpriteCookHigh/20261113/jobs.json`에 있다. 세실리아 v2 ID는 `2d92070c-6a58-4ca6-a12f-f94cd8aa3f29`이며 기각 상태를 기록했다.

## v3 산출물

| 산출물 | 위치 |
| --- | --- |
| 내장 편집 결과 원본, 1560×1008 | `Design/Stage/spritecook/4_enemy_death_cecilia_v3.png` |
| 128×96칸 정규화 시트·JSON, 4열×2행·8칸 | `Design/Stage/spritecook/identity-fixed-v3/4_enemy_death_cecilia_v3.{png,json}` |
| Unity Drafts 사본 | `Assets/SaintKnightSpriteCook/Drafts/4_enemy_death_cecilia_v3.{png,json}` |
| 원본/v2/v3 같은 키 비교 | `Design/Stage/spritecook/identity-fixed-v3/cecilia_design_comparison.png` |
| 전체 8칸 확대 및 동작 GIF | 같은 폴더의 `cecilia_identity_fixed_contact.png`, `cecilia_identity_fixed_motion.gif` |
| 편집 프롬프트·입출력 해시·검사 | `Logs/SpriteCookIdentityFix/20261113/` 및 `identity-fixed-v3/validation.json` |

정규화는 `Design/Stage/spritecook/normalize_cecilia_identity.py`로 재현한다. 생성 이미지의 실제 8개 본체 영역을 찾아 분리하고, 같은 배율·최근접 보간으로 맞춘다. 원본 캐릭터의 **32색만 사용**, 첫 서 있는 자세 **55px**, 모든 칸 발높이 **y=92**를 유지한다. 분리된 가장자리 잡티·충격 먼지는 제외했다. 512×192의 투명 PNG이며 새 Drafts PNG에만 새 GUID와 48 PPU·Point·무압축 설정을 부여했다. 아직 일반 Texture이며 Sprite 분할·게임 상태 연결은 하지 않았다.

```powershell
python Docs/Design/Stage/spritecook/normalize_cecilia_identity.py --sync-drafts
```

## 실제 검증

- 원본·기각 v2·교정 v3를 같은 55px 키로 맞춰 어두운/밝은 배경에서 비교했다. 긴 은발과 장식 의상을 확인하고 전체 8프레임을 검수했다.
- 규격 검사: 8개 비중복 프레임, 본체 잘림 없음, 원본 32색만 사용, 알파 0/255, 발 정렬, 기존 원본 해시 보존. 모두 통과했다.
- 별도 출력 폴더로 다시 생성한 PNG/JSON과 Drafts 사본의 SHA-256이 일치한다. 기존 PNG `.meta`도 그대로다.
- Unity PNG/JSON 임포트 확인. 전체 Play Mode **146/146 통과**(전체159, Explicit13, 실패0, 종료0). [XML](../Logs/SpriteCookIdentityFix/20261113/PlayMode/playmode-results.xml). C# 컴파일 오류 없음.
- Resources 캐릭터·C#·기존 씬은 변경하지 않았고 게임 내 시각 검수는 하지 않았다. 이 산출물은 디자인 교정된 **별도 시안**이다.

변경 전 문서·v2 기록은 `Logs/SpriteCookIdentityFix/20261113/Before/`. 초기 점액 작업 및 그 당시 146/146 결과는 [기존 검토 기록](SPRITECOOK_DRAFTS_REVIEW.md)에 보존한다.

## 후속 작업의 디자인 기준

세실리아는 위 원본의 얼굴 비율·긴 은발·장식 의상·체형·무기를 유지한다. 짧은 머리의 v2를 참조하거나 새 의상으로 재해석하지 않는다. 모션 수정 시 원본 단일 포즈 확대본과 기존 디자인이 맞는 시트를 직접 참조해 편집하고, 실제 게임 크기로 원본과 비교한다. 단순히 `reference_asset_id`를 전달했다는 이유로 디자인 보존을 통과 처리하지 않는다.
