using System;
using UnityEngine;
using SaintKnight.Day01;

namespace SaintKnight.Day02
{
    [RequireComponent(typeof(SKHealth), typeof(SKDay01Player))]
    public sealed class SKPlayerCombat : MonoBehaviour
    {
        [Serializable]
        public sealed class AttackProfile
        {
            public float damage = 20f;
            public Vector2 size = new Vector2(.95f, .8f);
            public float offset = .72f;
            [Min(0f)] public float windup;
            public float duration = .18f;
            public float cooldown = .45f;
            [Min(0f)] public float knockback;
            [Tooltip("Day 7 sword trait: cuts through the enemy's built-in armor (heavy swing, grab tell). " +
                "The post-hit counter armor still holds.")]
            public bool breaksArmor;
            [Tooltip("Day 7 chain trait: seconds added to the enemy's next attack when this hit cancels its tell.")]
            [Min(0f)] public float interruptDelay;
            public Color color = new Color(1f, .85f, .45f, .45f);
        }

        [SerializeField] private SKHitbox hitbox;
        [SerializeField] private AttackProfile cecilia = new AttackProfile();
        [SerializeField] private AttackProfile adela = new AttackProfile
        {
            damage = 25f, size = new Vector2(1.1f, .85f), offset = .8f,
            duration = .2f, cooldown = .55f, color = new Color(.6f, .8f, 1f, .45f)
        };
        private SKHealth health;
        private SKDay01Player player;
        private SKGrabState grab;
        private float readyAt;
        private int cancelledFrame = -1;
        private AttackProfile pendingAttack;
        private float strikeAt;
        private float strikeStartedAt;
        private float activeDuration;
        public float AttackFacing { get; private set; } = 1f;
        public int AttackCancelVersion { get; private set; }
        public bool IsWindingUp => pendingAttack != null;
        public bool LocksFacing => IsWindingUp || IsAttacking;
        public bool IsAttacking => hitbox != null && hitbox.IsActive;
        // Scaled progress for the sprite view. The hitbox still owns the end of the active phase.
        public float WindupProgress => pendingAttack != null && pendingAttack.windup > 0f
            ? Mathf.Clamp01(1f - (strikeAt - Time.time) / pendingAttack.windup) : 0f;
        public float StrikeProgress => IsAttacking && activeDuration > 0f
            ? Mathf.Clamp01((Time.time - strikeStartedAt) / activeDuration) : 0f;
        public SKCombatState State
        {
            get
            {
                if (!isActiveAndEnabled || health == null || !health.isActiveAndEnabled ||
                    player == null || !player.isActiveAndEnabled) return SKCombatState.Disabled;
                if (health.IsDead) return SKCombatState.Dead;
                if (grab != null && grab.IsHeld) return SKCombatState.Held;
                if (health.IsStunned) return SKCombatState.Hurt;
                if (IsWindingUp) return SKCombatState.Windup;
                if (IsAttacking) return SKCombatState.Attacking;
                return Time.time < readyAt ? SKCombatState.Recovery : SKCombatState.Ready;
            }
        }
        public bool CanAct => State != SKCombatState.Disabled && State != SKCombatState.Dead &&
            State != SKCombatState.Hurt && State != SKCombatState.Held && !SKHitStop.IsActive;
        public SKGrabState Grab => grab;

        private void Awake()
        {
            health = GetComponent<SKHealth>();
            player = GetComponent<SKDay01Player>();
            // Inert unless an enemy restrains the player; added at runtime so older scenes need no edits.
            if (!TryGetComponent(out grab)) grab = gameObject.AddComponent<SKGrabState>();
            if (hitbox == null) hitbox = GetComponentInChildren<SKHitbox>();
        }

        private void OnEnable()
        {
            if (health != null) health.Damaged += CancelAttack;
        }

        private void OnDisable()
        {
            if (health != null) health.Damaged -= CancelAttack;
            CancelAttack();
        }

        private void Update()
        {
            if (!LocksFacing) return;
            if (State == SKCombatState.Disabled || health.IsDead || health.IsStunned ||
                (grab != null && grab.IsHeld))
            {
                CancelAttack();
                return;
            }
            if (IsWindingUp && !SKHitStop.IsActive && Time.time >= strikeAt) StartStrike();
        }

        public bool TryAttack()
        {
            if (!CanAct || State != SKCombatState.Ready || hitbox == null || cancelledFrame == Time.frameCount)
                return false;
            var attack = player.SelectedCharacter == 0 ? cecilia : adela;
            if (!hitbox.isActiveAndEnabled) return false;
            activeDuration = Mathf.Max(Time.fixedDeltaTime, attack.duration);
            float windup = Mathf.Max(0f, attack.windup);
            AttackFacing = player.Facing;
            strikeAt = Time.time + windup;
            // Cooldown is measured from input, never shortened below preparation + strike.
            readyAt = Time.time + Mathf.Max(windup + activeDuration, attack.cooldown);
            pendingAttack = attack;
            if (windup > 0f)
                hitbox.ShowWindup(AttackFacing, attack.size, attack.offset, attack.color);
            else StartStrike();
            return IsWindingUp || IsAttacking;
        }

        private void StartStrike()
        {
            var attack = pendingAttack;
            pendingAttack = null;
            if (attack == null) return;
            // If Update ran late, still preserve the complete active interval and one-hit rule.
            strikeStartedAt = Time.time;
            readyAt = Mathf.Max(readyAt, Time.time + activeDuration);
            hitbox.Begin(AttackFacing, attack.size, attack.offset, attack.damage,
                activeDuration, attack.knockback, attack.color, 0f, attack.breaksArmor, attack.interruptDelay);
            if (!hitbox.IsActive) hitbox.Cancel();
        }

        public void CancelAttack()
        {
            pendingAttack = null;
            if (hitbox != null) hitbox.Cancel();
            cancelledFrame = Time.frameCount;
            AttackCancelVersion++;
        }

        public void ResetCombat()
        {
            if (grab != null) grab.ForceRelease();
            CancelAttack();
            readyAt = 0f;
        }
    }
}
