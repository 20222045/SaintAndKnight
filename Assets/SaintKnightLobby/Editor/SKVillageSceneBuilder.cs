using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SaintKnight.Lobby.Editor
{
    /// <summary>
    /// 2026-11-19: the village reached after the first boss clear. Opens the lobby scene, swaps in the village street
    /// and its spots, and saves it as a new scene (the lobby file itself is not saved).
    /// Art: Docs/Design/Village/make_village_bg.py (NovelAI street, pixelized), 48 PPU like the lobby.
    /// 2026-11-22 (user): the street was extended to both sides (NovelAI inpaint) to 1260x360; the old 640 px art
    /// sits unchanged at x = 310, so every old door keeps its px + 310, and the camera follows the player.
    /// </summary>
    public static class SKVillageSceneBuilder
    {
        public const string ScenePath = "Assets/SaintKnightLobby/Scenes/Village.unity";

        // doors measured on the old 640 px art (+ Mid in the 1260 px street): bathhouse, rabbit-sign shop (bunny gambling hall), blue door (beastkin bar), stone arch
        // hintPy: px height where the "[W] 들어가기" bubble's tail sits, just over the door (make_village_signs.py)
        private static readonly (string label, SKLobbyAction action, float px, float hintPy)[] Spots =
        {
            ("HOT SPRING - REST", SKLobbyAction.HotSpring, Mid + 598f, 236f),
            ("SILVER HARE - GAMBLING", SKLobbyAction.Tavern, Mid + 238f, 262f),
            ("TAIL LANTERN - BEASTKIN BAR", SKLobbyAction.BeastBar, Mid + 527f, 280f),   // the blue door (2026-11-22)
            ("RED ANVIL - FORGE", SKLobbyAction.Forge, Mid + 305f, 270f),                 // market stall fronts (2026-11-22)
            ("MOONLIGHT APOTHECARY", SKLobbyAction.Apothecary, Mid + 455f, 270f),
            ("GATE - SANCTUARY", SKLobbyAction.Sanctuary, Mid + 49f, 250f),
        };
        private const float Mid = 310f, ArtWidth = 1260f, Half = ArtWidth / 2f;   // old 640 art's left edge, street width
        private const float WalkLine = 326f, LeftWall = 14f, RightWall = ArtWidth - 14f;   // px in the street art
        private const string Art = "Assets/SaintKnightLobby/Art/";
        // chimney tops on village_bg.png (px): the old six (+ Mid) and the new sides
        private static readonly Vector2[] Chimneys =
            { new(Mid + 177, 133), new(Mid + 200, 170), new(Mid + 262, 161), new(Mid + 358, 160), new(Mid + 487, 148), new(Mid + 591, 156),
              new(248, 86), new(148, 92), new(993, 132), new(1045, 135) };

        private static Sprite Import(string file, bool sprite, Vector2? pivot = null)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(Art + file);
            importer.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            if (sprite) { importer.spriteImportMode = SpriteImportMode.Single; importer.spritePixelsPerUnit = 48f; }
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            var settings = new TextureImporterSettings();   // FullRect: a Tight mesh around many small lit windows rendered nothing (lobby builder)
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            if (pivot.HasValue) { settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = pivot.Value; }
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return sprite ? AssetDatabase.LoadAssetAtPath<Sprite>(Art + file) : null;
        }

        [MenuItem("Tools/SaintKnight/Lobby/Create Village Scene")]
        public static void CreateScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var street = Import("village_bg.png", true);
            var windowLights = Import("village_lights.png", true);
            Import("chimney_smoke.png", false);
            var smokeSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "chimney_smoke.png");
            var hint = Import("Signs/hint_enter.png", true, new Vector2(.5f, 0f));

            var scene = EditorSceneManager.OpenScene(SKLobbySceneBuilder.ScenePath);
            var bg = GameObject.Find("Lobby_Background");
            bg.name = "Village_Background";
            bg.GetComponent<SpriteRenderer>().sprite = street;
            Object.DestroyImmediate(GameObject.Find("Lobby_FrontProps"));
            Object.DestroyImmediate(GameObject.Find("Lobby_Foreground"));

            // time of day: sky behind the town (cut out), window lights over it, chimney smoke (SKVillageSky)
            var lightsRenderer = new GameObject("Village_WindowLights").AddComponent<SpriteRenderer>();
            lightsRenderer.sprite = windowLights;
            lightsRenderer.sortingOrder = -9;
            lightsRenderer.transform.position = new Vector3(0f, 0f, .9f);
            var skyData = new SerializedObject(new GameObject("Village_Sky_Clock").AddComponent<SKVillageSky>());
            skyData.FindProperty("town").objectReferenceValue = bg.GetComponent<SpriteRenderer>();
            skyData.FindProperty("lights").objectReferenceValue = lightsRenderer;
            skyData.FindProperty("smokeSheet").objectReferenceValue = smokeSheet;
            skyData.FindProperty("artWidth").floatValue = ArtWidth;
            var tops = skyData.FindProperty("chimneys");
            tops.arraySize = Chimneys.Length;
            for (int i = 0; i < Chimneys.Length; i++) tops.GetArrayElementAtIndex(i).vector2Value = Chimneys[i];
            skyData.ApplyModifiedPropertiesWithoutUndo();

            var ground = GameObject.Find("Ground_Collider_2D");
            ground.transform.position = new Vector3(0f, (180f - WalkLine) / 48f - .5f, 0f);
            ground.GetComponent<BoxCollider2D>().size = new Vector2(ArtWidth / 48f + 1f, 1f);
            GameObject.Find("Lobby_Left_Boundary").transform.position = new Vector3((LeftWall - Half) / 48f, 0f, 0f);
            GameObject.Find("Lobby_Right_Boundary").transform.position = new Vector3((RightWall - Half) / 48f, 0f, 0f);
            var player = Object.FindFirstObjectByType<SaintKnight.Day01.SKDay01Player>();
            player.transform.position = new Vector3((Mid + 320f - Half) / 48f, (180f - WalkLine) / 48f + .6f, 0f);   // mid-street, clear of the illustration
            // 2026-11-22: the street is two screens wide; the camera follows her and stops at both ends
            var follow = Object.FindFirstObjectByType<Camera>().gameObject;
            if (!follow.TryGetComponent<SaintKnight.Day08.SKHorizontalCamera>(out var scroll)) scroll = follow.AddComponent<SaintKnight.Day08.SKHorizontalCamera>();
            var camData = new SerializedObject(scroll);
            camData.FindProperty("target").objectReferenceValue = player.transform;
            camData.FindProperty("horizontalBounds").vector2Value = new Vector2(-Half / 48f, Half / 48f);
            camData.ApplyModifiedPropertiesWithoutUndo();

            var texts = Object.FindFirstObjectByType<Canvas>().GetComponentsInChildren<Text>(true);
            var title = texts.FirstOrDefault(t => t.name == "HUD_TITLE");
            if (title != null) title.text = "VILLAGE";
            // user: no English prompt at the top in the village, the "[W] 들어가기" bubble over the door says it
            // (the Text keeps updating underneath; only not drawn)
            var prompt = texts.FirstOrDefault(t => t.name == "Lobby_Prompt");
            if (prompt != null) prompt.enabled = false;

            var data = new SerializedObject(bg.GetComponent<SKLobby>());
            data.FindProperty("ceciliaBackground").objectReferenceValue = street;
            data.FindProperty("adelaBackground").objectReferenceValue = street;
            data.FindProperty("frontProps").objectReferenceValue = null;
            data.FindProperty("idleText").stringValue = "VILLAGE";
            var list = data.FindProperty("spots");
            list.arraySize = Spots.Length;
            for (int i = 0; i < Spots.Length; i++)
            {
                var e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("label").stringValue = Spots[i].label;
                e.FindPropertyRelative("action").enumValueIndex = (int)Spots[i].action;
                e.FindPropertyRelative("x").floatValue = (Spots[i].px - Half) / 48f;
                e.FindPropertyRelative("hintArt").objectReferenceValue = hint;
                e.FindPropertyRelative("hintY").floatValue = (180f - Spots[i].hintPy) / 48f;
            }
            data.ApplyModifiedPropertiesWithoutUndo();

            if (!EditorSceneManager.SaveScene(scene, ScenePath, true)) throw new IOException("Could not save " + ScenePath);
            if (EditorBuildSettings.scenes.All(s => s.path != ScenePath))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Append(new EditorBuildSettingsScene(ScenePath, true)).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("VILLAGE_SCENE_READY: " + ScenePath);
        }
    }
}
