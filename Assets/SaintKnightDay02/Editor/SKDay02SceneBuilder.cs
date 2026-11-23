using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SaintKnight.Day01;

namespace SaintKnight.Day02.Editor
{
    public static class SKDay02SceneBuilder
    {
        public const string ScenePath = "Assets/SaintKnightDay02/Scenes/Day02_CombatTraining.unity";
        private const string ArtPath = "Assets/SaintKnightDay02/Art";

        [MenuItem("Tools/SaintKnight/Day 02/Create Combat Training Scene")]
        public static void CreateScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory("Assets/SaintKnightDay02/Scenes");
            Directory.CreateDirectory(ArtPath);
            var square = MakeSprite("hitbox_square", 8, 8, (x, y) => Color.white, 8);
            var dummy = MakeSprite("training_dummy", 32, 48, (x, y) =>
            {
                bool head = x >= 10 && x <= 21 && y >= 34 && y <= 45;
                bool chest = x >= 7 && x <= 24 && y >= 16 && y <= 33;
                bool arms = y >= 26 && y <= 30 && x >= 2 && x <= 29;
                bool post = x >= 14 && x <= 17 && y >= 2 && y < 16;
                bool foot = y >= 1 && y <= 4 && x >= 5 && x <= 26;
                if (!(head || chest || arms || post || foot)) return Color.clear;
                if (chest && (x == 15 || x == 16 || y == 23 || y == 24)) return new Color(.75f, .26f, .22f);
                return (x % 5 == 0) ? new Color(.3f, .2f, .18f) : new Color(.6f, .43f, .29f);
            }, 40);

            var scene = EditorSceneManager.OpenScene("Assets/SaintKnightDay01/Scenes/Day01_Prototype.unity");
            var player = Object.FindFirstObjectByType<SKDay01Player>();
            if (player == null) throw new System.InvalidOperationException("Day 01 player is missing.");
            var health = player.gameObject.AddComponent<SKHealth>();
            AddHurtbox(player.transform, new Vector2(.47f, .94f), health);
            var attack = AddHitbox(player.transform, square, health);
            var combat = player.gameObject.AddComponent<SKPlayerCombat>();
            SetReferences(combat, "hitbox", attack);

            var target = new GameObject("Day02_Training_Target");
            target.transform.position = new Vector3(1f, -2.15f, 0f);
            var renderer = target.AddComponent<SpriteRenderer>();
            renderer.sprite = dummy;
            renderer.sortingOrder = 10;
            var targetHealth = target.AddComponent<SKHealth>();
            var serializedHealth = new SerializedObject(targetHealth);
            serializedHealth.FindProperty("team").enumValueIndex = (int)SKTeam.Enemy;
            serializedHealth.FindProperty("maximumHp").floatValue = 150f;
            serializedHealth.FindProperty("invulnerabilitySeconds").floatValue = .12f;
            serializedHealth.ApplyModifiedPropertiesWithoutUndo();
            AddHurtbox(target.transform, new Vector2(.7f, 1.15f), targetHealth);
            var targetAttack = AddHitbox(target.transform, square, targetHealth);
            var warning = new GameObject("Attack_Warning", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            warning.transform.SetParent(target.transform, false);
            warning.sprite = square;
            warning.drawMode = SpriteDrawMode.Sliced;
            warning.size = new Vector2(1.1f, .85f);
            warning.color = new Color(1f, .55f, .1f, .2f);
            warning.sortingOrder = 11;
            warning.enabled = false;
            var behavior = target.AddComponent<SKTrainingTarget>();
            SetReferences(behavior, "player", health, "hitbox", targetAttack, "warning", warning);

            var canvas = Object.FindFirstObjectByType<Canvas>();
            // uGUI ignores fillAmount when Image has no source sprite.
            canvas.GetComponentsInChildren<Image>().First(i => i.name == "HP_FILL").sprite = square;
            var texts = canvas.GetComponentsInChildren<Text>();
            texts.First(t => t.name == "HUD_TITLE").text = "CHARACTER STATUS - DAY 02";
            var controls = texts.First(t => t.name == "CONTROLS");
            controls.fontSize = 17;
            controls.text = "MOVE A/D   |   JUMP SPACE   |   SWITCH TAB   |   ATTACK J\n" +
                "RESET R   |   DEBUG HP - Q / + E";
            var statusGo = new GameObject("Day02_Target_Status", typeof(RectTransform), typeof(Text));
            statusGo.transform.SetParent(canvas.transform, false);
            var rect = statusGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(.25f, .875f);
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var status = statusGo.GetComponent<Text>();
            status.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            status.fontSize = 21;
            status.alignment = TextAnchor.MiddleCenter;
            status.color = new Color(.85f, .7f, .45f);
            status.text = "DAY 02  |  TRAINING TARGET  150 / 150\nREADY";
            status.raycastTarget = false;
            var session = new GameObject("Day02_Training_Session").AddComponent<SKTrainingSession>();
            SetReferences(session, "player", health, "target", targetHealth, "status", status);

            // Keep this compact training room on screen; a scrolling level remains Day 08+.
            AddWall("Training_Left_Boundary", -6.5f);
            AddWall("Training_Right_Boundary", 6.5f);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Could not save Day 02 scene.");
            // Add the new playable scene first without losing existing build entries.
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
                .Concat(EditorBuildSettings.scenes.Where(s => s.path != ScenePath)).ToArray();
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player.gameObject;
            Debug.Log("DAY02_SCENE_READY: " + ScenePath);
        }

        private static void AddWall(string name, float x)
        {
            var wall = new GameObject(name);
            wall.transform.position = new Vector3(x, 0, 0);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(.3f, 12f);
        }

        private static void AddHurtbox(Transform parent, Vector2 size, SKHealth owner)
        {
            var go = new GameObject("Hurtbox", typeof(BoxCollider2D), typeof(SKHurtbox));
            go.transform.SetParent(parent, false);
            var box = go.GetComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = size;
            SetReferences(go.GetComponent<SKHurtbox>(), "owner", owner);
        }

        private static SKHitbox AddHitbox(Transform parent, Sprite square, SKHealth owner)
        {
            var go = new GameObject("Attack_Hitbox", typeof(BoxCollider2D), typeof(SpriteRenderer), typeof(SKHitbox));
            go.transform.SetParent(parent, false);
            var collider = go.GetComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.enabled = false;
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.sortingOrder = 12;
            renderer.enabled = false;
            var hitbox = go.GetComponent<SKHitbox>();
            SetReferences(hitbox, "owner", owner);
            return hitbox;
        }

        private static Sprite MakeSprite(string name, int width, int height,
            System.Func<int, int, Color> pixel, float ppu)
        {
            string path = ArtPath + "/" + name + ".png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++) texture.SetPixel(x, y, pixel(x, y));
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void SetReferences(Object component, params object[] pairs)
        {
            var serialized = new SerializedObject(component);
            for (int i = 0; i < pairs.Length; i += 2)
                serialized.FindProperty((string)pairs[i]).objectReferenceValue = (Object)pairs[i + 1];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
