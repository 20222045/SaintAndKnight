using System.IO;
using System.Linq;
using SaintKnight.Day01;
using SaintKnight.Day02;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SaintKnight.Day04.Editor
{
    public static class SKEnemySceneBuilder
    {
        public const string Day04Path = "Assets/SaintKnightDay04/Scenes/Day04_EnemyPatrol.unity";
        public const string Day05Path = "Assets/SaintKnightDay04/Scenes/Day05_EnemyPatterns.unity";

        [MenuItem("Tools/SaintKnight/Day 04-05/Create Enemy Training Scenes")]
        public static void CreateScenes()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory("Assets/SaintKnightDay04/Scenes");
            CreateScene(Day04Path, false);
            CreateScene(Day05Path, true);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(Day05Path, true),
                new EditorBuildSettingsScene(Day04Path, true)
            }.Concat(EditorBuildSettings.scenes.Where(s => s.path != Day04Path && s.path != Day05Path)).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("DAY04_05_SCENES_READY");
        }

        private static void CreateScene(string path, bool secondEnemy)
        {
            var scene = EditorSceneManager.OpenScene("Assets/SaintKnightDay02/Scenes/Day02_CombatTraining.unity");
            var player = Object.FindFirstObjectByType<SKDay01Player>().GetComponent<SKHealth>();
            if (secondEnemy) ConfigureReferenceCombat(player.GetComponent<SKPlayerCombat>());
            var template = Object.FindFirstObjectByType<SKTrainingTarget>().gameObject;
            Object.DestroyImmediate(template.GetComponent<SKTrainingTarget>());
            Object.DestroyImmediate(Object.FindFirstObjectByType<SKTrainingSession>().gameObject);
            var a = Configure(template, player, false);
            SKEnemyAgent[] enemies;
            if (secondEnemy)
            {
                a.transform.position = new Vector3(.25f, -2.15f, 0f);
                ConfigureGrab(a);
                var b = Configure(Object.Instantiate(template), player, true);
                b.transform.position = new Vector3(4.25f, -2.15f, 0f);
                // Heavy commitment: hits do not cancel B's strike, and repeated hits cannot stun-lock it.
                var heavy = new SerializedObject(b);
                heavy.FindProperty("hurtRecoverySeconds").floatValue = .3f;
                heavy.FindProperty("strikeArmored").boolValue = true;
                heavy.FindProperty("counterArmorAfterHit").boolValue = true;
                // High swing: bottom 0.8 above the floor, so a crouch (0.72) passes under it; standing (0.94) is hit.
                heavy.FindProperty("attackSize").vector2Value = new Vector2(1.6f, .5f);
                heavy.FindProperty("attackHeight").floatValue = .475f;
                heavy.ApplyModifiedPropertiesWithoutUndo();
                enemies = new[] { a, b };
            }
            else enemies = new[] { a };

            var texts = Object.FindFirstObjectByType<Canvas>().GetComponentsInChildren<Text>();
            texts.First(t => t.name == "HUD_TITLE").text = secondEnemy ? "ENEMY PATTERNS - DAY 05" : "ENEMY PATROL - DAY 04";
            var status = texts.First(t => t.name == "Day02_Target_Status");
            status.name = "Enemy_Training_Status";
            status.fontSize = 17;
            status.text = secondEnemy ? "ENEMY A / ENEMY B\nR TO RESET TRAINING" : "ENEMY A\nR TO RESET TRAINING";
            texts.First(t => t.name == "NOTE").text = "TEST TARGET GRAPHICS - CHARACTER DESIGN PENDING";
            var session = new GameObject("Enemy_Training_Session").AddComponent<SKEnemyTrainingSession>();
            var serialized = new SerializedObject(session);
            serialized.FindProperty("player").objectReferenceValue = player;
            serialized.FindProperty("status").objectReferenceValue = status;
            serialized.FindProperty("autoRetrySeconds").floatValue = secondEnemy ? 1.6f : 0f;
            var entries = serialized.FindProperty("enemies");
            entries.arraySize = enemies.Length;
            for (int i = 0; i < enemies.Length; i++) entries.GetArrayElementAtIndex(i).objectReferenceValue = enemies[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException("Could not save " + path);
        }

        private static SKEnemyAgent Configure(GameObject go, SKHealth player, bool heavy)
        {
            go.name = heavy ? "Enemy_B_Heavy_TestTarget" : "Enemy_A_Patrol_TestTarget";
            var solid = go.GetComponent<BoxCollider2D>();
            if (solid == null) solid = go.AddComponent<BoxCollider2D>();
            solid.size = new Vector2(.7f, 1.15f);
            var body = go.GetComponent<Rigidbody2D>();
            if (body == null) body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 2.2f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var health = new SerializedObject(go.GetComponent<SKHealth>());
            health.FindProperty("maximumHp").floatValue = heavy ? 80f : 60f;
            health.ApplyModifiedPropertiesWithoutUndo();
            var agent = go.GetComponent<SKEnemyAgent>();
            if (agent == null) agent = go.AddComponent<SKEnemyAgent>();
            var data = new SerializedObject(agent);
            data.FindProperty("displayName").stringValue = heavy ? "ENEMY B - HEAVY" : "ENEMY A - PATROL";
            data.FindProperty("player").objectReferenceValue = player;
            data.FindProperty("hitbox").objectReferenceValue = go.GetComponentInChildren<SKHitbox>();
            data.FindProperty("warning").objectReferenceValue = go.transform.Find("Attack_Warning").GetComponent<SpriteRenderer>();
            data.FindProperty("patrolSpeed").floatValue = heavy ? .45f : .7f;
            data.FindProperty("chaseSpeed").floatValue = heavy ? .95f : 1.5f;
            data.FindProperty("patrolRadius").floatValue = heavy ? .65f : 1.2f;
            data.FindProperty("attackRange").floatValue = heavy ? 1.5f : 1.1f;
            data.FindProperty("attackSize").vector2Value = new Vector2(heavy ? 1.6f : 1f, .85f);
            data.FindProperty("attackOffset").floatValue = heavy ? 1f : .75f;
            data.FindProperty("windupSeconds").floatValue = heavy ? .85f : .4f;
            data.FindProperty("recoverySeconds").floatValue = heavy ? 1.5f : 1f;
            data.FindProperty("attackSeconds").floatValue = heavy ? .25f : .18f;
            data.FindProperty("damage").floatValue = heavy ? 25f : 15f;
            data.FindProperty("knockback").floatValue = heavy ? 3f : 2f;
            data.ApplyModifiedPropertiesWithoutUndo();
            return agent;
        }

        // Day 5 only: A answers hugging with a short-telegraph grab and a mash-to-escape hold.
        // Structure follows the reference game's restraint + struggle meter; all numbers are ours.
        private static void ConfigureGrab(SKEnemyAgent agent)
        {
            var data = new SerializedObject(agent);
            data.FindProperty("grabEnabled").boolValue = true;
            data.FindProperty("grabRange").floatValue = .55f;
            // 0.45 s: simulated playtest showed 0.3 s could not be dodged with a 0.2 s reaction.
            data.FindProperty("grabWindupSeconds").floatValue = .45f;
            data.FindProperty("grabWindupArmored").boolValue = true;
            // 0.2 s + armored counter windup: point-blank mashing meets A's strike before the third hit.
            data.FindProperty("hurtRecoverySeconds").floatValue = .2f;
            data.FindProperty("counterArmorAfterHit").boolValue = true;
            data.FindProperty("grabTickDamage").floatValue = 4f;
            data.FindProperty("grabTickSeconds").floatValue = .5f;
            data.FindProperty("grabMaxHoldSeconds").floatValue = 4f;
            data.FindProperty("grabThrowDamage").floatValue = 12f;
            data.FindProperty("struggleThreshold").floatValue = 6f;
            data.FindProperty("struggleDecayPerSecond").floatValue = 2.5f;
            data.FindProperty("escapeStaggerSeconds").floatValue = 1.4f;
            data.FindProperty("escapeInvulnerabilitySeconds").floatValue = .6f;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        // Prototype values for our two-character design, not measurements from the reference game.
        private static void ConfigureReferenceCombat(SKPlayerCombat combat)
        {
            var feel = new SerializedObject(combat.GetComponent<SKDay01Player>());
            feel.FindProperty("jumpReleaseVelocityScale").floatValue = .45f;
            feel.FindProperty("airSpeedMultiplier").floatValue = .85f;
            feel.FindProperty("faceAttackerOnHit").boolValue = true;
            feel.FindProperty("jumpDuringAttack").boolValue = false;
            feel.FindProperty("crouchHeight").floatValue = .72f;
            feel.ApplyModifiedPropertiesWithoutUndo();
            var data = new SerializedObject(combat);
            var reach = data.FindProperty("cecilia");
            reach.FindPropertyRelative("damage").floatValue = 20f;
            // Far edge = drawn flail/trail tip of the Day 6 attack sprite (1.02u); near edge unchanged (0.3u).
            reach.FindPropertyRelative("size").vector2Value = new Vector2(.72f, .8f);
            reach.FindPropertyRelative("offset").floatValue = .66f;
            reach.FindPropertyRelative("windup").floatValue = .06f;
            reach.FindPropertyRelative("duration").floatValue = .16f;
            reach.FindPropertyRelative("cooldown").floatValue = .45f;
            reach.FindPropertyRelative("knockback").floatValue = 0f;
            // Day 7 chain trait: cancelling a tell from range pushes the next attack back.
            reach.FindPropertyRelative("interruptDelay").floatValue = .2f;
            var impact = data.FindProperty("adela");
            impact.FindPropertyRelative("damage").floatValue = 35f;
            // Far edge = drawn sword trail tip of the Day 6 attack sprite (0.85u); near edge unchanged (0.2u).
            impact.FindPropertyRelative("size").vector2Value = new Vector2(.65f, .85f);
            impact.FindPropertyRelative("offset").floatValue = .525f;
            impact.FindPropertyRelative("windup").floatValue = .14f;
            impact.FindPropertyRelative("duration").floatValue = .2f;
            impact.FindPropertyRelative("cooldown").floatValue = .7f;
            impact.FindPropertyRelative("knockback").floatValue = 1.8f;
            // Day 7 sword trait: cuts B's armored swing and A's grab tell (not the post-hit counter).
            impact.FindPropertyRelative("breaksArmor").boolValue = true;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
