using SaintKnight.Day01;
using SaintKnight.Day02;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SaintKnight.Day04
{
    public sealed class SKEnemyTrainingSession : MonoBehaviour
    {
        [SerializeField] private SKHealth player;
        [SerializeField] private SKEnemyAgent[] enemies;
        [SerializeField] private Text status;
        [Tooltip("Seconds after the player is incapacitated before the room restarts. 0 = wait for R.")]
        [SerializeField, Min(0f)] private float autoRetrySeconds;
        private Vector3 spawn;
        private float downSince = -1f;
        public float AutoRetrySeconds => autoRetrySeconds;
        public SKHealth Player => player;
        public Vector3 Spawn => spawn;
        /// <summary>Day 20 checkpoint: R / auto-retry now brings the player back here.</summary>
        public void SetSpawn(Vector3 position) { spawn = position; spawnSet = true; }
        private bool spawnSet;
        /// <summary>Day 15: a game-over cutscene is on screen, so only R restarts the room.</summary>
        public bool HoldRetry { get; set; }
        /// <summary>Seconds after going down before any key (not only R) restarts the room.</summary>
        public const float AnyKeyGrace = .8f;
        /// <summary>Raised after R / auto-retry restores the room (bosses reset themselves here).</summary>
        public event System.Action TrainingReset;
        private void Start()
        {
            if (!spawnSet) spawn = player.transform.position;
            gameObject.AddComponent<SaintKnight.Day15.SKGameOverScreen>().Bind(player, this);
        }

        private void Update()
        {
            if (SaintKnight.Day26.SKPause.IsPaused) return;
#if ENABLE_INPUT_SYSTEM
            bool retry = SaintKnight.Day26.SKBindings.Pressed(SaintKnight.Day26.SKAction.Retry);
            bool anyKey = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame
                && !SaintKnight.Day26.SKBindings.Pressed(SaintKnight.Day26.SKAction.Pause);
#else
            bool retry = Input.GetKeyDown(KeyCode.R);
            bool anyKey = Input.anyKeyDown && !Input.GetKeyDown(KeyCode.Escape);
#endif
            // 2026-11-20 user: once down, any key (except pause) restarts; a short grace keeps struggle mashing from skipping the death.
            if (retry || player.IsDead && downSince >= 0f && Time.time - downSince >= AnyKeyGrace && anyKey)
            {
                ResetTraining();
                return;
            }
            // Short failure loop: the room restarts by itself so the next attempt comes quickly.
            if (!player.IsDead) downSince = -1f;
            else if (downSince < 0f) downSince = Time.time;
            else if (autoRetrySeconds > 0f && !HoldRetry && Time.time - downSince >= autoRetrySeconds) ResetTraining();

            if (status == null) return;
            var grab = player.GetComponent<SKGrabState>();
            string footer;
            if (player.IsDead)
                footer = autoRetrySeconds > 0f && downSince >= 0f
                    ? "PLAYER DOWN - RETRY IN " + Mathf.Max(0f, autoRetrySeconds - (Time.time - downSince)).ToString("0.0") + "s (R NOW)"
                    : "PLAYER DOWN - R TO RESET";
            else if (grab != null && grab.IsHeld)
                footer = "HELD! MASH J / SPACE / A-D  " + Meter(grab.StruggleProgress);
            else footer = "R  RETRY      ESC  PAUSE";
            status.text = footer;
        }

        private static string Meter(float progress)
        {
            int filled = Mathf.Clamp(Mathf.RoundToInt(progress * 10f), 0, 10);
            return "[" + new string('#', filled) + new string('-', 10 - filled) + "]";
        }

        public void ResetTraining()
        {
            SKHitStop.CancelActive();
            downSince = -1f;
            player.ResetHealth();
            player.GetComponent<SKPlayerCombat>().ResetCombat();
            player.transform.position = spawn;
            player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                enemy.Health.ResetHealth();
                enemy.ResetAgent();
            }
            Physics2D.SyncTransforms();
            TrainingReset?.Invoke();
        }
    }
}
