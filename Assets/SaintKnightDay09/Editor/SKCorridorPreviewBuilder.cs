using System.IO;
using SaintKnight.Day01;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SaintKnight.Day09
{
    public static class SKCorridorPreviewBuilder
    {
        private const string Stage = "Assets/SaintKnightDay09/Scenes/Day09_Stage1.unity";
        public const string Preview = "Assets/SaintKnightDay09/Scenes/Corridor3DPreview.unity";
        private const string Menu = "Tools/SaintKnight/Preview/";

        [MenuItem(Menu + "Open Corridor 3D Preview")]
        private static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(Preview);
        }

        [MenuItem(Menu + "Toggle 2D / 3D Corridor")]
        private static void Toggle()
        {
            var preview = Object.FindFirstObjectByType<SKCorridorPreview>();
            preview.ShowGeometry = !preview.ShowGeometry;
        }

        [MenuItem(Menu + "Toggle 2D / 3D Corridor", true)]
        private static bool CanToggle()
        {
            return EditorApplication.isPlaying && Object.FindFirstObjectByType<SKCorridorPreview>() != null;
        }

        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var source = EditorSceneManager.OpenScene(Stage, OpenSceneMode.Single);
            // Save the copy before moving the spawn or adding the preview renderer.
            if (!EditorSceneManager.SaveScene(source, Preview))
                throw new IOException("Could not create the corridor preview scene.");

            var player = Object.FindFirstObjectByType<SKDay01Player>();
            var spawn = player.transform.position;
            spawn.x = SKCorridorPreview.StageX(800f);
            player.transform.position = spawn;

            var root = new GameObject("Corridor 3D Preview");
            root.AddComponent<SKCorridorPreview>().Configure(Camera.main,
                AssetDatabase.LoadAssetAtPath<Shader>("Assets/SaintKnightDay09/Art/SKCorridorStone.shader"),
                AssetDatabase.LoadAssetAtPath<Shader>("Assets/SaintKnightDay09/Art/SKCorridorComposite.shader"));
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene()))
                throw new IOException("Could not save the corridor preview scene.");
        }
    }
}
