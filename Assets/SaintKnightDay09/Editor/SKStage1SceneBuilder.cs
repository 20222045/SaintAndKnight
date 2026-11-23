using System;
using System.IO;
using System.Linq;
using SaintKnight.Day01;
using SaintKnight.Day02;
using SaintKnight.Day04;
using SaintKnight.Day08;
using SaintKnight.Day10;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SaintKnight.Day09.Editor
{
    /// <summary>
    /// Builds the Day 9 first stage (ruined cathedral) and the boss chapel behind its door from the Day 8
    /// scene; the Day 8 file is not saved. stage1_layout.json (Docs/Design/Stage/make_day09_art.py) holds the
    /// colliders in art pixels (x right, y down, 48 PPU); stage1_bg.png is the pixel redraw aligned to it
    /// (Docs/Design/Stage/pixel-redraw-v1), boss_chapel.png is Docs/Design/Stage/boss-chapel-v1.
    /// </summary>
    public static class SKStage1SceneBuilder
    {
        public const string ScenePath = "Assets/SaintKnightDay09/Scenes/Day09_Stage1.unity";
        private const string Day08Path = "Assets/SaintKnightDay08/Scenes/Day08_Scrolling.unity";
        private const string Art = "Assets/SaintKnightDay09/Art/";
        private const string TrapArt = "Assets/SaintKnightDay10/Art/";

        [Serializable] private class Solid { public string name; public int x0, y0, x1, y1; }
        [Serializable] private class Spawn { public string name; public string kind; public int x, floor; }
        [Serializable] private class Layout
        {
            public int ppu, width, height, doorDay11;
            public Solid[] solids;
            public int[] spawn;
            public int[] trapDay10;
            public Spawn[] enemies;
        }

        private static Layout layout;
        // Art pixel -> world. The art's left edge sits on Day 8's left bound; its vertical centre on y = 0.
        private const float LeftEdge = -6.6666665f;
        private static float X(float px) => LeftEdge + px / layout.ppu;
        private static float Y(float py) => (layout.height * .5f - py) / layout.ppu;

        public const string BossPath = "Assets/SaintKnightDay09/Scenes/Day09_BossChapel.unity";
        private const int BossWidth = 640, BossSpawnPx = 72;

        [MenuItem("Tools/SaintKnight/Day 09/Create Stage 1 and Boss Chapel Scenes")]
        public static void CreateScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh();
            layout = JsonUtility.FromJson<Layout>(File.ReadAllText(Art + "stage1_layout.json"));
            Directory.CreateDirectory("Assets/SaintKnightDay09/Scenes");
            BuildStage();
            BuildBossChapel();
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath && s.path != BossPath).ToList();
            int day08 = scenes.FindIndex(s => s.path == Day08Path);
            scenes.InsertRange(day08 >= 0 ? day08 : scenes.Count, new[]
                { new EditorBuildSettingsScene(ScenePath, true), new EditorBuildSettingsScene(BossPath, true) });
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("DAY09_STAGE1_READY: " + ScenePath + " + " + BossPath);
        }

        // Opens Day 8 (never saved) and swaps in one background strip, its end walls and camera bounds.
        private static UnityEngine.SceneManagement.Scene OpenFromDay8(string name, string art, float right)
        {
            var scene = EditorSceneManager.OpenScene(Day08Path);
            Object.DestroyImmediate(GameObject.Find("Pixel_Background_Right_Mirrored"));
            var bg = GameObject.Find("Pixel_Background_Day01");
            bg.name = name;
            bg.transform.position = new Vector3((LeftEdge + right) * .5f, 0f, 1f);
            bg.transform.localScale = Vector3.one;
            var bgRenderer = bg.GetComponent<SpriteRenderer>();
            bgRenderer.sprite = LoadSprite(art, new Vector2(.5f, .5f));
            bgRenderer.flipX = false;
            PlaceWall("Training_Left_Boundary", LeftEdge, -1f);
            PlaceWall("Training_Right_Boundary", right, 1f);
            var cameraData = new SerializedObject(Object.FindFirstObjectByType<SKHorizontalCamera>());
            cameraData.FindProperty("horizontalBounds").vector2Value = new Vector2(LeftEdge, right);
            cameraData.ApplyModifiedPropertiesWithoutUndo();
            return scene;
        }

        private static void BuildStage()
        {
            float right = LeftEdge + (float)layout.width / layout.ppu;
            var scene = OpenFromDay8("Stage1_Background", "stage1_bg.png", right);
            BuildTerrain();

            var player = Object.FindFirstObjectByType<SKDay01Player>();
            StandOn(player.transform, player.GetComponent<BoxCollider2D>(), layout.spawn[0], layout.spawn[1]);
            BuildDoor(player.GetComponent<SKHealth>(), layout.doorDay11, "Day09_BossChapel");
            BuildTraps(player);

            SetSessionEnemies(BuildEnemies());
            var texts = Object.FindFirstObjectByType<Canvas>().GetComponentsInChildren<Text>(true);
            texts.First(t => t.name == "HUD_TITLE").text = "RUINED CATHEDRAL - DAY 09";
            var status = texts.First(t => t.name == "Enemy_Training_Status");
            status.fontSize = Mathf.Min(status.fontSize, 13); // five enemy rows + footer
            SaintKnight.Day20.EditorTools.SKCheckpointPlacer.Place();   // Day 20
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save " + ScenePath);
        }

        // One fixed screen behind the stage door: flat arena, floor y310, entrance door painted at x~40,
        // the Rose Window Madonna on the altar (x415). R / auto-retry resets player, boss and summon.
        private static void BuildBossChapel()
        {
            float right = LeftEdge + (float)BossWidth / layout.ppu;
            var scene = OpenFromDay8("Boss_Chapel_Background", "boss_chapel.png", right);
            var ground = GameObject.Find("Ground_Collider_2D");
            var renderer = ground.GetComponent<SpriteRenderer>();
            if (renderer != null) Object.DestroyImmediate(renderer);
            ground.name = "Terrain_Boss_Floor";
            ground.transform.position = new Vector3((LeftEdge + right) * .5f, Y((310f + layout.height + layout.ppu) * .5f), 0f);
            ground.transform.localScale = Vector3.one;
            var box = ground.GetComponent<BoxCollider2D>();
            box.offset = Vector2.zero;
            box.size = new Vector2((float)BossWidth / layout.ppu, (layout.height + layout.ppu - 310f) / layout.ppu);
            Object.DestroyImmediate(GameObject.Find("Enemy_B_Heavy_TestTarget"));
            var monk = GameObject.Find("Enemy_A_Patrol_TestTarget");
            monk.name = "Boss_Summon_Monk";
            monk.transform.localScale = Vector3.one;
            StandOn(monk.transform, monk.GetComponent<BoxCollider2D>(), 60f, 310f);   // comes in through the door
            var summon = ConfigureEnemy(monk, false, "SUMMONED MONK");
            monk.SetActive(false);
            SetSessionEnemies(new SKEnemyAgent[0]); // the boss resets its own summon (never-woken agents have no body)
            var player = Object.FindFirstObjectByType<SKDay01Player>();
            StandOn(player.transform, player.GetComponent<BoxCollider2D>(), BossSpawnPx, 310f);
            BuildBoss(player.GetComponent<SKHealth>(), summon, right);
            var texts = Object.FindFirstObjectByType<Canvas>().GetComponentsInChildren<Text>(true);
            texts.First(t => t.name == "HUD_TITLE").text = "BOSS CHAPEL - ROSE WINDOW MADONNA";
            if (!EditorSceneManager.SaveScene(scene, BossPath)) throw new IOException("Could not save " + BossPath);
        }

        private const float BossAltarPx = 415f, BossCentre = .575f;

        // Docs/Design/Boss/BOSS_ROSE_MADONNA.md; art = Docs/Design/Boss/make_boss_sprites.py (v2, file names keep _NOT_FINAL).
        private static void BuildBoss(SKHealth player, SKEnemyAgent summon, float right)
        {
            float floor = Y(310f);
            var square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SaintKnightDay02/Art/hitbox_square.png");
            Sprite B(string name, Vector2 pivot) => LoadSprite("Boss/" + name + "_NOT_FINAL.png", pivot);
            var feet = new Vector2(.5f, BossCentre * layout.ppu / 300f);   // body cell 256x300, feet on the bottom row

            var root = new GameObject("Boss_Rose_Madonna");
            root.transform.position = new Vector3(X(BossAltarPx), floor + BossCentre, 0f);
            var bodyRenderer = root.AddComponent<SpriteRenderer>();
            bodyRenderer.sortingOrder = 4;
            var health = root.AddComponent<SKHealth>();
            SetHealth(health, 360f, .12f, bodyRenderer);   // first boss: 600 -> 360 (2026-11-10)

            SpriteRenderer Child(string name, Transform parent, Vector3 local, int order)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = local;
                var r = go.AddComponent<SpriteRenderer>();
                r.sortingOrder = order;
                return r;
            }
            SKHitbox Hitbox(string name, Transform parent)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                var hit = go.AddComponent<SKHitbox>();
                var r = go.GetComponent<SpriteRenderer>();
                r.sprite = square;
                r.sortingOrder = 12;
                var d = new SerializedObject(hit);
                d.FindProperty("owner").objectReferenceValue = health;
                d.ApplyModifiedPropertiesWithoutUndo();
                return hit;
            }

            // Heart: drawn shell + hurtbox 14..58 px above the floor (both heroines' strikes reach it).
            var core = Child("Core_Heart", root.transform, new Vector3(0f, 36f / layout.ppu - BossCentre, 0f), 5);
            var coreHurt = new GameObject("Core_Hurtbox");
            coreHurt.transform.SetParent(core.transform, false);
            coreHurt.AddComponent<BoxCollider2D>().size = new Vector2(.83f, .92f);
            var hurt = new SerializedObject(coreHurt.AddComponent<SKHurtbox>());
            hurt.FindProperty("owner").objectReferenceValue = health;
            hurt.ApplyModifiedPropertiesWithoutUndo();

            var sweep = Hitbox("Sweep_Hitbox", root.transform);
            var spikes = new SKFloorSpike[3];
            for (int i = 0; i < spikes.Length; i++) spikes[i] = FloorSpike("Floor_Tentacle_" + i, root.transform, health, 0f, 0f);
            // Cut target: a separate health the tentacle hurtbox reports to (never runs out).
            var tentacleGo = new GameObject("Tentacle_Target");
            tentacleGo.transform.SetParent(root.transform, false);
            var tentacle = tentacleGo.AddComponent<SKHealth>();
            SetHealth(tentacle, 9999f, 0f, null);
            var tentacleHurt = new GameObject("Tentacle_Hurtbox");
            tentacleHurt.transform.SetParent(tentacleGo.transform, false);
            var tentacleArea = tentacleHurt.AddComponent<BoxCollider2D>();
            tentacleArea.enabled = false;
            var th = new SerializedObject(tentacleHurt.AddComponent<SKHurtbox>());
            th.FindProperty("owner").objectReferenceValue = tentacle;
            th.ApplyModifiedPropertiesWithoutUndo();

            var warning = Child("Boss_Warning", root.transform, Vector3.zero, 11);
            warning.sprite = square;
            warning.drawMode = SpriteDrawMode.Sliced;
            warning.enabled = false;
            var sweepVisual = Child("Sweep_Tentacle", root.transform, Vector3.zero, 13);
            var grabVisual = Child("Grab_Tentacle", root.transform, Vector3.zero, 13);
            var coilVisual = Child("Grab_Coil", root.transform, Vector3.zero, 14);
            var slimeBall = Child("Slime_Ball", root.transform, Vector3.zero, 13);
            var puddle = SlimePool("Slime_Puddle", root.transform, player.GetComponent<SKDay01Player>(), .65f, false);
            foreach (var r in new[] { sweepVisual, grabVisual, coilVisual, slimeBall }) r.enabled = false;

            // Boss bar across the top of the fixed boss camera (world space; the room never scrolls).
            float centre = (LeftEdge + right) * .5f;
            var bar = new GameObject("Boss_HP_Bar").transform;
            bar.position = new Vector3(centre, 3.25f, 0f);
            var back = Child("Back", bar, Vector3.zero, 30);
            back.sprite = square; back.drawMode = SpriteDrawMode.Sliced; back.size = new Vector2(8.1f, .22f);
            back.color = new Color(.05f, .03f, .04f, .9f);
            var fillPivot = new GameObject("Fill_Pivot").transform;
            fillPivot.SetParent(bar, false);
            fillPivot.localPosition = new Vector3(-4f, 0f, 0f);
            var fill = Child("Fill", fillPivot, new Vector3(4f, 0f, 0f), 31);
            fill.sprite = square; fill.drawMode = SpriteDrawMode.Sliced; fill.size = new Vector2(8f, .14f);
            fill.color = new Color(.62f, .08f, .1f, 1f);
            var label = new GameObject("Boss_Name").AddComponent<TextMesh>();
            label.transform.SetParent(bar, false);
            label.transform.localPosition = new Vector3(0f, .32f, 0f);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 48; label.characterSize = .04f; label.anchor = TextAnchor.MiddleCenter;
            label.color = new Color(.9f, .84f, .74f);
            var labelRenderer = label.GetComponent<MeshRenderer>();
            labelRenderer.sharedMaterial = label.font.material;
            labelRenderer.sortingOrder = 32;

            var agent = root.AddComponent<SKBossAgent>();
            var a = new SerializedObject(agent);
            a.FindProperty("player").objectReferenceValue = player;
            a.FindProperty("sweepHitbox").objectReferenceValue = sweep;
            var holders = a.FindProperty("spikes");
            holders.arraySize = spikes.Length;
            for (int i = 0; i < spikes.Length; i++) holders.GetArrayElementAtIndex(i).objectReferenceValue = spikes[i];
            a.FindProperty("tentacle").objectReferenceValue = tentacle;
            a.FindProperty("tentacleArea").objectReferenceValue = tentacleArea;
            a.FindProperty("warning").objectReferenceValue = warning;
            a.FindProperty("summon").objectReferenceValue = summon;
            a.FindProperty("arenaX").vector2Value = new Vector2(LeftEdge + .2f, right - .2f);
            a.FindProperty("floorOffset").floatValue = BossCentre;
            Set(a, "bodyPhase1", new[] { B("boss_body_p1_f00", feet), B("boss_body_p1_f01", feet) });
            Set(a, "bodyPhase2", new[] { B("boss_body_p2_f00", feet), B("boss_body_p2_f01", feet) });
            Set(a, "bodyDead", new[] { B("boss_body_dead_f00", feet) });
            var mid = new Vector2(.5f, .5f);
            Set(a, "coreClosed", new[] { B("boss_core_closed_f00", mid) });
            Set(a, "coreCracked", new[] { B("boss_core_cracked_f00", mid) });
            Set(a, "coreOpen", new[] { B("boss_core_open_f00", mid), B("boss_core_open_f01", mid) });
            a.FindProperty("sweepTellSprite").objectReferenceValue = B("boss_sweep_tell_f00", Vector2.zero);
            a.FindProperty("sweepStrikeSprite").objectReferenceValue = B("boss_sweep_strike_f00", Vector2.zero);
            a.FindProperty("grabReach").objectReferenceValue = B("boss_grab_reach_f00", Vector2.zero);
            a.FindProperty("grabCoil").objectReferenceValue = B("boss_grab_coil_f00", new Vector2(.5f, 0f));
            a.FindProperty("core").objectReferenceValue = core;
            a.FindProperty("sweepVisual").objectReferenceValue = sweepVisual;
            a.FindProperty("grabVisual").objectReferenceValue = grabVisual;
            a.FindProperty("coilVisual").objectReferenceValue = coilVisual;
            a.FindProperty("slimeBall").objectReferenceValue = slimeBall;
            a.FindProperty("puddle").objectReferenceValue = puddle;
            a.FindProperty("hpFill").objectReferenceValue = fillPivot;
            a.FindProperty("hpLabel").objectReferenceValue = label;
            a.ApplyModifiedPropertiesWithoutUndo();
            slimeBall.sprite = B("boss_slime_ball_f00", mid);
            bodyRenderer.sprite = B("boss_body_p1_f00", feet);
        }

        // Day 10: the cloister trap strip (layout trapDay10, floor y310). A slime pool over the strip slows
        // walking; two floor-tentacle cracks inside it erupt in turn (2.4 s rhythm, 1.2 s apart), so the
        // slowed player has to read the swelling crack and wait or jump. Each crack owns a never-hurt enemy
        // SKHealth: enemies are immune and knockback pushes away from the crack.
        private const float TrapPeriod = 2.4f;

        private static void BuildTraps(SKDay01Player player)
        {
            int x0 = layout.trapDay10[0], x1 = layout.trapDay10[1];
            var root = new GameObject("Stage1_Traps_Day10").transform;
            var pool = SlimePool("Trap_Slime_Pool", root, player, (x1 - x0) * .5f / layout.ppu - 15f / layout.ppu, true);
            pool.transform.position = new Vector3(X((x0 + x1) * .5f), Y(310f), 0f);
            var view = pool.GetComponent<SpriteRenderer>();
            view.drawMode = SpriteDrawMode.Tiled;
            view.size = new Vector2(pool.HalfWidth * 2f, 12f / layout.ppu);
            float[] cracks = { x0 + 50f, x1 - 50f };
            for (int i = 0; i < cracks.Length; i++)
            {
                var go = new GameObject("Trap_Floor_Tentacle_" + i);
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3(X(cracks[i]), Y(310f), 0f);
                var owner = go.AddComponent<SKHealth>();
                SetHealth(owner, 9999f, 0f, null);
                FloorSpike(go.name, go.transform, owner, TrapPeriod, i * TrapPeriod * .5f, go);
            }
            BuildFleshWall(root, player.GetComponent<SKHealth>());
        }

        // Flesh wall on the plain wall between the skull pillar and the next pillar of the chapel forecourt,
        // before monk A3 (2150). Art: Docs/Design/Stage/make_flesh_wall.py (160x112 cells, bottom centre).
        private const float FleshWallPx = 1945f;

        private static void BuildFleshWall(Transform parent, SKHealth player)
        {
            var bottom = new Vector2(.5f, 0f);
            var go = new GameObject("Trap_Flesh_Wall");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(X(FleshWallPx), Y(310f), 0f);
            var body = go.AddComponent<SpriteRenderer>();
            body.sortingOrder = 5;   // behind the player (10): the mass grows on the back wall
            var warning = new GameObject("Flesh_Wall_Warning").AddComponent<SpriteRenderer>();
            warning.transform.SetParent(go.transform, false);
            warning.transform.localPosition = new Vector3(0f, .3f, 0f);
            warning.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SaintKnightDay02/Art/hitbox_square.png");
            warning.drawMode = SpriteDrawMode.Sliced;
            warning.color = new Color(.65f, .25f, 1f, .3f);   // purple = grab (same rule as the monk and boss)
            warning.sortingOrder = 11;
            warning.enabled = false;
            var coil = new GameObject("Flesh_Wall_Coil").AddComponent<SpriteRenderer>();
            coil.transform.SetParent(go.transform, false);
            coil.sprite = LoadSprite("Boss/boss_grab_coil_f00_NOT_FINAL.png", bottom);
            coil.sortingOrder = 14;
            coil.enabled = false;
            var wall = go.AddComponent<SKFleshWall>();
            var d = new SerializedObject(wall);
            d.FindProperty("player").objectReferenceValue = player;
            warning.size = new Vector2(d.FindProperty("reach").floatValue * 2f, .6f);
            var idle = TrapFrames("flesh_wall_idle", 3);
            Set(d, "idle", idle);
            d.FindProperty("reachSprite").objectReferenceValue = TrapSprite("flesh_wall_reach", 0);
            Set(d, "ceciliaGrab", FleshWallGrabFrames("cecilia"));
            Set(d, "adelaGrab", FleshWallGrabFrames("adela"));
            d.FindProperty("warning").objectReferenceValue = warning;
            d.FindProperty("coil").objectReferenceValue = coil;
            d.ApplyModifiedPropertiesWithoutUndo();
            body.sprite = idle[0];
        }

        private static Sprite[] FleshWallGrabFrames(string character)
        {
            string prefix = $"flesh_wall_{character}_";
            return TrapFrames(prefix + "swallow", 2)
                .Concat(TrapFrames(prefix + "struggle", 2))
                .Concat(TrapFrames(prefix + "escape", 1)).ToArray();
        }

        private static Sprite[] TrapFrames(string motion, int count)
        {
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++) frames[i] = TrapSprite(motion, i);
            return frames;
        }

        private static Sprite TrapSprite(string motion, int index)
        {
            string path = $"{TrapArt}{motion}_f{index:00}.png";
            return AssetDatabase.LoadAssetAtPath<Sprite>(path) ?? throw new FileNotFoundException("Missing sprite: " + path);
        }

        // Shared Day 10 parts (boss + stage).
        private static SKFloorSpike FloorSpike(string name, Transform parent, SKHealth owner, float period,
            float firstDelay, GameObject on = null)
        {
            var go = on != null ? on : new GameObject(name);
            if (on == null) go.transform.SetParent(parent, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sortingOrder = 13;
            r.enabled = false;
            var hitGo = new GameObject(name + "_Hitbox");
            hitGo.transform.SetParent(go.transform, false);
            var hit = hitGo.AddComponent<SKHitbox>();
            var hr = hitGo.GetComponent<SpriteRenderer>();
            hr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SaintKnightDay02/Art/hitbox_square.png");
            hr.sortingOrder = 12;
            var h = new SerializedObject(hit);
            h.FindProperty("owner").objectReferenceValue = owner;
            h.ApplyModifiedPropertiesWithoutUndo();
            var spike = go.AddComponent<SKFloorSpike>();
            var d = new SerializedObject(spike);
            d.FindProperty("hitbox").objectReferenceValue = hit;
            var tellFrames = TrapFrames("floor_tentacle_tell", 2);
            var riseFrames = TrapFrames("floor_tentacle_rise", 3);
            Set(d, "tellFrames", tellFrames);
            Set(d, "riseFrames", riseFrames);
            Set(d, "retractFrames", TrapFrames("floor_tentacle_retract", 2));
            d.FindProperty("bulge").objectReferenceValue = tellFrames[0];
            d.FindProperty("strike").objectReferenceValue = riseFrames[2];
            d.FindProperty("period").floatValue = period;
            d.FindProperty("firstDelay").floatValue = firstDelay;
            d.ApplyModifiedPropertiesWithoutUndo();
            return spike;
        }

        private static SKSlimePool SlimePool(string name, Transform parent, SKDay01Player player, float halfWidth, bool permanent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<SpriteRenderer>();
            var loopFrames = TrapFrames("slime_pool", 3);
            r.sprite = loopFrames[0];
            r.sortingOrder = 9;
            r.enabled = permanent;
            var pool = go.AddComponent<SKSlimePool>();
            var d = new SerializedObject(pool);
            Set(d, "loopFrames", loopFrames);
            d.FindProperty("player").objectReferenceValue = player;
            d.FindProperty("halfWidth").floatValue = halfWidth;
            d.FindProperty("permanent").boolValue = permanent;
            d.ApplyModifiedPropertiesWithoutUndo();
            return pool;
        }

        private static void SetHealth(SKHealth health, float hp, float immunity, SpriteRenderer renderer)
        {
            var d = new SerializedObject(health);
            d.FindProperty("team").enumValueIndex = (int)SKTeam.Enemy;
            d.FindProperty("maximumHp").floatValue = hp;
            d.FindProperty("invulnerabilitySeconds").floatValue = immunity;
            d.FindProperty("hitStunSeconds").floatValue = 0f; // a boss is never stun-locked
            d.FindProperty("bodyRenderer").objectReferenceValue = renderer;
            d.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSessionEnemies(SKEnemyAgent[] enemies)
        {
            var data = new SerializedObject(Object.FindFirstObjectByType<SKEnemyTrainingSession>());
            var list = data.FindProperty("enemies");
            list.arraySize = enemies.Length;
            for (int i = 0; i < enemies.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = enemies[i];
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildDoor(SKHealth player, float px, string target)
        {
            var door = new GameObject("Door_To_" + target).AddComponent<SKSceneDoor>();
            door.transform.position = new Vector3(X(px), Y(310f) + .47f, 0f); // player centre height; enters on reach (Day 11)
            var data = new SerializedObject(door);
            data.FindProperty("player").objectReferenceValue = player;
            data.FindProperty("targetScene").stringValue = target;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildTerrain()
        {
            var template = GameObject.Find("Ground_Collider_2D");
            var renderer = template.GetComponent<SpriteRenderer>();
            if (renderer != null) Object.DestroyImmediate(renderer); // terrain is painted in the background
            var parent = new GameObject("Stage1_Terrain").transform;
            for (int i = 0; i < layout.solids.Length; i++)
            {
                var s = layout.solids[i];
                var go = i == 0 ? template : Object.Instantiate(template);
                go.name = "Terrain_" + s.name;
                go.transform.SetParent(parent, true);
                // Floors run 1 unit below the screen so nothing can fall through the bottom edge.
                float bottom = s.y1 >= layout.height ? s.y1 + layout.ppu : s.y1;
                go.transform.position = new Vector3(X((s.x0 + s.x1) * .5f), Y((s.y0 + bottom) * .5f), 0f);
                go.transform.rotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                var box = go.GetComponent<BoxCollider2D>();
                box.offset = Vector2.zero;
                box.size = new Vector2((s.x1 - s.x0) / (float)layout.ppu, (bottom - s.y0) / layout.ppu);
            }
        }

        private static void PlaceWall(string name, float edge, float side)
        {
            var wall = GameObject.Find(name);
            var box = wall.GetComponent<BoxCollider2D>();
            float half = box.size.x * Mathf.Abs(wall.transform.lossyScale.x) * .5f;
            float offset = box.offset.x * wall.transform.lossyScale.x;
            wall.transform.position = new Vector3(edge + side * half - offset, wall.transform.position.y, 0f);
        }

        private static void StandOn(Transform actor, BoxCollider2D body, float px, float floorPx)
        {
            float bottom = (body.offset.y - body.size.y * .5f) * actor.lossyScale.y;
            actor.position = new Vector3(X(px), Y(floorPx) - bottom + .01f, actor.position.z);
        }

        private static SKEnemyAgent[] BuildEnemies()
        {
            var templateA = GameObject.Find("Enemy_A_Patrol_TestTarget");
            var templateB = GameObject.Find("Enemy_B_Heavy_TestTarget");
            var parent = new GameObject("Stage1_Enemies").transform;
            var agents = new SKEnemyAgent[layout.enemies.Length];
            for (int i = 0; i < layout.enemies.Length; i++)
            {
                var e = layout.enemies[i];
                bool heavy = e.kind == "B";
                var template = heavy ? templateB : templateA;
                // Each template is reused once; the rest are copies that keep its tuned values.
                bool firstOfKind = layout.enemies.Take(i).All(o => o.kind != e.kind);
                var go = firstOfKind ? template : Object.Instantiate(template);
                go.name = e.name;
                go.transform.SetParent(parent, true);
                go.transform.localScale = Vector3.one;
                var body = go.GetComponent<BoxCollider2D>();
                StandOn(go.transform, body, e.x, e.floor);

                agents[i] = ConfigureEnemy(go, heavy, e.name.Replace("Enemy_", "").Replace('_', ' ').ToUpperInvariant());
            }
            return agents;
        }

        private static SKEnemyAgent ConfigureEnemy(GameObject go, bool heavy, string displayName)
        {
            var body = go.GetComponent<BoxCollider2D>();
            var agent = go.GetComponent<SKEnemyAgent>();
            var data = new SerializedObject(agent);
            data.FindProperty("displayName").stringValue = displayName;
            data.ApplyModifiedPropertiesWithoutUndo();

            // Docs/Design/Enemies/Day09 frames (split per file), feet on the collider's bottom.
            float bottomPx = -(body.offset.y - body.size.y * .5f) * layout.ppu;
            // Copies of the first enemy already carry the view (Unity's fake null defeats ??).
            if (!go.TryGetComponent<SKEnemySpriteView>(out var view)) view = go.AddComponent<SKEnemySpriteView>();
            var v = new SerializedObject(view);
            v.FindProperty("player").objectReferenceValue = data.FindProperty("player").objectReferenceValue;
            v.FindProperty("windupSeconds").floatValue = data.FindProperty("windupSeconds").floatValue;
            v.FindProperty("strikeSeconds").floatValue = data.FindProperty("attackSeconds").floatValue;
            Sprite[] idle;
            if (heavy)
            {
                Sprite[] F(string motion) => Frames("executioner", motion, 192, 112, 109, bottomPx);
                Set(v, "walk", F("walk")); Set(v, "idle", idle = F("idle"));
                Set(v, "windup", F("windup")); Set(v, "strike", F("strike")); Set(v, "recovery", F("recovery"));
                Set(v, "hit", F("hit")); Set(v, "armorBreak", F("armor-break")); Set(v, "defeat", F("defeat"));
                v.FindProperty("strideUnits").floatValue = .24f;
            }
            else
            {
                Sprite[] F(string motion) => Frames("monk", motion, 128, 96, 93, bottomPx);
                Set(v, "walk", F("walk")); Set(v, "idle", idle = F("idle"));
                Set(v, "windup", F("claw-windup")); Set(v, "strike", F("claw-strike")); Set(v, "recovery", F("claw-recovery"));
                Set(v, "grabWindup", F("grab-windup")); Set(v, "grabMiss", F("grab-miss"));
                Set(v, "hit", F("hit")); Set(v, "defeat", F("defeat"));
                v.FindProperty("strideUnits").floatValue = .18f;
                var grab = v.FindProperty("grab");
                grab.arraySize = 2; // SelectedCharacter: 0 Cecilia, 1 Adela
                var sets = new[] { "grab-cecilia", "grab-adela" };
                for (int c = 0; c < 2; c++)
                    foreach (var (field, motion) in new[] { ("pounce", "pounce"), ("hold", "hold"),
                                 ("resist", "resist"), ("escape", "escape"), ("thrown", "throw") })
                        Set(grab.GetArrayElementAtIndex(c).FindPropertyRelative(field),
                            Frames(sets[c], motion, 160, 96, 93, bottomPx));
            }
            v.ApplyModifiedPropertiesWithoutUndo();
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = idle[0];
            renderer.color = Color.white;
            renderer.flipX = false;
            return agent;
        }

        private static void Set(SerializedObject owner, string name, Sprite[] frames) =>
            Set(owner.FindProperty(name), frames);

        private static void Set(SerializedProperty list, Sprite[] frames)
        {
            list.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        }

        // Every cell keeps the body centre at x 64 and the feet just above groundY (frames.json).
        private static Sprite[] Frames(string set, string motion, int width, int height, int groundY, float bottomPx)
        {
            var pivot = new Vector2(64f / width, (height - groundY + bottomPx) / height);
            var frames = new System.Collections.Generic.List<Sprite>();
            for (int i = 0; File.Exists($"{Art}Enemies/{set}/{motion}_f{i:00}.png"); i++)
                frames.Add(LoadSprite($"Enemies/{set}/{motion}_f{i:00}.png", pivot));
            if (frames.Count == 0) throw new FileNotFoundException($"No frames for {set}/{motion}");
            return frames.ToArray();
        }

        private static Sprite LoadSprite(string file, Vector2 pivot)
        {
            var path = file.StartsWith("Assets/") ? file : Art + file;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("Missing texture: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = layout.ppu;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 4096; // the 2560 px stage strip must not be downscaled
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
