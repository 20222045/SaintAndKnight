using SaintKnight.Day01;
using SaintKnight.Day02;
using SaintKnight.Day04;
using UnityEngine;

namespace SaintKnight.Day10
{
    public enum SKFleshWallState { Idle, Tell, Holding, Rest, Consumed }

    /// <summary>
    /// Day 10 flesh wall: a mass on the back wall. A grounded player inside <c>reach</c> starts a purple tell;
    /// still inside when it ends = grabbed. Mash out before four seconds without a struggle press;
    /// otherwise the wall fully absorbs the player and closes until R / automatic retry.
    /// After an escape or miss it rests, so the answer is to bait the tell, step back, then pass.
    /// Running straight through is caught (reach 1.2 each side = 0.67 s at walking speed, tell 0.5 s).
    /// Scaled time; R / retry releases and resets it. Not damageable.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SKFleshWall : MonoBehaviour
    {
        private const float SwallowFrameSeconds = .10f, StruggleFrameSeconds = .18f, EscapeSeconds = .15f;
        private const int EscapeFrame = 4;

        [SerializeField] private SKHealth player;
        [SerializeField] private float reach = 1.2f, tell = .5f, rest = 2.5f, missRest = 1.5f;
        private const float ResistGrace = .35f;
        [Tooltip("Day 15: seconds an unresisted hold needs to eat a full HP bar. A struggle press pauses the loss briefly.")]
        [SerializeField, Min(.01f)] private float grabMaxHold = 4f;
        [SerializeField] private float struggleThreshold = 6f, struggleDecay = 2.5f, escapeImmunity = .6f;
        [Header("Drawing (display only)")]
        [SerializeField] private Sprite[] idle;
        [SerializeField] private Sprite reachSprite;
        [SerializeField] private SpriteRenderer warning, coil;
        [Tooltip("Frames in order: swallow (2), struggle loop (2), escape (1).")]
        [SerializeField] private Sprite[] ceciliaGrab, adelaGrab;

        private SpriteRenderer view, playerView;
        private SKGrabState grab;
        private SKDay01Player playerMove;
        private Rigidbody2D playerBody;
        private SKEnemyTrainingSession session;
        private float stateEnds, holdStartedAt;
        private float escapeUntil;
        private bool playerHiddenByWall;
        private Vector2 holdAt;
        private readonly SKTrapAnimationPlayback trapPlayback = new SKTrapAnimationPlayback();
        private readonly SaintKnight.SpriteCook.SKNativeDefeatSorting nativeSorting = new SaintKnight.SpriteCook.SKNativeDefeatSorting();
        private float escapedAt = float.NegativeInfinity, consumedAt = float.NegativeInfinity;
        private float displayedHoldSince = float.NegativeInfinity;
        private int displayedCharacter = -1;

        public SKFleshWallState State { get; private set; }
        public float Reach => reach;
        public bool IsHoldingPlayer => grab != null && grab.IsHeld && grab.Holder == this;
        public bool IsPlayerConsumed => State == SKFleshWallState.Consumed;
        /// <summary>Day 15: how far the body is swallowed = HP already lost (1 at HP 0).</summary>
        public float AbsorptionProgress => IsPlayerConsumed ? 1f : State == SKFleshWallState.Holding && IsHoldingPlayer
            ? Mathf.Clamp01(1f - player.CurrentHp / player.MaximumHp) : 0f;

        private void Awake() => view = GetComponent<SpriteRenderer>();

        private void Start()
        {
            // In Start: SKPlayerCombat adds the player's SKGrabState in its own Awake.
            if (player != null)
            {
                playerMove = player.GetComponent<SKDay01Player>();
                playerBody = player.GetComponent<Rigidbody2D>();
                playerView = player.GetComponent<SpriteRenderer>();
                var combat = player.GetComponent<SKPlayerCombat>();
                grab = combat != null ? combat.Grab : player.GetComponent<SKGrabState>();
            }
            session = FindFirstObjectByType<SKEnemyTrainingSession>();
            if (session != null) session.TrainingReset += ResetWall;
            ResetWall();
        }

        private void OnDestroy()
        {
            if (session != null) session.TrainingReset -= ResetWall;
        }

        private void OnDisable() => ResetWall();

        public void ResetWall()
        {
            Release();
            escapeUntil = 0f;
            escapedAt = consumedAt = float.NegativeInfinity;
            trapPlayback.Reset();
            displayedHoldSince = float.NegativeInfinity;
            displayedCharacter = -1;
            holdStartedAt = 0f;
            State = SKFleshWallState.Idle;
            SetPlayerHidden(false);
            if (view != null) RefreshVisual();
        }

        private bool InReach()
        {
            if (playerMove == null || !playerMove.IsGrounded) return false;
            Vector2 d = player.transform.position - transform.position;
            return Mathf.Abs(d.x) <= reach && d.y > -.2f && d.y < 1.2f;   // on this floor, not a ledge above
        }

        private void FixedUpdate()
        {
            if (SKHitStop.IsActive || player == null) return;
            if (!player.isActiveAndEnabled)
            {
                if (State != SKFleshWallState.Idle) ResetWall();
                return;
            }
            // Death caused by this hold (drain or another accepted hit) closes the wall too.
            // Keep the solo sprite hidden after releasing Grab until the existing retry resets the room.
            if (IsPlayerConsumed)
            {
                // Direct HP restoration (debug E / ResetHealth) may happen without TrainingReset.
                if (!player.IsDead) ResetWall();
                return;
            }
            if (player.IsDead)
            {
                if (State == SKFleshWallState.Holding && IsHoldingPlayer) ConsumePlayer();
                else if (State != SKFleshWallState.Idle) ResetWall();
                return;
            }
            switch (State)
            {
                case SKFleshWallState.Idle:
                    if (InReach() && !player.IsInvulnerable && grab != null && !grab.IsHeld)
                    {
                        State = SKFleshWallState.Tell;
                        stateEnds = Time.time + tell;
                    }
                    break;
                case SKFleshWallState.Tell:
                    if (Time.time < stateEnds) break;
                    if (InReach() && grab.TryBegin(this, struggleThreshold, struggleDecay))
                    {
                        State = SKFleshWallState.Holding;
                        holdAt = player.transform.position;
                        holdStartedAt = Time.time;
                        Pin();
                    }
                    else Rest(missRest);
                    break;
                case SKFleshWallState.Holding:
                    UpdateHold();
                    break;
                case SKFleshWallState.Rest:
                    if (Time.time >= stateEnds) State = SKFleshWallState.Idle;
                    break;
            }
        }

        private void UpdateHold()
        {
            if (!IsHoldingPlayer) { Rest(rest); return; }   // released elsewhere (reset / disable)
            Pin();
            float away = Mathf.Sign(player.transform.position.x - transform.position.x);
            if (away == 0f) away = 1f;
            if (grab.EscapeReady)
            {
                grab.Release(this, true);
                player.GrantInvulnerability(escapeImmunity);
                if (playerBody != null) playerBody.linearVelocity = new Vector2(away * 3f, 2f);
                Rest(rest);
                escapeUntil = Time.time + EscapeSeconds;
                escapedAt = Time.time;
                return;
            }
            if (Time.time - grab.LastStruggleAt >= ResistGrace)
                player.Drain(player.MaximumHp / Mathf.Max(.01f, grabMaxHold) * Time.fixedDeltaTime, this);
            if (player.IsDead) ConsumePlayer();
        }

        private void ConsumePlayer()
        {
            State = SKFleshWallState.Consumed;
            consumedAt = Time.time;
            escapedAt = float.NegativeInfinity;
            escapeUntil = 0f;
            player.Drain(player.CurrentHp, this);
            Release();
            SetPlayerHidden(true);
            if (view != null) RefreshVisual();
        }

        private void Rest(float seconds)
        {
            State = SKFleshWallState.Rest;
            stateEnds = Time.time + seconds;
        }

        private void Pin()
        {
            if (playerBody != null) { playerBody.position = holdAt; playerBody.linearVelocity = Vector2.zero; }
            player.transform.position = new Vector3(holdAt.x, holdAt.y, player.transform.position.z);
        }

        private void Release()
        {
            if (IsHoldingPlayer) grab.Release(this);
        }

        private void LateUpdate() => RefreshVisual();

        private void RefreshVisual()
        {
            bool holding = IsHoldingPlayer && player != null && player.isActiveAndEnabled && !player.IsDead;
            Sprite merged = GrabFrame(holding);
            if (merged != null)
            {
                view.sprite = merged;
                view.flipX = holdAt.x > transform.position.x;
            }
            else
            {
                bool reaching = State == SKFleshWallState.Tell || holding;
                view.sprite = reaching && reachSprite != null ? reachSprite
                    : idle != null && idle.Length > 0 ? idle[(int)Mathf.Repeat(Time.time / .5f, idle.Length)] : null;
                view.flipX = false;
            }
            var nativeActor = playerMove != null ? playerMove.GetComponent<SaintKnight.Day06.SKCharacterSpriteView>() : null;
            bool nativeHeld = holding && nativeActor != null && nativeActor.isActiveAndEnabled &&
                SaintKnight.SpriteCook.SKNativeDefeatLibrary.Load(playerMove.SelectedCharacter, "fleshwall") != null;
            nativeSorting.Apply(view, IsPlayerConsumed && nativeActor != null && nativeActor.isActiveAndEnabled && nativeActor.HasNativeDeathMotion);
            SetPlayerHidden(merged != null || IsPlayerConsumed || nativeHeld);
            if (warning != null) warning.enabled = State == SKFleshWallState.Tell;
            if (coil != null)
            {
                coil.enabled = holding && merged == null;
                if (coil.enabled) coil.transform.position = new Vector3(holdAt.x, transform.position.y, coil.transform.position.z);
            }
        }

        private Sprite GrabFrame(bool holding)
        {
            if (playerView == null || playerMove == null) return null;
            // The native scene now draws the actor and foreground at the player's true anchor.
            // Keep this wall's original idle mass; do not draw a second swallowed actor here.
            var nativeView = playerMove.GetComponent<SaintKnight.Day06.SKCharacterSpriteView>();
            if (nativeView != null && nativeView.isActiveAndEnabled && (holding || IsPlayerConsumed && nativeView.HasNativeDeathMotion) &&
                SaintKnight.SpriteCook.SKNativeDefeatLibrary.Load(playerMove.SelectedCharacter, "fleshwall") != null)
            {
                if (holding) { displayedHoldSince = holdStartedAt; displayedCharacter = playerMove.SelectedCharacter; }
                var nativeFrame = nativeView.Refresh();
                if (nativeFrame != null && nativeFrame.name.Contains("_native_fleshwall_")) return nativeFrame;
            }
            var combat = playerMove.GetComponent<SKPlayerCombat>();
            if (!holding && combat != null && (combat.State == SKCombatState.Windup ||
                combat.State == SKCombatState.Attacking || combat.State == SKCombatState.Hurt ||
                combat.State == SKCombatState.Disabled || combat.State == SKCombatState.Dead))
            {
                // A new action owns presentation immediately; an old escape must not hide it.
                escapedAt = float.NegativeInfinity;
                escapeUntil = 0f;
            }
            var library = SKTrapAnimationLibrary.Load(playerMove.SelectedCharacter, SKTrapMotionKind.Wall);
            if (library != null)
            {
                if (IsPlayerConsumed) return library.Sample(SKTrapMotionPhase.Consumed, 1f, Time.time - consumedAt);
                if (holding)
                {
                    if (displayedHoldSince != holdStartedAt || displayedCharacter != playerMove.SelectedCharacter)
                    {
                        trapPlayback.Reset();
                        displayedHoldSince = holdStartedAt;
                        displayedCharacter = playerMove.SelectedCharacter;
                    }
                    return trapPlayback.Held(library, AbsorptionProgress, grab.LastStruggleAt, Time.time);
                }
                if (displayedCharacter == playerMove.SelectedCharacter &&
                    Time.time - escapedAt < SKTrapAnimationLibrary.EscapeSeconds && !player.IsDead &&
                    player.isActiveAndEnabled && !grab.IsHeld)
                    return library.Sample(SKTrapMotionPhase.Escape, 0f, Time.time - escapedAt);
                return null;
            }
            var frames = playerMove.SelectedCharacter switch
            {
                0 => ceciliaGrab,
                1 => adelaGrab,
                _ => null,
            };
            if (frames == null || frames.Length <= EscapeFrame) return null;
            if (!holding)
                return Time.time < escapeUntil && !player.IsDead && player.isActiveAndEnabled && !grab.IsHeld
                    ? frames[EscapeFrame] : null;

            float elapsed = Time.time - holdStartedAt;
            float swallowSeconds = 2 * SwallowFrameSeconds;
            if (elapsed < swallowSeconds) return frames[(int)(elapsed / SwallowFrameSeconds)];
            int struggleFrame = (int)((elapsed - swallowSeconds) / StruggleFrameSeconds) % 2;
            return frames[2 + struggleFrame];
        }

        private void SetPlayerHidden(bool hidden)
        {
            if (playerView == null || playerHiddenByWall == hidden) return;
            // Leave SpriteRenderer.enabled to the monk's composite view.
            playerView.forceRenderingOff = hidden;
            playerHiddenByWall = hidden;
        }
    }
}
