using UnityEngine;

namespace SaintKnight.Day02
{
    /// <summary>
    /// Player-side restraint state. An enemy owns the hold timing; this component only tracks
    /// whether the player is held and how far the struggle meter has filled.
    /// Input is still read by SKDay01Player (single input owner) and forwarded here.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SKGrabState : MonoBehaviour
    {
        private SKHealth health;
        private float threshold = 1f;
        private float decayPerSecond;
        private float meter;

        public bool IsHeld { get; private set; }
        public Object Holder { get; private set; }
        public float StruggleProgress => IsHeld ? Mathf.Clamp01(meter / threshold) : 0f;
        public bool EscapeReady => IsHeld && meter >= threshold;
        public float HeldSince { get; private set; } = float.NegativeInfinity;
        public float LastStruggleAt { get; private set; } = float.NegativeInfinity;
        public float EscapedAt { get; private set; } = float.NegativeInfinity;

        private void Awake() => health = GetComponent<SKHealth>();

        public bool TryBegin(Object holder, float struggleThreshold, float struggleDecayPerSecond,
            bool allowInvulnerabilityFromAcceptedHit = false)
        {
            if (IsHeld || holder == null || !isActiveAndEnabled || SKHitStop.IsActive) return false;
            if (health != null && (!health.isActiveAndEnabled || health.IsDead ||
                (health.IsInvulnerable && !allowInvulnerabilityFromAcceptedHit))) return false;
            IsHeld = true;
            Holder = holder;
            HeldSince = Time.time;
            LastStruggleAt = EscapedAt = float.NegativeInfinity;
            threshold = Mathf.Max(1f, struggleThreshold);
            decayPerSecond = Mathf.Max(0f, struggleDecayPerSecond);
            meter = 0f;
            var combat = GetComponent<SKPlayerCombat>();
            if (combat != null) combat.CancelAttack();
            var body = GetComponent<Rigidbody2D>();
            if (body != null) body.linearVelocity = Vector2.zero;
            return true;
        }

        /// <summary>One discrete mash input (attack, jump or a new direction press).</summary>
        public void RegisterStruggle()
        {
            if (!IsHeld || SKHitStop.IsActive) return;
            meter += 1f;
            LastStruggleAt = Time.time;
        }

        private void Update()
        {
            if (!IsHeld) return;
            if (health != null && (health.IsDead || !health.isActiveAndEnabled)) { meter = 0f; return; }
            if (!EscapeReady) meter = Mathf.Max(0f, meter - decayPerSecond * Time.deltaTime);
        }

        public void Release(Object holder, bool escaped = false)
        {
            if (!IsHeld || (holder != null && holder != Holder)) return;
            ForceRelease();
            if (escaped) EscapedAt = Time.time;
        }

        public void ForceRelease()
        {
            IsHeld = false;
            Holder = null;
            meter = 0f;
            HeldSince = LastStruggleAt = EscapedAt = float.NegativeInfinity;
        }

        private void OnDisable() => ForceRelease();
    }
}
