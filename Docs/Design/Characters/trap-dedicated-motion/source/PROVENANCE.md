# 전용 함정 모션 소재

2026-11-12. Built-in `image_gen`으로 캐릭터가 없는 함정 소재만 제작했다.
캐릭터 얼굴·복장·몸체는 이 도구로 다시 그리지 않으며, 기존 게임 아틀라스의 도트를 합성 시 사용한다.

| 파일 | 참고한 프로젝트 소재 | 프롬프트 |
| --- | --- | --- |
| `slime_material.png` | `Assets/SaintKnightDay10/Art/slime_pool_f01.png` | `slime_prompt.txt` |
| `tentacle_material.png` | `Assets/SaintKnightDay10/Art/floor_tentacle_rise_f01.png` | `tentacle_prompt.txt` |
| `wall_material.png` | `Assets/SaintKnightDay10/Art/flesh_wall_idle_f01.png` | `wall_prompt.txt` |

세 파일은 1536×1024, 가로 3칸·세로 2칸으로 진행 단계 6개를 담는다.
슬라임 형태는 사용자가 승인했다. 최신 사용자 지시에 따라 촉수는 사진 1의 나선 감김 구조,
벽은 사진 2의 옆에서 벽 안으로 끌려 들어가는 진행을 참고한다. 첨부 이미지의 픽셀을 게임 에셋에 복사하지 않는다.

소재의 투명 영역에 남은 RGB 및 부드러운 가장자리는 패킹에서 알파 기준으로 정리한다.
이 기술 처리는 캐릭터 원본의 색·외형 변경과 구분한다. 실제 적용 여부와 결과는 상위 폴더의 제작 기록을 따른다.
