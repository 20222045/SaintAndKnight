# GothicVania Church Pack — 출처와 라이선스

- 제작: Luis Zuno (@ansimuz)
- 받은 곳: https://opengameart.org/content/gothicvania-church-pack (`gothicvania church files.zip`, 732,593 bytes, 2026-11-08, 사용자 승인 후 다운로드)
- OpenGameArt 라이선스 표기: CC0
- 팩 안의 [public-license.txt](public-license.txt): 퍼블릭 도메인, 개인·상업 용도 자유, 출처 표기 불필요(표기는 환영)

## 이 폴더에 둔 파일

ZIP의 `ENVIRONMENT/`에서 `backgrounds.png`, `column.png`, `tileset.png`와 라이선스 파일만 복사했다. 캐릭터·적 스프라이트, PSD, 음악은 가져오지 않았다.

## 로비 v6에서 쓴 부분

| 이름 | 원본 영역 | 처리 |
| --- | --- | --- |
| `ext_column` 해골 얼굴 기둥 + 랜턴 | `column.png` (0,66)-(85,190) | 오른쪽 랜턴 잘라냄, 왼쪽 뒤 모서리 |
| `ext_gargoyle` 가고일 석상·묘 십자가 | `backgrounds.png` (469,104)-(540,192) | 왼쪽 중간 바닥 |
| `ext_candles` 촛불 무더기 | `backgrounds.png` (105,148)-(156,192) | 가고일 발치 |

처리 방식: 배경색 (39,38,56)을 투명 처리 → 채도를 80% 빼고 밝기를 우리 고유색 수준(×1.55)으로 맞춤 → 따뜻한 색(불꽃·랜턴 유리)은 발광으로 분리 → 로비의 조명·저채도·팔레트 디더를 똑같이 거침. 코드는 [make_lobby_v6.py](../../make_lobby_v6.py)의 `[v6] 외부 에셋` 구간.
