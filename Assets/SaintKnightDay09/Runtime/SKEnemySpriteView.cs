using System;
using SaintKnight.Day01;
using SaintKnight.Day02;
using SaintKnight.Day04;
using UnityEngine;

namespace SaintKnight.Day09
{
    /// <summary>
    /// Display only: picks the enemy frame from SKEnemyAgent state. Never starts, stops or times combat.
    /// While this enemy holds the player, one merged drawing (monk + heroine) replaces both sprites.
    /// Uses scaled time, so hit-stop freezes the drawing with the fight.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer), typeof(SKEnemyAgent))]
    public sealed class SKEnemySpriteView : MonoBehaviour
    {
        [Serializable]
        public sealed class GrabSet
        {
            public Sprite[] pounce, hold, resist;
            [Tooltip("Shown briefly after release, while the real bodies separate.")]
            public Sprite[] escape, thrown;
        }

        [SerializeField] private SKHealth player;
        [SerializeField] private Sprite[] walk, idle, windup, strike, recovery, hit, defeat;
        [Tooltip("Monk only.")]
        [SerializeField] private Sprite[] grabWindup, grabMiss;
        [Tooltip("Executioner only.")]
        [SerializeField] private Sprite[] armorBreak;
        [Tooltip("Index = SKDay01Player.SelectedCharacter (0 Cecilia, 1 Adela). Empty = no merged drawing.")]
        [SerializeField] private GrabSet[] grab;
        [Tooltip("Copied from the agent so windup/strike frames span the real phase.")]
        [SerializeField, Min(.01f)] private float windupSeconds = .4f, strikeSeconds = .18f;
        [Tooltip("Ground travel per walk frame, world units.")]
        [SerializeField, Min(.01f)] private float strideUnits = .2f;

        private const float IdleFrame = .5f, DefeatFrame = .25f, PounceFrame = .1f, HoldFrame = .2f,
            ResistShow = .18f, ArmorBreakShow = .5f;
        // Release frames last until the real bodies roughly reach the drawn gap: escape pushes the player
        // at 3 u/s (14 -> ~36 px, drawn 38), the throw knockback at 3 u/s plus hit-stop (14 -> ~50 px, drawn 50).
        private const float EscapeShow = .15f, ThrowShow = .25f, ReleaseMaxGap = 1.5f;
        private SpriteRenderer display, playerDisplay;
        private SKEnemyAgent agent;
        private Rigidbody2D body;
        private SKGrabState playerGrab;
        private SKDay01Player playerInput;
        private SKEnemyState shown = SKEnemyState.Disabled;
        private float enteredAt, walked, lastStruggleSeen = float.NegativeInfinity;
        private int struggles;
        private bool hidingPlayer, wasHolding;
        private Sprite releaseFrame;
        private float releaseUntil = float.NegativeInfinity;

        private void Awake()
        {
            display = GetComponent<SpriteRenderer>();
            agent = GetComponent<SKEnemyAgent>();
            body = GetComponent<Rigidbody2D>();
            if (player == null) return;
            playerDisplay = player.GetComponent<SpriteRenderer>();
            playerInput = player.GetComponent<SKDay01Player>();
            var combat = player.GetComponent<SKPlayerCombat>();
            playerGrab = combat != null ? combat.Grab : player.GetComponent<SKGrabState>();
        }

        private void OnDisable() => ShowPlayer();

        private void LateUpdate()
        {
            var state = agent.State;
            if (state != shown) { shown = state; enteredAt = Time.time; }
            float t = Time.time - enteredAt;

            bool holding = agent.IsHoldingPlayer;
            if (wasHolding && !holding) BeginRelease();
            wasHolding = holding;
            var merged = holding ? MergedFrame() : ReleaseFrame(state);
            if (merged != null)
            {
                display.sprite = merged;
                HidePlayer();
                return;
            }
            ShowPlayer();

            Sprite frame = state switch
            {
                SKEnemyState.Dead => Hold(defeat, t / DefeatFrame),
                SKEnemyState.Hurt => Time.time - agent.ArmorBrokenAt < ArmorBreakShow && Has(armorBreak)
                    ? armorBreak[0] : First(hit),
                SKEnemyState.Windup => Spread(windup, t / windupSeconds),
                SKEnemyState.Attacking => Spread(strike, t / strikeSeconds),
                SKEnemyState.Recovery => agent.RecoveringFromGrabMiss && Has(grabMiss) ? grabMiss[0] : First(recovery),
                SKEnemyState.GrabWindup => Has(grabWindup) ? grabWindup[0] : First(windup),
                SKEnemyState.Holding => First(idle),
                SKEnemyState.Disabled => null,
                _ => Moving(),
            };
            if (frame != null) display.sprite = frame;
        }

        private Sprite Moving()
        {
            float speed = body != null ? Mathf.Abs(body.linearVelocity.x) : 0f;
            if (speed < .05f || !Has(walk)) return Loop(idle, Time.time / IdleFrame);
            walked += speed * Time.deltaTime;
            return Loop(walk, walked / strideUnits);
        }

        private Sprite MergedFrame()
        {
            if (playerGrab == null || playerInput == null) return null;
            var set = CurrentSet();
            if (set == null || !Has(set.hold)) return null;
            float since = Time.time - playerGrab.HeldSince;
            if (Has(set.pounce) && since < set.pounce.Length * PounceFrame) return Hold(set.pounce, since / PounceFrame);
            if (playerGrab.LastStruggleAt != lastStruggleSeen)
            {
                lastStruggleSeen = playerGrab.LastStruggleAt;
                struggles++;
            }
            if (Has(set.resist) && Time.time - playerGrab.LastStruggleAt < ResistShow)
                return set.resist[struggles % set.resist.Length];
            return Loop(set.hold, since / HoldFrame);
        }

        private GrabSet CurrentSet() => playerInput != null && grab != null &&
            playerInput.SelectedCharacter < grab.Length ? grab[playerInput.SelectedCharacter] : null;

        // Escape (struggle won) or throw (hold timed out, player hit); a reset or death shows neither.
        private void BeginRelease()
        {
            var set = CurrentSet();
            releaseFrame = null;
            if (set == null || playerGrab == null || player.IsDead) return;
            // Release clears HeldSince; only a struggle win stamps EscapedAt (this frame).
            if (Time.time - playerGrab.EscapedAt < .1f && Has(set.escape))
            {
                releaseFrame = set.escape[0];
                releaseUntil = Time.time + EscapeShow;
            }
            else if (player.IsStunned && Has(set.thrown))
            {
                releaseFrame = set.thrown[0];
                releaseUntil = Time.time + ThrowShow;
            }
        }

        private Sprite ReleaseFrame(SKEnemyState state)
        {
            if (releaseFrame == null) return null;
            bool together = Mathf.Abs(player.transform.position.x - transform.position.x) < ReleaseMaxGap;
            if (Time.time < releaseUntil && state == SKEnemyState.Recovery && together && !player.IsDead)
                return releaseFrame;
            releaseFrame = null;
            return null;
        }

        private void HidePlayer()
        {
            if (playerDisplay == null) return;
            playerDisplay.enabled = false;
            hidingPlayer = true;
        }

        private void ShowPlayer()
        {
            if (!hidingPlayer || playerDisplay == null) return;
            playerDisplay.enabled = true;
            hidingPlayer = false;
        }

        private static bool Has(Sprite[] frames) => frames != null && frames.Length > 0;
        private static Sprite First(Sprite[] frames) => Has(frames) ? frames[0] : null;
        private static Sprite Loop(Sprite[] frames, float f) =>
            Has(frames) ? frames[(int)Mathf.Repeat(Mathf.Floor(f), frames.Length)] : null;
        private static Sprite Hold(Sprite[] frames, float f) =>
            Has(frames) ? frames[Mathf.Clamp((int)f, 0, frames.Length - 1)] : null;
        private static Sprite Spread(Sprite[] frames, float progress) =>
            Has(frames) ? frames[Mathf.Clamp((int)(progress * frames.Length), 0, frames.Length - 1)] : null;
    }
}
