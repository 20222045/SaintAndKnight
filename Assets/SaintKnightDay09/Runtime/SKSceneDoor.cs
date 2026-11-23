using SaintKnight.Day02;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SaintKnight.Day09
{
    /// <summary>
    /// A door that takes the player to another scene as soon as they reach it (Day 11, user decision:
    /// no key press, no lever — the enemies on the way are the gate). Not while held or down.
    /// </summary>
    public sealed class SKSceneDoor : MonoBehaviour
    {
        [SerializeField] private SKHealth player;
        [SerializeField] private string targetScene;
        [SerializeField, Min(.1f)] private float reach = .6f;

        public string TargetScene => targetScene;
        public bool PlayerInReach
        {
            get
            {
                if (player == null || player.IsDead) return false;
                var grab = player.GetComponent<SKGrabState>();
                if (grab != null && grab.IsHeld) return false;
                return Mathf.Abs(player.transform.position.x - transform.position.x) <= reach &&
                    Mathf.Abs(player.transform.position.y - transform.position.y) <= 1.2f;
            }
        }

        /// <summary>Day 25: doors made at run time (the boss chapel exit).</summary>
        public void Configure(SKHealth who, string scene, float reachX = .6f, bool pressUp = false)
        {
            player = who;
            targetScene = scene;
            reach = reachX;
            requireUp = pressUp;
        }

        /// <summary>Day 25 exit only: wait for W / Up instead of entering on touch.</summary>
        [SerializeField] private bool requireUp;
        public bool RequiresUp => requireUp;

        private static bool UpPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return SaintKnight.Day26.SKBindings.Pressed(SaintKnight.Day26.SKAction.Up);
#else
            return Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
#endif
        }

        /// <summary>Sets carry-over to <paramref name="leaving"/> + loads the target. Used by doors and the lobby.</summary>
        public static void Go(SKHealth leaving, string scene, bool fullHp = false)
        {
            var who = leaving != null ? leaving.GetComponent<SaintKnight.Day01.SKDay01Player>() : null;
            SaintKnight.Day20.SKCarry.Set(who != null ? who.SelectedCharacter : 0,
                fullHp || leaving == null ? -1f : leaving.CurrentHp);
            SceneManager.LoadScene(scene);
        }

        private void Update()
        {
            if (SaintKnight.Day26.SKPause.IsPaused || !PlayerInReach || (requireUp && !UpPressed())) return;
            enabled = false;   // load once
            Go(player, targetScene);
        }
    }
}
