using UnityEngine;

namespace SaintKnight.Day02
{
    /// <summary>Stationary Day 02 target: telegraphs a short strike when the player is near.</summary>
    [RequireComponent(typeof(SKHealth))]
    public sealed class SKTrainingTarget : MonoBehaviour
    {
        [SerializeField] private SKHealth player;
        [SerializeField] private SKHitbox hitbox;
        [SerializeField] private SpriteRenderer warning;
        [SerializeField] private float detectionRange = 1.65f;
        [SerializeField] private float windupSeconds = .5f;
        [SerializeField] private float cooldownSeconds = 1.5f;
        private SKHealth health;
        private float readyAt;
        private float strikeAt;
        private float facing;
        private bool attacksEnabled = true;
        public bool IsWindingUp { get; private set; }
        public bool AttacksEnabled
        {
            get => attacksEnabled;
            set
            {
                if (attacksEnabled == value) return;
                attacksEnabled = value;
                if (!value) Interrupt();
            }
        }
        public SKCombatState State
        {
            get
            {
                if (!isActiveAndEnabled || health == null || !health.isActiveAndEnabled) return SKCombatState.Disabled;
                if (health.IsDead) return SKCombatState.Dead;
                if (health.IsStunned) return SKCombatState.Hurt;
                if (!AttacksEnabled) return SKCombatState.Disabled;
                if (IsWindingUp) return SKCombatState.Windup;
                if (hitbox != null && hitbox.IsActive) return SKCombatState.Attacking;
                return Time.time < readyAt ? SKCombatState.Recovery : SKCombatState.Ready;
            }
        }
        public string Status => State switch
        {
            SKCombatState.Dead => "DESTROYED - R TO RESET",
            SKCombatState.Windup => "STRIKE INCOMING!",
            SKCombatState.Attacking => "ATTACK",
            SKCombatState.Recovery => "RECOVERING",
            SKCombatState.Hurt => "HIT",
            SKCombatState.Disabled => "INACTIVE",
            _ => "READY"
        };

        private void Awake() => health = GetComponent<SKHealth>();
        private void OnEnable() { if (health != null) health.Damaged += Interrupt; }
        private void OnDisable()
        {
            if (health != null) health.Damaged -= Interrupt;
            Interrupt();
        }

        private void Update()
        {
            if (State == SKCombatState.Disabled || State == SKCombatState.Dead || State == SKCombatState.Hurt ||
                player == null || !player.isActiveAndEnabled || player.IsDead)
            {
                if (IsWindingUp || (hitbox != null && hitbox.IsActive)) Interrupt();
                return;
            }
            if (SKHitStop.IsActive) return;
            if (State == SKCombatState.Windup)
            {
                if (Time.time < strikeAt) return;
                IsWindingUp = false;
                if (warning != null) warning.enabled = false;
                hitbox.Begin(facing, new Vector2(1.1f, .85f), .8f, 15f, .22f, 3f,
                    new Color(1f, .2f, .15f, .5f));
                readyAt = Time.time + Mathf.Max(.22f, cooldownSeconds);
                return;
            }
            if (State != SKCombatState.Ready) return;
            var distance = player.transform.position - transform.position;
            if (Mathf.Abs(distance.x) > detectionRange || Mathf.Abs(distance.y) > 1.1f)
                return;
            facing = distance.x >= 0f ? 1f : -1f;
            IsWindingUp = true;
            strikeAt = Time.time + Mathf.Max(0f, windupSeconds);
            if (warning != null)
            {
                warning.transform.localPosition = new Vector3(facing * .8f, 0f, 0f);
                warning.enabled = true;
            }
        }

        private void Interrupt()
        {
            IsWindingUp = false;
            if (warning != null) warning.enabled = false;
            if (hitbox != null) hitbox.Cancel();
            readyAt = Time.time + Mathf.Max(0f, cooldownSeconds);
        }

        public void ResetCombat()
        {
            Interrupt();
            readyAt = Time.time + .75f;
        }
    }
}
