# 보스 신규 자세 제작 프롬프트

> **2026-11-18 현재 적용하지 않는 과거 제작 기록이다.** 사용자가 그림체 불일치를 지적했으므로 생성 포즈를 베이크 입력에서 제외했다. 현재 보스 캐릭터는 원래 게임 전신 도트로 관절을 구성하며, 마지막 사진의 무릎 자세로 공통 마무리를 통일했다. 최신 결과는 `CONTACT_RESULT_20261118.md`를 본다.

사용 도구: 내장 `image_gen.imagegen`, `transparent_background=true`. 원본 게임 캐릭터의 `*_start.png`와 `*_boss_pattern.png`만 `referenced_image_paths`로 전달했다. 다른 게임 사진은 전달하지 않았다. 원본 결과는 `Assets/SaintKnightSpriteCook/Art/DefeatPoses/*_pose_source.png`이며 직접 런타임에 표시하지 않는다.

## 공통 제작 지시

Edit the exact existing tiny pixel art game character. Input 1 is the identity/edit target; input 2 contains poses of the SAME character as supporting game-art reference. Production atlas: three columns by two rows, six isolated full-body poses, identical tiles, transparent RGBA background, ample margins. No text, grid, background, glow, blur or smooth painted shading. Preserve original body proportions, clothes, hairstyle, palette, ornaments and miniature pixel facial marks. No redesign, extra detail, increased bust, exposed torso, bare thighs, nudity or sexual content. No enemy, tentacles, restraints, liquid or VFX in this pose atlas. Existing Unity game assets, chunky hard pixel clusters at original tiny native resolution.

Poses, left-to-right and top-to-bottom:

1. Standing upright in front three-quarter view, arms lowered slightly away from torso, weary head bowed.
2. Compact crouch/kneel with face and clothed shoulders visible and the body covered by the existing outfit.
3. Lifted upright, hands bent near shoulder height, legs hanging together fully clothed.
4. Lifted upright with BOTH ARMS clearly extended left and right, wrists above shoulders, broad readable silhouette; legs hang down close together.
5. Seated upright, hands resting low near sides, knees together under the original clothes.
6. Kneeling, head lowered and torso leaning slightly forward, hands near chest, fully clothed.

These are ordinary nonsexual fantasy combat incapacitation poses. Each separate sprite must retain the same character and pixel scale as the source.

## 세실리아 추가 지시

Cecilia, adult silver-haired gothic cleric, long flowing silver hair, ornate ivory-lavender and antique-gold FULL LENGTH layered ceremonial dress, sleeves, high neck, covered torso and legs, veil/headpiece, tiny chain-flail. Approximate standing height55 native pixels. Long dress hangs vertically when lifted and covers knees when seated or crouching. Match source ornaments and silhouette; do not invent another heroine.

## 아델라 추가 지시

Adela Crow, adult crimson-haired knight, long high red ponytail, ornate charcoal/dark-steel full plate armor, enclosed breastplate and waist, gold trim, armored legs and boots, deep crimson cape, long sword and pointed shield. Approximate standing height62 native pixels. Weapons remain small, away from face and pose silhouette, may hang low in relaxed hands. Match the actual source knight; do not invent another red-haired heroine.

## 실제 적용 정리

`SKNativeDefeatBuilder.BossPose`는 512×512칸에서 alpha220 이상인 실제 형상만 읽고, 자세별 높이(서기/들림55·62px)를 맞춘다. 반투명 생성 후광은 가져오지 않는다. 색은 원본 Idle 팔레트의 가장 가까운 색으로 제한하고, 원본 Idle 머리 픽셀을 넣어 작은 얼굴·장식을 유지한다. 적용 크기 검수본은 `*_poses_native.png`다. 가느다란 촉수는 전경·후경의 연속 곡선으로 별도 베이크하므로 원본 얼굴 위에 큰 촉수 조각을 덮지 않는다.

이 결과는 AI 보조 신규 자세를 포함하며 원본 포즈를 그대로 복사한 장면과 구분한다. 얼굴·팔레트 고정과 원본 파일 보존은 사진과의 정량90% 일치율을 뜻하지 않는다.
