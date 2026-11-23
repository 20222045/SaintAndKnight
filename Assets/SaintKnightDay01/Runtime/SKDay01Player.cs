using UnityEngine;
using SaintKnight.Day02;
using SaintKnight.Day26;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SaintKnight.Day01
{
    /// <summary>Day 01 playable 2D movement prototype (not the final combat controller).</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public sealed class SKDay01Player : MonoBehaviour
    {
        [SerializeField] private Sprite ceciliaTemporarySprite;
        [SerializeField] private Sprite adelaTemporarySprite;
        [SerializeField] private float moveSpeed = 3.6f;
        [SerializeField] private float jumpVelocity = 7.0f;
        [SerializeField] private SKDay01HUD hud;
        [Header("Feel (1 / 1 / off keeps the Day 01-04 behaviour)")]
        [Tooltip("Upward speed kept when Jump is released early. 1 = fixed-height jump.")]
        [SerializeField, Range(0f, 1f)] private float jumpReleaseVelocityScale = 1f;
        [Tooltip("Horizontal speed multiplier while airborne. 1 = full air control.")]
        [SerializeField, Range(0f, 1f)] private float airSpeedMultiplier = 1f;
        [Tooltip("Turn toward the source of accepted damage so the sprite faces the threat.")]
        [SerializeField] private bool faceAttackerOnHit;
        [Tooltip("Jump may start during an attack's windup/strike. Off = a ground attack commits (reference rule).")]
        [SerializeField] private bool jumpDuringAttack = true;
        [Tooltip("Hold S/Down on the ground to crouch: no walking, lower body and hurtbox. 0 = no crouch.")]
        [SerializeField, Min(0f)] private float crouchHeight;

        private Rigidbody2D rb;
        private BoxCollider2D ownCollider;
        private SpriteRenderer display;
        private float direction;
        private int selected; // 0 = Cecilia, 1 = Adela
        private bool grounded;
        private SKPlayerCombat combat;
        private SKHealth health;
        private bool jumpRising;
        private BoxCollider2D hurtArea;
        private Vector2 standSize, standOffset, hurtStandSize, hurtStandOffset;

        public int SelectedCharacter => selected;
        public float Facing { get; private set; } = 1f;
        public bool IsGrounded => grounded;
        public bool IsCrouching { get; private set; }
        /// <summary>Ground hazards (slime) scale walking speed. 1 = unchanged.</summary>
        public float SpeedMultiplier { get; set; } = 1f;
        public float JumpReleaseVelocityScale => jumpReleaseVelocityScale;
        public float AirSpeedMultiplier => airSpeedMultiplier;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            ownCollider = GetComponent<BoxCollider2D>();
            display = GetComponent<SpriteRenderer>();
            combat = GetComponent<SKPlayerCombat>();
            health = GetComponent<SKHealth>();
            standSize = ownCollider.size;
            standOffset = ownCollider.offset;
            var hurtbox = GetComponentInChildren<SKHurtbox>();
            if (hurtbox != null && hurtbox.TryGetComponent(out hurtArea))
            {
                hurtStandSize = hurtArea.size;
                hurtStandOffset = hurtArea.offset;
            }
            if (hud != null && health != null) hud.BindHealth(health);
            rb.gravityScale = 2.2f;
            rb.freezeRotation = true;
            ApplySelection();
        }

        private void Update()
        {
            if (SaintKnight.Day26.SKPause.IsPaused) { direction = 0f; return; }   // Day 26 menu owns the keys
            // A single input owner makes Tab+J deterministic, independent of script Update order.
            // Q/E debug HP stays in the editor and development builds only (Day 29: not in the trial build).
            if (!SKHitStop.IsActive && hud != null && (Application.isEditor || Debug.isDebugBuild))
            {
                if (WasDamagePressed()) hud.AdjustHp(-25f);
                if (WasHealPressed()) hud.AdjustHp(25f);
            }
            direction = ReadDirection();
            var grab = combat != null ? combat.Grab : null;
            if (grab != null && grab.IsHeld)
            {
                // Held: every discrete press (attack, jump or a new direction) fills the struggle meter.
                direction = 0f;
                jumpRising = false;
                SetCrouch(false);
                if (WasAttackPressed()) grab.RegisterStruggle();
                if (WasJumpPressed()) grab.RegisterStruggle();
                if (WasDirectionPressed()) grab.RegisterStruggle();
                return;
            }
            bool canAct = combat != null ? combat.CanAct :
                health == null || (health.isActiveAndEnabled && !health.IsDead && !health.IsStunned);
            if (!canAct) direction = 0f;
            // Sample facing before J so direction+attack in one frame uses the new direction.
            // Movement remains free while a committed attack keeps its visual facing.
            UpdateFacing();
            grounded = CheckGrounded();
            bool attackCommitted = !jumpDuringAttack && combat != null && combat.LocksFacing;
            if (WasJumpPressed() && grounded && canAct && !attackCommitted)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVelocity);
                jumpRising = true;
            }
            else if (jumpRising)
            {
                // Variable height: releasing Jump while rising keeps only part of the upward speed.
                if (rb.linearVelocity.y <= 0f) jumpRising = false;
                else if (!IsJumpHeld())
                {
                    if (jumpReleaseVelocityScale < 1f)
                        rb.linearVelocity = new Vector2(rb.linearVelocity.x,
                            rb.linearVelocity.y * jumpReleaseVelocityScale);
                    jumpRising = false;
                }
            }
            if (WasSwitchPressed() && canAct) SwitchCharacter();
            if (WasAttackPressed() && canAct && combat != null) combat.TryAttack();
            // Attacking or jumping stands up; crouch resumes while the key is still held.
            SetCrouch(crouchHeight > 0f && canAct && grounded && !jumpRising && IsCrouchHeld() &&
                (combat == null || !combat.LocksFacing));
        }

        private void SetCrouch(bool crouch)
        {
            if (crouch == IsCrouching) return;
            IsCrouching = crouch;
            // Keep the feet where they are: only the top of the body and hurtbox comes down.
            // ponytail: no ceiling check on stand-up; add one if low passages appear.
            float drop = crouch ? standSize.y - crouchHeight : 0f;
            ownCollider.size = new Vector2(standSize.x, standSize.y - drop);
            ownCollider.offset = standOffset - new Vector2(0f, drop * .5f);
            if (hurtArea == null) return;
            hurtArea.size = new Vector2(hurtStandSize.x, hurtStandSize.y - drop);
            hurtArea.offset = hurtStandOffset - new Vector2(0f, drop * .5f);
        }

        private void FixedUpdate()
        {
            if ((health != null && (!health.isActiveAndEnabled || health.IsDead)) ||
                (combat != null && combat.State == SKCombatState.Disabled))
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                return;
            }
            if (health != null && health.IsStunned) return; // Allow the short knockback to finish.
            if (combat != null && !combat.CanAct) return;
            float speed = (IsCrouching ? 0f : grounded ? moveSpeed : moveSpeed * airSpeedMultiplier) * SpeedMultiplier;
            rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);
            UpdateFacing();
        }

        private void OnEnable()
        {
            if (health != null) health.Damaged += FaceDamageSource;
        }

        private void FaceDamageSource()
        {
            if (!faceAttackerOnHit || health == null || (combat != null && combat.LocksFacing)) return;
            float dx = health.LastDamageSource.x - transform.position.x;
            if (Mathf.Abs(dx) < .01f) return; // Debug damage uses our own position.
            Facing = dx < 0f ? -1f : 1f;
            display.flipX = Facing < 0f;
        }

        private void UpdateFacing()
        {
            if (combat != null && combat.LocksFacing)
            {
                Facing = combat.AttackFacing;
                display.flipX = Facing < 0f;
            }
            else if (direction != 0f)
            {
                Facing = direction < 0f ? -1f : 1f;
                display.flipX = Facing < 0f;
            }
        }

        private void OnDisable()
        {
            if (health != null) health.Damaged -= FaceDamageSource;
            direction = 0f;
            jumpRising = false;
            SetCrouch(false);
            if (combat != null) combat.CancelAttack();
        }

        public void SwitchCharacter()
        {
            if (!isActiveAndEnabled || (combat != null && !combat.CanAct)) return;
            if (health != null && (health.IsDead || health.IsStunned)) return;
            if (combat != null) combat.CancelAttack();
            selected = 1 - selected;
            ApplySelection();
        }

        /// <summary>Day 20: puts the carried / saved character in place when a scene starts (no cooldown checks).</summary>
        public void SelectCharacter(int index)
        {
            index = index == 1 ? 1 : 0;
            if (index == selected) return;
            if (combat != null) combat.CancelAttack();
            selected = index;
            ApplySelection();
        }

        private void ApplySelection()
        {
            if (display == null) return;
            display.sprite = selected == 0 ? ceciliaTemporarySprite : adelaTemporarySprite;
            if (hud != null) hud.SetCharacter(selected);
        }

        private bool CheckGrounded()
        {
            var hits = Physics2D.OverlapBoxAll((Vector2)transform.position + Vector2.down * .54f,
                new Vector2(.38f, .10f), 0f);
            foreach (var hit in hits)
                if (hit != null && hit != ownCollider && !hit.isTrigger) return true;
            return false;
        }

        private static float ReadDirection()
        {
#if ENABLE_INPUT_SYSTEM
            float right = SKBindings.Held(SKAction.Right) ? 1f : 0f;
            float left = SKBindings.Held(SKAction.Left) ? 1f : 0f;
            return right - left;
#else
            return Input.GetAxisRaw("Horizontal");
#endif
        }

        private static bool WasJumpPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return SKBindings.Pressed(SKAction.Jump);
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }

        private static bool IsJumpHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return SKBindings.Held(SKAction.Jump);
#else
            return Input.GetKey(KeyCode.Space);
#endif
        }

        private static bool IsCrouchHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return SKBindings.Held(SKAction.Down);
#else
            return Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
#endif
        }

        private static bool WasDirectionPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return SKBindings.Pressed(SKAction.Left) || SKBindings.Pressed(SKAction.Right);
#else
            return Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) ||
                Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow);
#endif
        }

        private static bool WasAttackPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return SKBindings.Pressed(SKAction.Attack);
#else
            return Input.GetKeyDown(KeyCode.J);
#endif
        }

        private static bool WasSwitchPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return SKBindings.Pressed(SKAction.Switch);
#else
            return Input.GetKeyDown(KeyCode.Tab);
#endif
        }

        private static bool WasDamagePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Q);
#endif
        }

        private static bool WasHealPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.E);
#endif
        }
    }
}
