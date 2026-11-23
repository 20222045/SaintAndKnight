using System.Collections.Generic;
using SaintKnight.Day02;
using UnityEngine;

namespace SaintKnight.Day04
{
    // GrabWindup / Holding are appended so existing numeric values stay unchanged.
    public enum SKEnemyState { Disabled, Patrol, Chase, Return, Windup, Attacking, Recovery, Hurt, Dead, GrabWindup, Holding }

    /// <summary>Ground-only enemy: bounded patrol, sight, pursuit, return and telegraphed melee.</summary>
    [RequireComponent(typeof(SKHealth), typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class SKEnemyAgent : MonoBehaviour
    {
        [SerializeField] private string displayName = "ENEMY A";
        [SerializeField] private SKHealth player;
        [SerializeField] private SKHitbox hitbox;
        [SerializeField] private SpriteRenderer warning;
        [Header("Movement and perception")]
        [SerializeField, Min(0f)] private float patrolRadius = 1.2f;
        [SerializeField, Min(0f)] private float patrolSpeed = .7f;
        [SerializeField, Min(0f)] private float chaseSpeed = 1.5f;
        [SerializeField, Min(0f)] private float detectionRange = 3f;
        [SerializeField, Min(0f)] private float loseRange = 4.5f;
        [SerializeField, Min(0f)] private float leashRadius = 4f;
        [SerializeField, Min(0f)] private float verticalRange = 1.2f;
        [Header("Attack (temporary balance)")]
        [SerializeField] private bool attacksEnabled = true;
        [SerializeField, Min(0f)] private float attackRange = 1.1f;
        [SerializeField, Min(0f)] private float windupSeconds = .4f;
        [SerializeField, Min(0f)] private float recoverySeconds = 1f;
        [SerializeField, Min(.02f)] private float attackSeconds = .18f;
        [SerializeField, Min(0f)] private float damage = 15f;
        [SerializeField, Min(0f)] private float knockback = 2f;
        [SerializeField] private Vector2 attackSize = new Vector2(1f, .85f);
        [SerializeField, Min(0f)] private float attackOffset = .75f;
        [Tooltip("Vertical offset of the strike box from the body centre. Positive = a high swing a crouch passes under.")]
        [SerializeField] private float attackHeight;
        [Header("Hit reaction (defaults keep the Day 4 behaviour)")]
        [Tooltip("Delay before the next attack after being hit. Negative = use recoverySeconds. " +
            "A short value stops the player from stun-locking the enemy with repeated hits.")]
        [SerializeField] private float hurtRecoverySeconds = -1f;
        [Tooltip("Hits no longer cancel a committed strike windup/strike (heavy commitment).")]
        [SerializeField] private bool strikeArmored;
        [Tooltip("After taking a hit, the next strike windup ignores hits (a counter), so mashing at point blank " +
            "cannot kill the enemy before its tell resolves. Kill counts are unchanged.")]
        [SerializeField] private bool counterArmorAfterHit;
        [Header("Close-range grab (off by default; temporary balance)")]
        [Tooltip("Hits no longer cancel the grab tell: hugging and mashing attack gets grabbed.")]
        [SerializeField] private bool grabWindupArmored;
        [Tooltip("Player standing inside grabRange gets a short purple telegraph, then is restrained.")]
        [SerializeField] private bool grabEnabled;
        [SerializeField, Min(0f)] private float grabRange = .55f;
        [SerializeField, Min(0f)] private float grabWindupSeconds = .3f;
        [Tooltip("HP drained per tick while holding. No hit-stun or immunity is applied per tick.")]
        [SerializeField, Min(0f)] private float grabTickDamage = 4f;
        [SerializeField, Min(.05f)] private float grabTickSeconds = .5f;
        [Tooltip("If the player has not escaped by then, the enemy throws them for throw damage.")]
        [SerializeField, Min(.1f)] private float grabMaxHoldSeconds = 4f;
        [SerializeField, Min(0f)] private float grabThrowDamage = 12f;
        [SerializeField, Min(1f)] private float struggleThreshold = 6f;
        [SerializeField, Min(0f)] private float struggleDecayPerSecond = 2.5f;
        [Tooltip("Enemy recovery after the player breaks free (the punish window).")]
        [SerializeField, Min(0f)] private float escapeStaggerSeconds = 1.4f;
        [SerializeField, Min(0f)] private float escapeInvulnerabilitySeconds = .6f;
        private readonly List<RaycastHit2D> casts = new List<RaycastHit2D>();
        private SKGrabState heldGrab;
        private float holdStartedAt;
        private float nextDrainAt;
        private SKHealth health;
        private Rigidbody2D body;
        private BoxCollider2D solid;
        private SpriteRenderer visual;
        private Vector3 home;
        private float patrolDirection = 1f;
        private float phaseEndsAt;
        private float readyAt;
        private float attackFacing;
        private SKEnemyState phase = SKEnemyState.Patrol;
        private bool counterPending;
        private bool windupArmored;
        private float staggeredUntil;
        // Display-only signals (Day 9 sprites); combat never reads them.
        public float ArmorBrokenAt { get; private set; } = float.NegativeInfinity;
        public bool RecoveringFromGrabMiss { get; private set; }
        /// <summary>Day 18 display: a chain hit pushed the next attack back until this time.</summary>
        public float StaggeredUntil => staggeredUntil;
        public string DisplayName => displayName;
        public Vector3 Home => home;
        public SKHealth Health => health;
        public bool GrabEnabled
        {
            get => grabEnabled;
            set { grabEnabled = value; if (!value && heldGrab != null) Interrupt(); }
        }
        public bool IsHoldingPlayer => heldGrab != null && heldGrab.IsHeld && heldGrab.Holder == this;
        public bool AttacksEnabled
        {
            get => attacksEnabled;
            set { attacksEnabled = value; if (!value) Interrupt(); }
        }
        public SKEnemyState State
        {
            get
            {
                if (!isActiveAndEnabled || health == null || !health.isActiveAndEnabled) return SKEnemyState.Disabled;
                if (health.IsDead) return SKEnemyState.Dead;
                if (health.IsStunned) return SKEnemyState.Hurt;
                return phase;
            }
        }

        private void Awake()
        {
            health = GetComponent<SKHealth>();
            body = GetComponent<Rigidbody2D>();
            solid = GetComponent<BoxCollider2D>();
            visual = GetComponent<SpriteRenderer>();
            home = transform.position;
            if (hitbox == null) hitbox = GetComponentInChildren<SKHitbox>();
            body.freezeRotation = true;
        }

        private void Start()
        {
            // Body contact must not push actors; damage still goes through separate trigger Hurtboxes.
            if (player != null && player.TryGetComponent<Collider2D>(out var playerBody))
                Physics2D.IgnoreCollision(solid, playerBody);
            foreach (var other in FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None))
                if (other != this) Physics2D.IgnoreCollision(solid, other.GetComponent<Collider2D>());
        }

        private void OnEnable() { if (health != null) health.Damaged += OnDamaged; }
        private void OnDisable()
        {
            if (health != null) health.Damaged -= OnDamaged;
            Interrupt();
        }

        private void OnDamaged()
        {
            bool cutsArmor = health != null && health.LastHitBreaksArmor;
            if (health != null && !health.IsDead)
            {
                // Armored commitments keep running; the hit still deals damage and flashes.
                // A sword hit (Day 7) cuts built-in armor, never the post-hit counter.
                if (phase == SKEnemyState.GrabWindup && grabWindupArmored && !cutsArmor) return;
                if ((phase == SKEnemyState.Windup || phase == SKEnemyState.Attacking) &&
                    (windupArmored || (strikeArmored && !cutsArmor))) return;
            }
            bool cutTell = phase == SKEnemyState.Windup || phase == SKEnemyState.Attacking ||
                phase == SKEnemyState.GrabWindup;
            // Reaching here with armor-breaking hit on an armored commitment means the sword cut it.
            if (cutsArmor && ((phase == SKEnemyState.GrabWindup && grabWindupArmored) ||
                ((phase == SKEnemyState.Windup || phase == SKEnemyState.Attacking) && strikeArmored)))
                ArmorBrokenAt = Time.time;
            bool staggered = Time.time < staggeredUntil;
            Interrupt();
            if (hurtRecoverySeconds >= 0f) readyAt = Time.time + hurtRecoverySeconds;
            // Chain hit (Day 7) on a tell pushes the next attack back by a fixed amount. Hits during that
            // wait neither shorten nor extend it, so cut-then-mash still meets the counter.
            if (staggered) readyAt = staggeredUntil;
            else if (cutTell && health != null && health.LastHitInterruptDelay > 0f)
                readyAt = staggeredUntil = readyAt + health.LastHitInterruptDelay;
            counterPending = counterArmorAfterHit;
        }

        private void FixedUpdate()
        {
            if (State == SKEnemyState.Dead || State == SKEnemyState.Disabled ||
                player == null || !player.isActiveAndEnabled || player.IsDead)
            {
                Interrupt();
                Stop();
                return;
            }
            if (State == SKEnemyState.Hurt) return; // Preserve any knockback during hit reaction.
            if (SKHitStop.IsActive) return;
            if (phase == SKEnemyState.GrabWindup)
            {
                Stop();
                if (Time.time < phaseEndsAt) return;
                SetWarning(false);
                TryStartHold();
                return;
            }
            if (phase == SKEnemyState.Holding)
            {
                UpdateHold();
                return;
            }
            if (phase == SKEnemyState.Windup)
            {
                Stop();
                if (Time.time < phaseEndsAt) return;
                SetWarning(false);
                // A wall or a target moving behind us causes a miss, not a late retarget.
                if (HasSight())
                    hitbox.Begin(attackFacing, attackSize, attackOffset, damage, attackSeconds,
                        knockback, new Color(1f, .2f, .15f, .5f), attackHeight);
                phase = SKEnemyState.Attacking;
                phaseEndsAt = Time.time + Mathf.Max(Time.fixedDeltaTime, attackSeconds);
                return;
            }
            if (phase == SKEnemyState.Attacking)
            {
                Stop();
                if (hitbox.IsActive || Time.time < phaseEndsAt) return;
                phase = SKEnemyState.Recovery;
                readyAt = Time.time + recoverySeconds;
                return;
            }
            if (phase == SKEnemyState.Recovery)
            {
                Stop();
                if (Time.time < readyAt) return;
                phase = SKEnemyState.Patrol;
                RecoveringFromGrabMiss = false;
            }

            Vector2 distance = player.transform.position - transform.position;
            bool returning = phase == SKEnemyState.Return;
            bool leashed = Mathf.Abs(transform.position.x - home.x) >= leashRadius;
            bool seen = Mathf.Abs(distance.y) <= verticalRange && HasSight();
            bool tracking = phase == SKEnemyState.Chase;
            float range = tracking ? Mathf.Max(detectionRange, loseRange) : detectionRange;
            if (!returning && !leashed && seen && Mathf.Abs(distance.x) <= range)
            {
                phase = SKEnemyState.Chase;
                // Hugging the enemy is answered with a grab rather than the normal strike.
                if (grabEnabled && attacksEnabled && Time.time >= readyAt &&
                    Mathf.Abs(distance.x) <= grabRange && CanGrabPlayer())
                {
                    Stop();
                    Face(distance.x);
                    attackFacing = distance.x >= 0f ? 1f : -1f;
                    phase = SKEnemyState.GrabWindup;
                    phaseEndsAt = Time.time + grabWindupSeconds;
                    SetWarning(true, true);
                    return;
                }
                if (Mathf.Abs(distance.x) <= attackRange)
                {
                    Stop();
                    Face(distance.x);
                    if (attacksEnabled && hitbox != null && Time.time >= readyAt)
                    {
                        attackFacing = distance.x >= 0f ? 1f : -1f;
                        phase = SKEnemyState.Windup;
                        phaseEndsAt = Time.time + windupSeconds;
                        windupArmored = counterPending;
                        counterPending = false;
                        SetWarning(true);
                    }
                    return;
                }
                Move(Mathf.Sign(distance.x), chaseSpeed);
                return;
            }
            if (returning || tracking || leashed)
            {
                phase = SKEnemyState.Return;
                float delta = home.x - transform.position.x;
                if (Mathf.Abs(delta) > .08f) { Move(Mathf.Sign(delta), patrolSpeed); return; }
                phase = SKEnemyState.Patrol;
                Stop();
                return;
            }

            phase = SKEnemyState.Patrol;
            if (transform.position.x >= home.x + patrolRadius) patrolDirection = -1f;
            else if (transform.position.x <= home.x - patrolRadius) patrolDirection = 1f;
            if (!Move(patrolDirection, patrolSpeed)) patrolDirection = -patrolDirection;
        }

        private bool HasSight()
        {
            Vector2 delta = player.transform.position - transform.position;
            foreach (var hit in Physics2D.RaycastAll(transform.position, delta.normalized, delta.magnitude))
                if (IsTerrain(hit.collider)) return false;
            return true;
        }

        private static bool IsTerrain(Collider2D collider) => collider != null && !collider.isTrigger &&
            collider.GetComponentInParent<SKHealth>() == null;

        private bool Move(float direction, float speed)
        {
            Face(direction);
            float advance = speed * Time.fixedDeltaTime;
            var filter = new ContactFilter2D { useTriggers = false };
            solid.Cast(new Vector2(direction, 0f), filter, casts, advance + .04f);
            foreach (var hit in casts)
                if (IsTerrain(hit.collider) && Mathf.Abs(hit.normal.x) > .5f) { Stop(); return false; }
            Vector2 probe = new Vector2(solid.bounds.center.x + direction * (solid.bounds.extents.x + advance + .08f),
                solid.bounds.min.y + .12f);
            bool groundAhead = false;
            foreach (var hit in Physics2D.RaycastAll(probe, Vector2.down, .32f))
                if (IsTerrain(hit.collider)) { groundAhead = true; break; }
            if (!groundAhead) { Stop(); return false; }
            body.linearVelocity = new Vector2(direction * speed, body.linearVelocity.y);
            return true;
        }

        private SKGrabState PlayerGrab()
        {
            var combat = player != null ? player.GetComponent<SKPlayerCombat>() : null;
            return combat != null ? combat.Grab : player != null ? player.GetComponent<SKGrabState>() : null;
        }

        private bool CanGrabPlayer()
        {
            var grab = PlayerGrab();
            return grab != null && !grab.IsHeld && !player.IsDead && !player.IsInvulnerable;
        }

        private void TryStartHold()
        {
            Vector2 distance = player.transform.position - transform.position;
            var grab = PlayerGrab();
            // Jumping away, stepping back past the reach or still being immune makes the grab whiff.
            bool inReach = Mathf.Abs(distance.x) <= grabRange + .15f && Mathf.Abs(distance.y) <= .6f &&
                (Mathf.Abs(distance.x) < .2f || Mathf.Sign(distance.x) == attackFacing) && HasSight();
            if (grab != null && inReach && grab.TryBegin(this, struggleThreshold, struggleDecayPerSecond))
            {
                heldGrab = grab;
                phase = SKEnemyState.Holding;
                holdStartedAt = Time.time;
                nextDrainAt = Time.time + grabTickSeconds;
                PinPlayer();
                return;
            }
            phase = SKEnemyState.Recovery;
            RecoveringFromGrabMiss = true;
            readyAt = Time.time + recoverySeconds;
        }

        private void UpdateHold()
        {
            Stop();
            if (!IsHoldingPlayer || player.IsDead || !player.isActiveAndEnabled)
            {
                // Released elsewhere (reset, death): end without punishing either side again.
                ReleaseHold();
                phase = SKEnemyState.Recovery;
                readyAt = Time.time + recoverySeconds;
                return;
            }
            PinPlayer();
            if (heldGrab.EscapeReady)
            {
                ReleaseHold(escaped: true);
                player.GrantInvulnerability(escapeInvulnerabilitySeconds);
                var pushed = player.GetComponent<Rigidbody2D>();
                if (pushed != null) pushed.linearVelocity = new Vector2(attackFacing * 3f, 2f);
                phase = SKEnemyState.Recovery;
                readyAt = Time.time + escapeStaggerSeconds;
                return;
            }
            if (Time.time >= holdStartedAt + grabMaxHoldSeconds)
            {
                ReleaseHold();
                // The throw is a normal accepted hit: stun, knockback, immunity and hit-stop.
                if (player.TryTakeDamage(grabThrowDamage, transform.position, knockback * 1.5f, cause: this))
                    SKHitStop.Request(.045f, gameObject.scene);
                phase = SKEnemyState.Recovery;
                readyAt = Time.time + recoverySeconds;
                return;
            }
            while (Time.time >= nextDrainAt && !player.IsDead)
            {
                player.Drain(grabTickDamage, this);
                nextDrainAt += grabTickSeconds;
            }
        }

        private void PinPlayer()
        {
            var held = player.GetComponent<Rigidbody2D>();
            Vector2 at = (Vector2)transform.position + new Vector2(attackFacing * .3f, 0f);
            if (held != null)
            {
                held.position = at;
                held.linearVelocity = Vector2.zero;
            }
            player.transform.position = new Vector3(at.x, at.y, player.transform.position.z);
        }

        private void ReleaseHold(bool escaped = false)
        {
            if (heldGrab != null) heldGrab.Release(this, escaped);
            heldGrab = null;
        }

        private void Face(float direction) { if (visual != null && direction != 0f) visual.flipX = direction < 0f; }
        private void Stop() { if (body != null) body.linearVelocity = new Vector2(0f, body.linearVelocity.y); }
        private void SetWarning(bool show, bool grab = false)
        {
            if (warning == null) return;
            warning.enabled = show;
            if (!show) return;
            // Grab tell is short, close and purple; the strike tell keeps the committed hitbox shape.
            warning.transform.localPosition = new Vector3(attackFacing * (grab ? grabRange * .5f : attackOffset),
                grab ? 0f : attackHeight, 0f);
            warning.size = grab ? new Vector2(grabRange + .3f, .95f) : attackSize;
            if (!warningColorKnown) { strikeWarningColor = warning.color; warningColorKnown = true; }
            warning.color = grab ? new Color(.65f, .25f, 1f, strikeWarningColor.a) : strikeWarningColor;
        }
        private Color strikeWarningColor;
        private bool warningColorKnown;
        private void Interrupt()
        {
            SetWarning(false);
            ReleaseHold();
            windupArmored = false;
            RecoveringFromGrabMiss = false;
            if (hitbox != null) hitbox.Cancel();
            phase = SKEnemyState.Recovery;
            readyAt = Time.time + recoverySeconds;
            Stop();
        }

        public void ResetAgent()
        {
            Interrupt();
            counterPending = false;
            staggeredUntil = 0f;
            ArmorBrokenAt = float.NegativeInfinity;
            transform.position = home;
            body.linearVelocity = Vector2.zero;
            patrolDirection = 1f;
            readyAt = Time.time + .75f;
            phase = SKEnemyState.Recovery;
        }
    }
}
