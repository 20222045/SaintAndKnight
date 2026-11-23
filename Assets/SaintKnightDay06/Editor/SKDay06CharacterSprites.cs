using System.IO;
using System.Linq;
using SaintKnight.Day01;
using SaintKnight.Day06;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SaintKnight.Day06.Editor
{
    /// <summary>
    /// Imports Day 6 character pixel sprites (128x96 canvas, feet bottom 3px above the canvas bottom)
    /// and attaches SKCharacterSpriteView to the player in the Day 5 and Lobby scenes.
    /// Only adds/updates that one component; the scenes are not regenerated.
    /// Art source: Docs/Design/Characters/make_characters_v5.py (copy v5/*.png here).
    /// </summary>
    public sealed class SKDay06CharacterSprites : AssetPostprocessor
    {
        public const string ArtFolder = "Assets/SaintKnightDay06/Art/Characters";
        private static readonly string[] Scenes =
        {
            "Assets/SaintKnightDay04/Scenes/Day05_EnemyPatterns.unity",
            "Assets/SaintKnightLobby/Scenes/Lobby_Sanctuary.unity",
        };

        // Pivot puts the feet bottom (3px above the canvas bottom) on the player collider bottom:
        // BoxCollider2D 0.47x0.94 centred → bottom at local y = -0.47 = 22.56px @48 PPU. (3 + 22.56) / 96.
        public static readonly Vector2 Pivot = new Vector2(64f / 128f, (3f + .47f * 48f) / 96f);

        public const string PortraitFolder = "Assets/SaintKnightDay06/Art/Portraits";

        private void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            if (path.StartsWith(PortraitFolder))
            {
                // Side HUD full-body illustrations: smooth UI image, mipmaps for the ~1/2 downscale.
                var ui = (TextureImporter)assetImporter;
                ui.textureType = TextureImporterType.Sprite;
                ui.spriteImportMode = SpriteImportMode.Single;
                ui.filterMode = FilterMode.Bilinear;
                ui.mipmapEnabled = true;
                ui.alphaIsTransparency = true;
                ui.textureCompression = TextureImporterCompression.CompressedHQ;
                return;
            }
            if (!path.StartsWith(ArtFolder)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 48f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = true; // Day06SpriteViewTests measures the drawn weapon reach vs the hitbox.
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = Pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
        }

        [MenuItem("Tools/SaintKnight/Day 06/Attach Character Sprites To Player")]
        public static void Attach()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh();
            // First launch may import the PNGs before this postprocessor compiles: force the settings above.
            AssetDatabase.ImportAsset(ArtFolder, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(PortraitFolder, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                AttachPortraits(path);
                var player = Object.FindFirstObjectByType<SKDay01Player>();
                if (player == null) throw new IOException("No SKDay01Player in " + path);
                var view = player.GetComponent<SKCharacterSpriteView>(); // Unity fake-null: no ?? here.
                if (view == null) view = player.gameObject.AddComponent<SKCharacterSpriteView>();
                var data = new SerializedObject(view);
                Fill(data.FindProperty("cecilia"), "cecilia");
                Fill(data.FindProperty("adela"), "adela");
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save " + path);
                Debug.Log("[Day06] Character sprite view attached: " + path);
            }
        }

        /// <summary>
        /// 2D side HUD (user decision 2026-11-10, replaces the planned 3D model): stage 0~4 full-body
        /// illustrations, a taller portrait area, and the outdated "3D" labels hidden.
        /// </summary>
        private static void AttachPortraits(string scenePath)
        {
            var hud = Object.FindFirstObjectByType<SKDay01HUD>();
            if (hud == null) throw new IOException("No SKDay01HUD in " + scenePath);
            var data = new SerializedObject(hud);
            var cecilia = data.FindProperty("ceciliaStagePortraits");
            cecilia.arraySize = 5;
            for (int stage = 0; stage < 5; stage++)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PortraitFolder + "/cecilia_portrait_stage" + stage + ".png");
                if (sprite == null) throw new FileNotFoundException("Missing portrait stage " + stage);
                cecilia.GetArrayElementAtIndex(stage).objectReferenceValue = sprite;
            }
            data.ApplyModifiedPropertiesWithoutUndo(); // Adela: no illustrations yet → keeps the bust crop.
            var portrait = data.FindProperty("portrait").objectReferenceValue as UnityEngine.UI.Image;
            if (portrait == null) throw new IOException("HUD portrait image missing in " + scenePath);
            var rect = portrait.rectTransform;
            rect.anchorMin = new Vector2(.02f, .295f); // Just above CHAR_NAME (.23~.29) ...
            rect.anchorMax = new Vector2(.98f, .905f); // ... up to the project title (.91).
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            portrait.preserveAspect = true;
            foreach (var label in new[] { "HUD_TITLE", "NOTE" })
            {
                var t = hud.transform.Find(label);
                if (t != null) t.gameObject.SetActive(false);
            }
        }

        private static void Fill(SerializedProperty set, string name)
        {
            foreach (var motion in new[] { "idle", "run", "windup", "attack", "hit" })
                set.FindPropertyRelative(motion).objectReferenceValue = Load(name + "_" + motion);
            SetArray(set.FindPropertyRelative("damagedIdle"),
                Enumerable.Range(1, 3).Select(i => Load(name + "_stage" + i)).ToArray());
            SetArray(set.FindPropertyRelative("collapse"),
                Enumerable.Range(0, 12).Select(i => Load(name + "_collapse_f" + i.ToString("00"))).ToArray());
        }

        private static void SetArray(SerializedProperty array, Sprite[] sprites)
        {
            array.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }

        private static Sprite Load(string file)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/" + file + ".png");
            if (sprite == null) throw new FileNotFoundException("Missing sprite " + file);
            return sprite;
        }
    }
}
