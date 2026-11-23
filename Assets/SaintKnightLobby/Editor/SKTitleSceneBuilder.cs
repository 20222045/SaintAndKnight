using System.Linq;
using SaintKnight.Day26;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SaintKnight.Lobby.EditorTools
{
    /// <summary>Day 26: the title scene (camera + <see cref="SKTitleMenu"/>), first in the build.</summary>
    public static class SKTitleSceneBuilder
    {
        public const string ScenePath = "Assets/SaintKnightDay26/Scenes/Title.unity";

        [MenuItem("Tools/SaintKnight/Day 26/Create Title Scene")]
        public static void Create()
        {
            System.IO.Directory.CreateDirectory("Assets/SaintKnightDay26/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.gameObject.AddComponent<AudioListener>();
            new GameObject("Title_Menu").AddComponent<SKTitleMenu>();
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new System.IO.IOException("Could not save " + ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
                .Concat(EditorBuildSettings.scenes.Where(s => s.path != ScenePath)).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("DAY26_TITLE_READY");
        }
    }
}
