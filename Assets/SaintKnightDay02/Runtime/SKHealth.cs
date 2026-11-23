using System;
using UnityEngine;

namespace SaintKnight.Day02
{
    public enum SKTeam { Player, Enemy }

    /// <summary>Authoritative health, damage immunity and brief hit reaction.</summary>
    public sealed class SKHealth : MonoBehaviour
    {
        [SerializeField] private SKTeam team;
        [SerializeField, Min(1f)] private float maximumHp = 100f;
        [SerializeField, Min(0f)] private float invulnerabilitySeconds = .55f;
        [SerializeField, Min(0f)] private float hitStunSeconds = .16f;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [Tooltip("Day 15: HP regained per second once nothing has hurt this body for the delay. 0 = no regeneration.")]
        [SerializeField, Min(0f)] private float regenPerSecond;
        [SerializeField, Min(0f)] private float regenDelaySeconds = 3f;
        private float lastHarmAt = float.NegativeInfinity;
        private SKGrabState grabState;
        private float immuneUntil;
        private float stunnedUntil;
        private Color originalColor;

        public event Action Changed;
        public event Action Damaged;
        public float CurrentHp { get; private set; }
        public float MaximumHp => maximumHp;
        /// <summary>HP-based appearance only; independent of combat and future mental states.</summary>
        public int DamageStage => DamageStageFor(CurrentHp, maximumHp);
        public SKTeam Team => team;
        public bool IsDead => CurrentHp <= 0f;
        public bool IsStunned => !IsDead && Time.time < stunnedUntil;
        public bool IsInvulnerable => Time.time < immuneUntil;
        /// <summary>World position of the most recent accepted damage source.</summary>
        public Vector2 LastDamageSource { get; private set; }
        /// <summary>Day 15: who dealt the most recent damage or drain (picks the game-over cutscene). May be null.</summary>
        public UnityEngine.Object LastCause { get; private set; }
        /// <summary>Day 7 traits of the most recent accepted hit, read by the victim's AI in Damaged.</summary>
        public bool LastHitBreaksArmor { get; private set; }
        public float LastHitInterruptDelay { get; private set; }
        /// <summary>Multiplies accepted damage (boss shell). 1 = unchanged.</summary>
        public float DamageScale { get; set; } = 1f;

        /// <summary>
        /// Appearance damage 0~4 for a positive maximum HP: over 75%, over 50%, over 25%,
        /// above zero, then incapacitated. Uses unrounded HP; healing reverses the stages.
        /// </summary>
        public static int DamageStageFor(float currentHp, float maximumHp)
        {
            if (currentHp <= 0f) return 4;
            if (currentHp <= maximumHp * .25f) return 3;
            if (currentHp <= maximumHp * .5f) return 2;
            if (currentHp <= maximumHp * .75f) return 1;
            return 0;
        }

        private void Awake()
        {
            if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();
            originalColor = bodyRenderer != null ? bodyRenderer.color : Color.white;
            CurrentHp = maximumHp;
        }

        private void Update()
        {
            Regenerate();
            if (bodyRenderer == null) return;
            bodyRenderer.color = IsDead ? new Color(.35f, .35f, .35f, 1f) :
                IsStunned ? new Color(1f, .3f, .3f, 1f) :
                IsInvulnerable && Mathf.FloorToInt(Time.time * 18f) % 2 == 0
                    ? new Color(originalColor.r, originalColor.g, originalColor.b, .4f) : originalColor;
        }

        // Day 15: unhurt for a while and not restrained -> HP climbs back on the scaled game clock.
        private void Regenerate()
        {
            if (regenPerSecond <= 0f || IsDead || CurrentHp >= maximumHp ||
                Time.time - lastHarmAt < regenDelaySeconds) return;
            if (grabState == null) grabState = GetComponent<SKGrabState>();
            if (grabState != null && grabState.IsHeld) return;
            CurrentHp = Mathf.Min(maximumHp, CurrentHp + regenPerSecond * Time.deltaTime);
            Changed?.Invoke();
        }

        public bool TryTakeDamage(float damage, Vector2 sourcePosition, float knockback = 0f,
            bool breaksArmor = false, float interruptDelay = 0f, UnityEngine.Object cause = null)
        {
            if (!isActiveAndEnabled || damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage) ||
                IsDead || IsInvulnerable) return false;
            CurrentHp = Mathf.Max(0f, CurrentHp - damage * Mathf.Max(0f, DamageScale));
            LastDamageSource = sourcePosition;
            LastCause = cause;
            lastHarmAt = Time.time;
            LastHitBreaksArmor = breaksArmor;
            LastHitInterruptDelay = Mathf.Max(0f, interruptDelay);
            immuneUntil = Time.time + invulnerabilitySeconds;
            stunnedUntil = Time.time + hitStunSeconds;
            var body = GetComponent<Rigidbody2D>();
            if (body != null && body.bodyType == RigidbodyType2D.Dynamic && knockback > 0f)
            {
                float away = transform.position.x >= sourcePosition.x ? 1f : -1f;
                body.linearVelocity = new Vector2(away * knockback, 1.8f);
            }
            Damaged?.Invoke();
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Continuous loss while restrained: no hit-stun, no knockback and no immunity window,
        /// so a hold can drain in small readable steps. Returns the HP actually removed.
        /// </summary>
        public float Drain(float amount, UnityEngine.Object cause = null)
        {
            if (!isActiveAndEnabled || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount) || IsDead)
                return 0f;
            float before = CurrentHp;
            LastCause = cause;
            lastHarmAt = Time.time;
            CurrentHp = Mathf.Max(0f, CurrentHp - amount);
            Changed?.Invoke();
            return before - CurrentHp;
        }

        /// <summary>Extends (never shortens) the damage immunity window, e.g. after escaping a hold.</summary>
        public void GrantInvulnerability(float seconds)
        {
            if (seconds > 0f) immuneUntil = Mathf.Max(immuneUntil, Time.time + seconds);
        }

        // Preserves the Day 01 Q/E debug controls, now routed through actual health.
        public void AdjustForTraining(float amount)
        {
            if (amount < 0f) { TryTakeDamage(-amount, transform.position); return; }
            CurrentHp = Mathf.Clamp(CurrentHp + amount, 0f, maximumHp);
            Changed?.Invoke();
        }

        /// <summary>Day 20: HP carried over from the previous scene (at least 1, at most full).</summary>
        public void RestoreHp(float hp)
        {
            CurrentHp = Mathf.Clamp(hp, 1f, maximumHp);
            Changed?.Invoke();
        }

        public void ResetHealth()
        {
            CurrentHp = maximumHp;
            immuneUntil = stunnedUntil = 0f;
            lastHarmAt = float.NegativeInfinity;
            LastCause = null;
            if (bodyRenderer != null) bodyRenderer.color = originalColor;
            Changed?.Invoke();
        }
    }
}
