using System.IO;
using System.Linq;
using SaintKnight.Day01;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SaintKnight.Lobby.Editor
{
    /// <summary>
    /// Builds the sanctuary lobby from the Day 5 scene's player/HUD (Day 5 file itself is not saved).
    /// Art: Docs/Design/Lobby/make_lobby_v6.py exports, 640x360 at 48 PPU = the gameplay camera view.
    /// </summary>
    public static class SKLobbySceneBuilder
    {
        public const string ScenePath = "Assets/SaintKnightLobby/Scenes/Lobby_Sanctuary.unity";
        private const string Day05Path = "Assets/SaintKnightDay04/Scenes/Day05_EnemyPatterns.unity";
        private const string Art = "Assets/SaintKnightLobby/Art/";
        private const float Ppu = 48f;

        // Spot x = (pixel x in the 640 px art - 320) / 48.
        private static readonly (string label, SKLobbyAction action, float px)[] Spots =
        {
            // 2026-11-19: hot spring and salon moved to the village (SKVillageSceneBuilder)
            ("SWORD - SWITCH CHARACTER", SKLobbyAction.SwitchCharacter, 216f),
            ("LECTERN - RECORDS", SKLobbyAction.Records, 304f),
            ("SIDE GATE - VILLAGE", SKLobbyAction.Village, 358f),
            ("ALTAR - PRAY", SKLobbyAction.Pray, 414f),
            ("MAP TABLE - SORTIE", SKLobbyAction.Sortie, 509f),
            ("DOOR - TRAINING GROUNDS", SKLobbyAction.Training, 575f),
        };

        [MenuItem("Tools/SaintKnight/Lobby/Create Lobby Scene")]
        public static void CreateScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var cecilia = LoadSprite("lobby_bg_cecilia.png");
            var adela = LoadSprite("lobby_bg_adela.png");
            var front = LoadSprite("lobby_foreground.png");
            var ceciliaFront = LoadSprite("lobby_front_cecilia.png");
            var adelaFront = LoadSprite("lobby_front_adela.png");
            Directory.CreateDirectory("Assets/SaintKnightLobby/Scenes");

            var scene = EditorSceneManager.OpenScene(Day05Path);
            foreach (var name in new[] { "Enemy_A_Patrol_TestTarget", "Enemy_B_Heavy_TestTarget", "Enemy_Training_Session" })
                Object.DestroyImmediate(GameObject.Find(name));

            var player = Object.FindFirstObjectByType<SKDay01Player>();
            player.transform.position = new Vector3(.5f, -2.2f, 0f);

            var bg = GameObject.Find("Pixel_Background_Day01");
            bg.name = "Lobby_Background";
            bg.transform.position = new Vector3(0f, 0f, 1f); // depth backs up sortingOrder
            var bgRenderer = bg.GetComponent<SpriteRenderer>();
            bgRenderer.sprite = cecilia;
            bgRenderer.sortingOrder = -10;

            // Sword, map table and bell stand in front of the walk line: the player passes behind them.
            var props = new GameObject("Lobby_FrontProps");
            var propsRenderer = props.AddComponent<SpriteRenderer>();
            propsRenderer.sprite = ceciliaFront;
            propsRenderer.sortingOrder = 15;
            props.transform.position = new Vector3(0f, 0f, -.5f);

            var fg = new GameObject("Lobby_Foreground");
            var fgRenderer = fg.AddComponent<SpriteRenderer>();
            fgRenderer.sprite = front;
            fgRenderer.sortingOrder = 20; // in front of the player (10)
            fg.transform.position = new Vector3(0f, 0f, -1f);

            // Walk line = 310 px in the art.
            var ground = GameObject.Find("Ground_Collider_2D");
            ground.transform.position = new Vector3(1f, (180f - 310f) / Ppu - .5f, 0f);
            ground.GetComponent<BoxCollider2D>().size = new Vector2(14f, 1f);
            var groundRenderer = ground.GetComponent<SpriteRenderer>();
            if (groundRenderer != null) groundRenderer.enabled = false;
            var left = GameObject.Find("Training_Left_Boundary");
            left.name = "Lobby_Left_Boundary";
            left.transform.position = new Vector3((130f - 320f) / Ppu, 0f, 0f);
            var right = GameObject.Find("Training_Right_Boundary");
            right.name = "Lobby_Right_Boundary";
            right.transform.position = new Vector3((610f - 320f) / Ppu, 0f, 0f);

            var texts = Object.FindFirstObjectByType<Canvas>().GetComponentsInChildren<Text>();
            texts.First(t => t.name == "HUD_TITLE").text = "SANCTUARY - LOBBY";
            texts.First(t => t.name == "NOTE").text = "LOBBY ART V6 - CHARACTER SPRITES PENDING";
            texts.First(t => t.name == "CONTROLS").text =
                "MOVE A/D   |   JUMP SPACE   |   SWITCH TAB   |   INTERACT W";
            var prompt = texts.First(t => t.name == "Enemy_Training_Status");
            prompt.name = "Lobby_Prompt";
            prompt.text = "SANCTUARY";

            var lobby = bg.AddComponent<SKLobby>();
            var data = new SerializedObject(lobby);
            data.FindProperty("player").objectReferenceValue = player;
            data.FindProperty("background").objectReferenceValue = bgRenderer;
            data.FindProperty("ceciliaBackground").objectReferenceValue = cecilia;
            data.FindProperty("adelaBackground").objectReferenceValue = adela;
            data.FindProperty("frontProps").objectReferenceValue = propsRenderer;
            data.FindProperty("ceciliaFront").objectReferenceValue = ceciliaFront;
            data.FindProperty("adelaFront").objectReferenceValue = adelaFront;
            data.FindProperty("prompt").objectReferenceValue = prompt;
            data.FindProperty("trainingScene").stringValue = "Day09_Stage1";
            var list = data.FindProperty("spots");
            list.arraySize = Spots.Length;
            for (int i = 0; i < Spots.Length; i++)
            {
                var e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("label").stringValue = Spots[i].label;
                e.FindPropertyRelative("action").enumValueIndex = (int)Spots[i].action;
                e.FindPropertyRelative("x").floatValue = (Spots[i].px - 320f) / Ppu;
            }
            data.ApplyModifiedPropertiesWithoutUndo();

            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save " + ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
                .Concat(EditorBuildSettings.scenes.Where(s => s.path != ScenePath)).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("LOBBY_SCENE_READY: " + ScenePath);
        }

        private static Sprite LoadSprite(string file)
        {
            var path = Art + file;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("Missing texture: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Ppu;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            // Full-screen layers: a Tight mesh traced around many small foreground shapes rendered nothing.
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
