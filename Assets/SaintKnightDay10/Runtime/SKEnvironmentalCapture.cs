using System.Collections.Generic;
using SaintKnight.Day01;
using SaintKnight.Day02;
using SaintKnight.Day04;
using UnityEngine;

namespace SaintKnight.Day10
{
    public enum SKEnvironmentalCaptureKind { Slime, Tentacle }

    /// <summary>
    /// Trap-side owner of a restraint. The existing player input/struggle state remains authoritative;
    /// this component pins the body, drains HP and absorbs a player who stops resisting.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SKEnvironmentalCapture : MonoBehaviour
    {
        // Day 15: an unresisted hold eats a full HP bar in QuietSeconds; a struggle press pauses it for ResistGrace.
        private const float QuietSeconds = 4f, ResistGrace = .35f;
        private const float StruggleThreshold = 6f, StruggleDecay = 2.5f, EscapeImmunity = .6f, EscapeRest = 1.5f;
        private static readonly List<SKEnvironmentalCapture> Captures = new List<SKEnvironmentalCapture>();

        private SKDay01Player player;
        private SKHealth health;
        private SKGrabState grab;
        private SKPlayerCombat combat;
        private Rigidbody2D body;
        private SpriteRenderer playerView;
        private SKEnemyTrainingSession session;
        private Vector2 holdAt;
        private float readyAt;
        private bool hiddenByCapture;
        private bool hasBoundPlayer;

        public SKEnvironmentalCaptureKind Kind { get; private set; }
        public SKDay01Player Player => player;
        public bool IsHoldingPlayer => grab != null && grab.IsHeld && grab.Holder == this;
        public bool IsPlayerConsumed { get; private set; }
        public float ConsumedAt { get; private set; } = float.NegativeInfinity;
        public float EscapedAt { get; private set; } = float.NegativeInfinity;
        /// <summary>Day 15: how far the body is swallowed = HP already lost (1 at HP 0).</summary>
        public float AbsorptionProgress => IsPlayerConsumed ? 1f : IsHoldingPlayer
            ? Mathf.Clamp01(1f - health.CurrentHp / health.MaximumHp) : 0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Captures.Clear();

        private void OnEnable() => Captures.Add(this);

        private void Start()
        {
            session = FindFirstObjectByType<SKEnemyTrainingSession>();
            if (session != null) session.TrainingReset += ResetCapture;
        }

        public void Configure(SKEnvironmentalCaptureKind kind)
        {
            if (Kind != kind && (IsHoldingPlayer || IsPlayerConsumed)) ResetCapture();
            Kind = kind;
        }

        /// <summary>
        /// A floor tentacle may bypass only the immunity just granted by its own accepted hit.
        /// Other callers use the default and respect ordinary immunity.
        /// </summary>
        public bool TryCapture(SKDay01Player who, bool allowInvulnerabilityFromAcceptedHit = false)
        {
            if (!isActiveAndEnabled || who == null || !who.isActiveAndEnabled ||
                IsHoldingPlayer || IsPlayerConsumed || Time.time < readyAt || SKHitStop.IsActive) return false;
            var target = who.GetComponent<SKHealth>();
            var targetCombat = who.GetComponent<SKPlayerCombat>();
            var targetGrab = targetCombat != null ? targetCombat.Grab : null;
            if (target == null || target.Team != SKTeam.Player || !target.isActiveAndEnabled || target.IsDead ||
                targetCombat == null || !targetCombat.isActiveAndEnabled || targetGrab == null ||
                !targetGrab.TryBegin(this, StruggleThreshold, StruggleDecay, allowInvulnerabilityFromAcceptedHit))
                return false;

            UnbindPlayer();
            player = who;
            health = target;
            grab = targetGrab;
            combat = targetCombat;
            body = who.GetComponent<Rigidbody2D>();
            playerView = who.GetComponent<SpriteRenderer>();
            health.Changed += OnHealthChanged;
            holdAt = who.transform.position;
            IsPlayerConsumed = false;
            ConsumedAt = EscapedAt = float.NegativeInfinity;
            hasBoundPlayer = true;
            Pin();
            return true;
        }

        /// <summary>Includes the absorbed state after SKGrabState has released its holder.</summary>
        public static SKEnvironmentalCapture ForPlayer(SKDay01Player who)
        {
            if (who == null) return null;
            foreach (var capture in Captures)
                if (capture != null && capture.player == who && (capture.IsHoldingPlayer || capture.IsPlayerConsumed))
                    return capture;
            return null;
        }

        // Lifecycle cleanup also runs while scaled physics is stopped by hit-stop.
        private void Update()
        {
            if (hasBoundPlayer && PlayerUnavailable()) ResetCapture();
        }

        private bool PlayerUnavailable() => player == null || !player.isActiveAndEnabled ||
            health == null || !health.isActiveAndEnabled || combat == null || !combat.isActiveAndEnabled ||
            grab == null || !grab.isActiveAndEnabled;

        private void FixedUpdate()
        {
            if (!hasBoundPlayer) return;
            if (PlayerUnavailable())
            {
                ResetCapture();
                return;
            }
            if (IsPlayerConsumed)
            {
                if (!health.IsDead) { ResetCapture(); return; }
                Pin();
                SetHidden(true);
                return;
            }
            if (!IsHoldingPlayer) { EndHold(false); return; }
            if (SKHitStop.IsActive) return;
            Pin();
            if (health.IsDead) { Consume(); return; }
            if (grab.EscapeReady) { EndHold(true); return; }
            if (Time.time - grab.LastStruggleAt >= ResistGrace)
                health.Drain(health.MaximumHp / QuietSeconds * Time.fixedDeltaTime, this);
        }

        private void OnHealthChanged()
        {
            if (IsPlayerConsumed)
            {
                if (health != null && !health.IsDead) ResetCapture();
            }
            else if (IsHoldingPlayer && health != null && health.IsDead) Consume();
        }

        private void Consume()
        {
            if (IsPlayerConsumed || player == null) return;
            IsPlayerConsumed = true;
            ConsumedAt = Time.time;
            Pin();
            if (grab != null) grab.Release(this);
            SetHidden(true);
            if (health != null && !health.IsDead) health.Drain(health.CurrentHp, this);
        }

        private void EndHold(bool escaped)
        {
            if (escaped) EscapedAt = Time.time;
            var releasedHealth = health;
            var releasedBody = body;
            float away = holdAt.x >= transform.position.x ? 1f : -1f;
            if (IsHoldingPlayer) grab.Release(this, escaped);
            readyAt = Time.time + EscapeRest;
            RestorePlayer();
            if (escaped && releasedHealth != null && !releasedHealth.IsDead)
            {
                releasedHealth.GrantInvulnerability(EscapeImmunity);
                if (releasedBody != null) releasedBody.linearVelocity = new Vector2(away * 3f, 2f);
            }
        }

        /// <summary>R/retry, trap clear/spawn, disable and destruction restore the original player rendering.</summary>
        public void ResetCapture()
        {
            ConsumedAt = EscapedAt = float.NegativeInfinity;
            if (IsHoldingPlayer) grab.Release(this);
            readyAt = 0f;
            RestorePlayer();
        }

        private void RestorePlayer()
        {
            SetHidden(false);
            IsPlayerConsumed = false;
            UnbindPlayer();
            player = null;
            health = null;
            grab = null;
            combat = null;
            body = null;
            playerView = null;
            hasBoundPlayer = false;
        }

        private void UnbindPlayer()
        {
            if (health != null) health.Changed -= OnHealthChanged;
        }

        private void Pin()
        {
            if (body != null) { body.position = holdAt; body.linearVelocity = Vector2.zero; }
            if (player != null) player.transform.position = new Vector3(holdAt.x, holdAt.y, player.transform.position.z);
        }

        private void SetHidden(bool hidden)
        {
            if (hiddenByCapture == hidden) return;
            if (playerView != null) playerView.forceRenderingOff = hidden;
            hiddenByCapture = hidden;
        }

        private void OnDisable()
        {
            Captures.Remove(this);
            ResetCapture();
        }

        private void OnDestroy()
        {
            if (session != null) session.TrainingReset -= ResetCapture;
            ResetCapture();
        }
    }
}
