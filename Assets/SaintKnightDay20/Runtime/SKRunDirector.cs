using SaintKnight.Day01;
using SaintKnight.Day02;
using SaintKnight.Day04;
using SaintKnight.Day09;
using SaintKnight.Day15;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SaintKnight.Day20
{
    /// <summary>
    /// One per playable scene, made automatically when a scene with a player loads.
    /// Day 20: applies the carried character / HP and the checkpoint to start at.
    /// Day 21: records deaths and play time in the save.
    /// Day 25: after the boss falls, marks the clear and opens a way back to the lobby.
    /// </summary>
    public sealed class SKRunDirector : MonoBehaviour
    {
        public const string LobbyScene = "Lobby_Sanctuary", VillageScene = "Village", StageScene = "Day09_Stage1", BossScene = "Day09_BossChapel";
        public const float ExitDelay = 2.5f;

        private SKDay01Player player;
        private SKHealth health;
        private SKBossAgent boss;
        private bool wasDead, clearMarked;
        private float bossDownAt = -1f;

        public static SKRunDirector Current { get; private set; }
        public SKSceneDoor ExitDoor { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            var p = FindAnyObjectByType<SKDay01Player>();
            if (p == null || p.gameObject.scene != scene) { SKCarry.Clear(); return; }
            var go = new GameObject("Run_Director");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<SKRunDirector>().player = p;
        }

        private void Awake() => Current = this;
        private void OnDestroy() { if (Current == this) Current = null; }

        private void Start()
        {
            health = player.GetComponent<SKHealth>();
            boss = FindAnyObjectByType<SKBossAgent>();
            SaintKnight.Day18.SKHitFx.Watch(gameObject.scene);
            gameObject.AddComponent<SaintKnight.Day26.SKPauseMenu>().Bind(health);
            // 2026-11-16 user decision: no HP / name / damage / control-key text on the play screen,
            // and no HP bar on enemies or the boss (kept running underneath, only not drawn).
            // Same day, user layout: play field fills the screen, illustration + orb float over it.
            var hud = FindAnyObjectByType<SKDay01HUD>();
            if (hud != null) hud.ApplyOverlayLayout();
            // 2026-11-17 face close-up (SKExpressionCutIn) removed 2026-11-19 at the user's request ("the square face is not needed")
            var cam = Camera.main;
            if (cam != null)
            {
                if (cam.TryGetComponent<SaintKnight.Day08.SKHorizontalCamera>(out var follow))
                {
                    // 2026-11-19 user: no black band left of the stage under the illustration; the camera stays
                    // inside the level and the illustration fades when she walks behind it (SKDay01HUD).
                    follow.LeftInset = 0f;
                    follow.SetSlot(new Rect(0f, 0f, 1f, 1f));
                }
                else cam.rect = new Rect(0f, 0f, 1f, 1f);
            }
            var bossBar = GameObject.Find("Boss_HP_Bar");
            if (bossBar != null)
                foreach (var r in bossBar.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            if (SKCarry.Pending)
            {
                player.SelectCharacter(SKCarry.Character);
                if (SKCarry.Hp > 0f) health.RestoreHp(SKCarry.Hp);
                if (!string.IsNullOrEmpty(SKCarry.Checkpoint)) StartAt(SKCarry.Checkpoint);
                SKCarry.Clear();
                // Entering a playable room by door / menu saves it as the place to continue from.
                if (FindAnyObjectByType<SKEnemyTrainingSession>() != null)
                {
                    var save = SKSave.Data;
                    if (save.scene != gameObject.scene.name) { save.scene = gameObject.scene.name; save.checkpoint = ""; }
                    save.character = player.SelectedCharacter;
                    SKSave.Write();
                }
            }
        }

        private void StartAt(string checkpointName)
        {
            foreach (var cp in FindObjectsByType<SKCheckpoint>(FindObjectsSortMode.None))
            {
                if (cp.name != checkpointName) continue;
                cp.LightQuietly();
                player.transform.position = cp.RespawnPoint();
                Physics2D.SyncTransforms();
                var session = FindAnyObjectByType<SKEnemyTrainingSession>();
                if (session != null) session.SetSpawn(cp.RespawnPoint());
                return;
            }
        }

        private void Update()
        {
            SKSave.Data.playSeconds += Time.unscaledDeltaTime;
            bool dead = health != null && health.IsDead;
            if (dead && !wasDead) SKSave.RecordDeath(player.SelectedCharacter, SKGameOverScreen.KeyFor(health.LastCause));
            wasDead = dead;
            WatchBoss();
        }

        // Day 25: boss down -> record the clear once, then a light opens at the entrance back to the lobby.
        private void WatchBoss()
        {
            if (boss == null) return;
            if (boss.State != SKBossState.Dead) { bossDownAt = -1f; return; }
            if (bossDownAt < 0f) bossDownAt = Time.time;
            if (!clearMarked)
            {
                clearMarked = true;
                var save = SKSave.Data;
                if (save.clears == 0) save.returnRevealPending = true;
                save.bossDefeated = true;
                save.clears++;
                save.scene = save.checkpoint = "";   // the run is over; Continue starts a new sortie
                SKSave.Unlock("boss_rose_madonna");
                SKSave.Write();
            }
            if (ExitDoor == null && Time.time - bossDownAt >= ExitDelay) OpenExit();
        }

        private void OpenExit()
        {
            var cam = Camera.main;
            // clear of the illustration that stands in the lower-left corner (it reaches ~33% of the width)
            float half = cam != null ? cam.orthographicSize * cam.aspect : 0f;
            float x = cam != null ? cam.transform.position.x - half + half * 2f * .46f : player.transform.position.x;
            var door = new GameObject("Exit_To_Lobby");
            SceneManager.MoveGameObjectToScene(door, gameObject.scene);
            var spawn = FindAnyObjectByType<SKEnemyTrainingSession>();
            float y = spawn != null ? spawn.Spawn.y : player.transform.position.y;
            door.transform.position = new Vector3(x, y, 0f);
            ExitDoor = door.AddComponent<SKSceneDoor>();
            // 2026-11-19 user: clearing stage 1 leads to the village, not the lobby
            ExitDoor.Configure(health, VillageScene, .8f, true);
            var label = new GameObject("Label").AddComponent<TextMesh>();
            label.transform.SetParent(door.transform, false);
            label.transform.localPosition = new Vector3(0f, .55f, 0f);
            label.transform.localScale = new Vector3(1f / .7f, 1f / 1.6f, 1f);
            label.text = "W  TO THE VILLAGE";
            label.characterSize = .04f;
            label.fontSize = 48;
            label.anchor = TextAnchor.MiddleCenter;
            label.color = new Color(1f, .86f, .55f);
            label.GetComponent<MeshRenderer>().sortingOrder = 32;
            var glow = door.AddComponent<SpriteRenderer>();
            var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            glow.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1f);
            glow.color = new Color(1f, .86f, .55f, .35f);
            door.transform.localScale = new Vector3(.7f, 1.6f, 1f);
            glow.sortingOrder = 8;
        }
    }
}
