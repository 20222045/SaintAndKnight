using SaintKnight.Day20;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SaintKnight.Day20.EditorTools
{
    /// <summary>
    /// Adds the Day 20 checkpoints to Day09_Stage1 without rebuilding the scene (other objects untouched).
    /// Art pixels follow stage1_layout.json: world x = -6.6666665 + px / 48, floor y310 -> world y.
    /// </summary>
    public static class SKCheckpointPlacer
    {
        public const string StagePath = "Assets/SaintKnightDay09/Scenes/Day09_Stage1.unity";
        // after the slime pool / floor tentacles, and past the upper floor before the flesh wall
        private static readonly (string name, float px)[] Points = { ("Checkpoint_Nave", 1340f), ("Checkpoint_Forecourt", 1840f) };

        [MenuItem("Tools/SaintKnight/Day 20/Place Stage 1 Checkpoints")]
        public static void PlaceInStage()
        {
            var scene = EditorSceneManager.OpenScene(StagePath);
            Place();
            if (!EditorSceneManager.SaveScene(scene)) throw new System.IO.IOException("Could not save " + StagePath);
            Debug.Log("DAY20_CHECKPOINTS_PLACED");
        }

        /// <summary>Into the open scene; also called by the Day 9 scene builder so a rebuild keeps them.</summary>
        public static void Place()
        {
            foreach (var old in Object.FindObjectsByType<SKCheckpoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(old.gameObject);
            foreach (var (name, px) in Points)
            {
                var go = new GameObject(name);
                go.transform.position = new Vector3(-6.6666665f + px / 48f, (180f - 310f) / 48f, 0f);
                go.AddComponent<SKCheckpoint>();
            }
        }
    }
}
