using System;
using SaintKnight.Day01;
using SaintKnight.Day02;
using SaintKnight.Day09;
using SaintKnight.Day10;
using SaintKnight.Day15;
using SaintKnight.SpriteCook;
using UnityEngine;

namespace SaintKnight.Day06
{
    /// <summary>
    /// Day 6 display adapter: picks the player's pixel sprite from state it only reads
    /// (selected character, combat state, HP, movement). It owns no input, timing, damage or facing —
    /// SKDay01Player keeps flipX, SKPlayerCombat keeps attack timing, SKHealth keeps HP.
    /// No Animator / Animation Events, so nothing here can trigger or revive a hitbox.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SKCharacterSpriteView : MonoBehaviour
    {
        [Serializable]
        public sealed class SpriteSet
        {
            public Sprite idle, run, windup, attack, hit;
            [Tooltip("Idle pose for appearance damage stage 1, 2, 3 (stage 0 = idle).")]
            public Sprite[] damagedIdle = new Sprite[0];
            [Tooltip("Played once on death (HP 0 / INCAPACITATED), then held on the last frame.")]
            public Sprite[] collapse = new Sprite[0];
        }

        [SerializeField] private SpriteSet cecilia = new SpriteSet();
        [SerializeField] private SpriteSet adela = new SpriteSet();
        [SerializeField] private float collapseFramesPerSecond = 10f;
        [SerializeField] private float runSpeedThreshold = .1f;
        [Tooltip("Follow-through displayed after a completed strike, then idle/run for the rest of the cooldown.")]
        [SerializeField] private float followThroughSeconds = .1f;

        private SpriteRenderer display;
        private SKDay01Player player;
        private SKPlayerCombat combat;
        private SKHealth health;
        private Rigidbody2D body;
        private SKCharacterMotionLibrary ceciliaMotion;
        private SKCharacterMotionLibrary adelaMotion;
        private int previousCharacter = -1;
        private int previousCancelVersion;
        private SKCombatState previousState = SKCombatState.Disabled;
        private SKSpriteMotion? currentMotion;
        private float stateSince;
        private float motionSince;
        private float followThroughSince = float.NegativeInfinity;
        private float escapeInvalidatedAt = float.NegativeInfinity;
        private bool airborne;
        private bool spikeHurt;
        private float groundHeight;
        private float launchSpeed;
        private SKTrapEnvelopeView envelope;
        private SKEnvironmentalCapture dedicatedCapture;
        private int dedicatedCharacter = -1;
        private bool heldByBoss;
        private string deathScene;
        private SpriteRenderer hiddenKiller;
        private bool nativeDeathRecorded;
        private string nativeDeathKey, nativeBossFinish;
        private string nativeCommonForDeath;
        private Component nativeFatalSource;
        private SKBossAgent nativeDeathBoss;
        private SKBossDefeatCarryView bossCarry;
        private float nativeCommonSeconds=-1f;
        private float nativeDeathSince;
        private int nativeFirstFrame, nativeBossDeaths;
        private readonly SKNativeDefeatSorting nativeSorting = new SKNativeDefeatSorting();
        public string NativeSceneKey { get; private set; }
        public bool NativeFlipX { get; private set; }
        public bool IsBossCarryActive => bossCarry != null && bossCarry.IsActive;
        public Vector3 VisualPosition => IsBossCarryActive ? bossCarry.WorldPosition : transform.position;
        public SKBossDefeatCarryView BossCarry => bossCarry;
        public bool UsesNativeMotion => isActiveAndEnabled && display != null && display.sprite != null &&
            display.sprite.name.Contains("_native_") && (health != null && health.IsDead || combat != null && combat.Grab.IsHeld);
        public bool HasNativeDeathMotion
        {
            get { ObserveNativeDeath(); return health != null && health.IsDead && nativeDeathKey != null && NativeCauseAvailable(); }
        }
        private readonly SKTrapAnimationPlayback trapPlayback = new SKTrapAnimationPlayback();

        public Sprite Current => display != null ? display.sprite : null;

        private void Awake()
        {
            display = GetComponent<SpriteRenderer>();
            player = GetComponent<SKDay01Player>();
            combat = GetComponent<SKPlayerCombat>();
            health = GetComponent<SKHealth>();
            body = GetComponent<Rigidbody2D>();
            ceciliaMotion = SKCharacterMotionLibrary.Load("cecilia");
            adelaMotion = SKCharacterMotionLibrary.Load("adela");
            envelope = GetComponent<SKTrapEnvelopeView>();
            if (envelope == null) envelope = gameObject.AddComponent<SKTrapEnvelopeView>();
            bossCarry = GetComponent<SKBossDefeatCarryView>();
            if (bossCarry == null) bossCarry = gameObject.AddComponent<SKBossDefeatCarryView>();
        }

        private void OnEnable() { if (health != null) health.Changed += ObserveNativeDeath; }
        private void OnDestroy() { if (health != null) health.Changed -= ObserveNativeDeath; }

        private void OnDisable()
        {
            if (bossCarry != null) bossCarry.ResetVisual();
            nativeSorting.Apply(display, false);
            if (health != null) health.Changed -= ObserveNativeDeath;
            previousCharacter = -1;
            airborne = false;
            dedicatedCapture = null;
            trapPlayback.Reset();
            ShowKiller();
            var set = player != null && player.SelectedCharacter == 1 ? adela : cecilia;
            if (display != null && set.idle != null) display.sprite = set.idle;
            if (envelope != null) envelope.Hide();
        }

        // After SKDay01Player.Update (which may restore its temporary sprite on switch).
        private void LateUpdate() => Refresh();

        /// <summary>Applies the sprite for the current state now (also used by tests).</summary>
        public Sprite Refresh()
        {
            if (display == null) return null;
            nativeCommonSeconds=-1f;
            int character = player != null ? player.SelectedCharacter : 0;
            var sprite = character == 1 ? Pick(adela, adelaMotion, character) : Pick(cecilia, ceciliaMotion, character);
            if (sprite != null) display.sprite = sprite;
            if (UsesNativeMotion) display.flipX = NativeFlipX;
            if (UsesNativeMotion && health.IsDead) display.color = Color.white;
            nativeSorting.Apply(display, UsesNativeMotion && health.IsDead);
            if (bossCarry != null)
            {
                if (nativeCommonSeconds>=0f && nativeDeathBoss!=null && NativeCauseAvailable())
                    bossCarry.Refresh(display.sprite,nativeDeathBoss,character,NativeSceneKey,nativeCommonSeconds,NativeFlipX);
                else if (NativeSceneKey=="boss_grab" && health!=null && health.IsDead && nativeDeathBoss!=null && NativeCauseAvailable())
                    bossCarry.ShowGrabTether(display.sprite,nativeDeathBoss,character,Time.time-nativeDeathSince,NativeFlipX);
                else bossCarry.ResetVisual();
            }
            if (envelope != null) envelope.Refresh();
            return display.sprite;
        }

        private Sprite Pick(SpriteSet set, SKCharacterMotionLibrary motion, int character)
        {
            var state = combat != null ? combat.State : SKCombatState.Ready;
            if (health != null && health.IsDead) state = SKCombatState.Dead;
            TrackState(state, character);
            TrackAirborne();

            switch (state)
            {
                case SKCombatState.Disabled:
                    return set.idle;
                case SKCombatState.Dead:
                    var fallen = DeathPose(character, Time.time - stateSince);
                    if (fallen != null) return fallen;
                    if (set.collapse.Length == 0) return set.hit;
                    int frame = Mathf.Min((int)((Time.time - stateSince) * collapseFramesPerSecond), set.collapse.Length - 1);
                    return set.collapse[frame];
                case SKCombatState.Held:
                    heldByBoss = combat.Grab.Holder is SKBossAgent;
                    string heldKey = SKNativeDefeatLibrary.KeyFor(combat.Grab.Holder);
                    if (heldByBoss) heldKey = "boss_grab";
                    var nativeHeld = SKNativeDefeatLibrary.IsTrap(heldKey) ? SKNativeDefeatLibrary.LoadHeld(character, heldKey) : null;
                    if (nativeHeld != null)
                    {
                        if (combat.Grab.Holder is SKEnvironmentalCapture nativeCapture)
                        { dedicatedCapture = nativeCapture; dedicatedCharacter = character; }
                        NativeSceneKey = heldKey;
                        NativeFlipX = NativeFacing(heldKey, combat.Grab.Holder);
                        return nativeHeld.Held(1f - health.CurrentHp / health.MaximumHp, Time.time - combat.Grab.HeldSince,
                            Time.time - combat.Grab.LastStruggleAt < SKTrapAnimationLibrary.ResistSeconds);
                    }
                    if (heldByBoss)
                    {
                        // Lifted off the floor: limp while quiet, three struggling poses after a press.
                        bool resisting = Time.time - combat.Grab.LastStruggleAt < SKTrapAnimationLibrary.ResistSeconds;
                        var hoisted = SKAppliedSheets.Character(character, "boss_pattern",
                            resisting ? 5 + (int)(Time.time / .09f) % 3 : 4);
                        if (hoisted != null) { currentMotion = null; return hoisted; }
                    }
                    if (combat.Grab.Holder is SKEnvironmentalCapture dedicated)
                    {
                        if (dedicatedCapture != dedicated || dedicatedCharacter != character) trapPlayback.Reset();
                        dedicatedCapture = dedicated;
                        dedicatedCharacter = character;
                        var library = SKTrapAnimationLibrary.For(character, dedicated);
                        var composite = trapPlayback.Held(library, dedicated.AbsorptionProgress, combat.Grab.LastStruggleAt, Time.time);
                        if (composite != null) return composite;
                    }
                    if (combat.Grab.Holder is SKEnvironmentalCapture capture &&
                        capture.Kind == SKEnvironmentalCaptureKind.Tentacle &&
                        Time.time - combat.Grab.HeldSince < .16f)
                        return Pose(motion, SKSpriteMotion.SpikeHurt, set.hit, Time.time - combat.Grab.HeldSince);
                    return HeldPose(motion, set.hit);
                case SKCombatState.Hurt:
                    if (!spikeHurt && health != null && health.LastCause is Component hitter &&
                        hitter.GetComponentInParent<SKBossAgent>() != null)
                    {
                        // Boss sweep / throw: knocked off the feet instead of the ordinary flinch.
                        var swept = SKAppliedSheets.Character(character, "boss_pattern",
                            Mathf.Min((int)((Time.time - stateSince) / .08f), 3));
                        if (swept != null) { currentMotion = null; return swept; }
                    }
                    return Pose(motion, spikeHurt ? SKSpriteMotion.SpikeHurt : SKSpriteMotion.Hurt, set.hit,
                        Time.time - stateSince);
                case SKCombatState.Windup:
                    return ProgressPose(motion, SKSpriteMotion.Windup, set.windup, combat.WindupProgress);
                case SKCombatState.Attacking:
                    return ProgressPose(motion, SKSpriteMotion.Attack, set.attack, combat.StrikeProgress);
            }

            if (dedicatedCapture != null && dedicatedCapture.isActiveAndEnabled && dedicatedCharacter == character &&
                dedicatedCapture.EscapedAt > escapeInvalidatedAt &&
                Time.time - dedicatedCapture.EscapedAt < SKTrapAnimationLibrary.EscapeSeconds)
            {
                var library = SKTrapAnimationLibrary.For(character, dedicatedCapture);
                var escaped = library?.Sample(SKTrapMotionPhase.Escape, 0f, Time.time - dedicatedCapture.EscapedAt);
                if (escaped != null) return escaped;
            }

            // Crouch reuses the drawn jump take-off pose (lowest standing silhouette in each atlas).
            if (player != null && player.IsCrouching) return Pose(motion, SKSpriteMotion.Crouch, set.idle, loop: true);

            float followThrough = Time.time - followThroughSince;
            if (followThrough < followThroughSeconds)
                return ProgressPose(motion, SKSpriteMotion.FollowThrough, set.attack, followThrough / followThroughSeconds);

            var grab = combat != null ? combat.Grab : null;
            bool dedicatedEscape = grab != null && dedicatedCapture != null && dedicatedCharacter == character &&
                dedicatedCapture.EscapedAt == grab.EscapedAt &&
                SKTrapAnimationLibrary.For(character, dedicatedCapture) != null;
            if (!dedicatedEscape && grab != null && grab.EscapedAt > escapeInvalidatedAt &&
                Time.time - grab.EscapedAt < Duration(motion, SKSpriteMotion.Escape))
            {
                var freed = heldByBoss ? SKAppliedSheets.Character(character, "boss_pattern", 8) : null;
                if (freed != null) { currentMotion = null; return freed; }
                return Pose(motion, SKSpriteMotion.Escape, set.run, Time.time - grab.EscapedAt);
            }

            if (airborne) return ProgressPose(motion, SKSpriteMotion.Jump, set.run, JumpProgress());
            // Day 14: grounded on a slime pool swaps run/idle for the sticky versions (display only).
            bool slime = SKSlimePool.AnyCovers(player);
            if (body != null && Mathf.Abs(body.linearVelocity.x) > runSpeedThreshold)
                return Pose(motion, slime ? SKSpriteMotion.SlimeWalk : SKSpriteMotion.Run, set.run, loop: true);
            if (slime) return Pose(motion, SKSpriteMotion.SlimeIdle, set.idle, loop: true);

            int stage = health != null ? health.DamageStage : 0;
            if (stage > 0 && stage <= set.damagedIdle.Length && set.damagedIdle[stage - 1] != null)
            {
                currentMotion = null;
                return set.damagedIdle[stage - 1];
            }
            return Pose(motion, SKSpriteMotion.Idle, set.idle, loop: true);
        }

        private void TrackState(SKCombatState state, int character)
        {
            int cancelVersion = combat != null ? combat.AttackCancelVersion : 0;
            bool reset = character != previousCharacter || cancelVersion != previousCancelVersion;
            if (!reset && state == previousState) return;
            if (reset)
            {
                heldByBoss = false;
                dedicatedCapture = null;
                trapPlayback.Reset();
            }

            bool free = state == SKCombatState.Ready || state == SKCombatState.Recovery;
            if (reset || !free) followThroughSince = float.NegativeInfinity;
            else if (previousState == SKCombatState.Attacking) followThroughSince = Time.time;
            if (reset || state == SKCombatState.Disabled || state == SKCombatState.Dead ||
                state == SKCombatState.Hurt || state == SKCombatState.Windup || state == SKCombatState.Attacking)
                escapeInvalidatedAt = Time.time;

            deathScene = null;
            ShowKiller();
            // All presentation clocks use scaled time and stop together with the combat timers.
            stateSince = Time.time;
            // Decided once, as the hit lands: knockback then carries the player off the crack.
            spikeHurt = state == SKCombatState.Hurt && SKFloorSpike.AnyStrikingAt(transform.position);
            currentMotion = null;
            previousCharacter = character;
            previousCancelVersion = cancelVersion;
            previousState = state;
        }

        private Sprite HeldPose(SKCharacterMotionLibrary motion, Sprite fallback)
        {
            var grab = combat.Grab;
            float sinceGrab = Time.time - grab.HeldSince;
            if (sinceGrab < Duration(motion, SKSpriteMotion.Pounced))
                return Pose(motion, SKSpriteMotion.Pounced, fallback, sinceGrab);
            // A fresh press keeps the effort visible without restarting the clip on every mash.
            if (Time.time - grab.LastStruggleAt < Duration(motion, SKSpriteMotion.Resist))
                return Pose(motion, SKSpriteMotion.Resist, fallback, loop: true);
            return Pose(motion, SKSpriteMotion.Held, fallback, loop: true);
        }

        private void TrackAirborne()
        {
            if (body == null || player == null) return;
            bool inAir = !player.IsGrounded || body.linearVelocity.y > .1f;
            if (!airborne) launchSpeed = Mathf.Max(2f, body.linearVelocity.y);
            if (!airborne || !inAir) groundHeight = body.position.y;
            airborne = inAir;
        }

        private float JumpProgress()
        {
            float verticalSpeed = body.linearVelocity.y;
            float gravity = Mathf.Abs(Physics2D.gravity.y * body.gravityScale);
            float height = Mathf.Max(0f, body.position.y - groundHeight);
            // Use the actual arc after an early jump release, so a short hop can still show its landing pose.
            float landingSpeed = Mathf.Max(2f, Mathf.Sqrt(verticalSpeed * verticalSpeed + 2f * gravity * height));
            float speed = verticalSpeed >= 0f ? launchSpeed : landingSpeed;
            return .5f * (1f - Mathf.Clamp(verticalSpeed / speed, -1f, 1f));
        }

        private Sprite ProgressPose(SKCharacterMotionLibrary motion, SKSpriteMotion clip, Sprite fallback, float progress)
            => Pose(motion, clip, fallback, Mathf.Clamp01(progress) * Duration(motion, clip));

        private Sprite Pose(SKCharacterMotionLibrary motion, SKSpriteMotion clip, Sprite fallback, float? elapsed = null, bool loop = false)
        {
            if (currentMotion != clip)
            {
                currentMotion = clip;
                motionSince = Time.time;
            }
            return motion?.Sample(clip, elapsed ?? Time.time - motionSince, loop) ?? fallback;
        }

        // Drawn defeat per cause (monk hold, executioner blow, boss). Traps keep their absorbed presentation.
        private Sprite DeathPose(int character, float elapsed)
        {
            ObserveNativeDeath();
            if (nativeDeathKey != null && NativeCauseAvailable())
            {
                float nativeElapsed = Mathf.Max(0f, Time.time - nativeDeathSince);
                float patternSeconds = SKNativeDefeatLibrary.BossPatternSeconds(nativeDeathKey);
                bool finish = nativeBossFinish != null && nativeElapsed >= patternSeconds;
                string keyNow = finish ? nativeBossFinish : nativeDeathKey;
                var library = SKNativeDefeatLibrary.Load(character, keyNow);
                if (library != null)
                {
                    NativeSceneKey = keyNow;
                    if(keyNow.Length==5 && keyNow.StartsWith("boss",StringComparison.Ordinal) && nativeDeathBoss!=null)
                    {
                        nativeCommonSeconds=finish?nativeElapsed-patternSeconds:nativeElapsed;
                        NativeFlipX=nativeDeathBoss.transform.position.x<transform.position.x;
                    }
                    if (hiddenKiller == null && (nativeDeathKey == "monk" || nativeDeathKey == "executioner") &&
                        health.LastCause is Component attacker)
                    {
                        var killerView = attacker.GetComponentInParent<SKEnemySpriteView>();
                        hiddenKiller = killerView != null ? killerView.GetComponent<SpriteRenderer>() : null;
                        if (hiddenKiller != null) hiddenKiller.enabled = false;
                    }
                    return library.Sample(finish ? nativeElapsed - patternSeconds : nativeElapsed,
                        finish ? 0 : nativeFirstFrame);
                }
            }
            int step = (int)(elapsed * 6f);
            string key = health != null ? SKGameOverScreen.KeyFor(health.LastCause) : "";
            // Defeat scene with the attacker drawn in: <character>_death_<cause>[1-4].png, played once, last two cells loop.
            // Several numbered sheets = one picked per death. No sheet = the drawn poses below.
            if (deathScene == null) deathScene = PickDeathScene(character, key.ToLowerInvariant());
            if (deathScene.Length > 0)
            {
                if (hiddenKiller == null && key != "BOSS" && health.LastCause is Component killer)
                {
                    var killerView = killer.GetComponentInParent<SKEnemySpriteView>();
                    hiddenKiller = killerView != null ? killerView.GetComponent<SpriteRenderer>() : null;
                    if (hiddenKiller != null) hiddenKiller.enabled = false;
                }
                return SKAppliedSheets.CharacterScene(character, deathScene, step);
            }
            switch (key)
            {
                case "MONK": return SKAppliedSheets.Character(character, "enemy_death", Mathf.Min(step, 3));
                case "EXECUTIONER": return SKAppliedSheets.Character(character, "enemy_death", 4 + Mathf.Min(step, 3));
                case "BOSS": return SKAppliedSheets.Character(character, "boss_pattern", 9 + Mathf.Min(step, 1));
                default: return null;
            }
        }

        private bool NativeFacing(string key, UnityEngine.Object cause)
        {
            var component = cause as Component;
            if (component == null) return display != null && display.flipX;
            bool onRight = component.transform.position.x > transform.position.x;
            // Enemy composites have the attacker on the left; wall/boss composites enter from the right.
            return key == "monk" || key == "executioner" ? onRight :
                key == "tentacle" || key == "boss_spikes" ? display.flipX : !onRight;
        }

        private bool NativeCauseAvailable()
        {
            if (health == null) return false;
            if (nativeDeathBoss != null) return nativeDeathBoss.gameObject.activeInHierarchy &&
                nativeDeathBoss.Health != null && !nativeDeathBoss.Health.IsDead;
            if (health.LastCause is SKEnvironmentalCapture capture) return capture != null && capture.isActiveAndEnabled;
            if (health.LastCause is SKFleshWall wall) return wall != null && wall.isActiveAndEnabled;
            return true;
        }

        private void ObserveNativeDeath()
        {
            if (health == null || !health.IsDead)
            {
                nativeDeathRecorded = false; nativeDeathKey = nativeBossFinish = nativeCommonForDeath = null;
                nativeFatalSource = null; nativeDeathBoss=null; NativeSceneKey = null;
                return;
            }
            if (nativeDeathRecorded || player == null) return;
            nativeDeathRecorded = true;
            var cause = nativeFatalSource != null ? (UnityEngine.Object)nativeFatalSource : health.LastCause;
            string key = SKNativeDefeatLibrary.KeyFor(cause);
            if (key == null) return;
            nativeDeathBoss=(cause as Component)!=null?(cause as Component).GetComponentInParent<SKBossAgent>():null;
            nativeDeathSince = Time.time;
            NativeFlipX = nativeDeathBoss!=null && (key=="boss_spit" || key=="boss_grab")
                ? nativeDeathBoss.transform.position.x < transform.position.x : NativeFacing(key,cause);
            bool boss = key.StartsWith("boss", StringComparison.Ordinal);
            if (boss && nativeCommonForDeath == null) nativeCommonForDeath = "boss" + (nativeBossDeaths++ % SKNativeDefeatLibrary.CommonBossCount + 1);
            // The four compositions play in the user's reference order, once per accepted death.
            nativeDeathKey = key == "boss" ? nativeCommonForDeath : key;
            nativeBossFinish = boss && key != "boss" ? nativeCommonForDeath : null;
            bool alreadyCaptured = previousState == SKCombatState.Held ||
                health.LastCause is SKEnvironmentalCapture capture && capture.Player == player ||
                health.LastCause is SKFleshWall wall && wall.IsPlayerConsumed;
            nativeFirstFrame = SKNativeDefeatLibrary.IsTrap(key) && !SKNativeDefeatLibrary.IsBossPattern(key) && alreadyCaptured
                ? SKNativeDefeatLibrary.LoopStart - 1 : 0;
            if (SKNativeDefeatLibrary.Load(player.SelectedCharacter, nativeDeathKey) == null)
            { nativeDeathKey = nativeBossFinish = null; }
        }

        /// <summary>Accepted-hit callbacks can refine the display source after health notifies death.
        /// This preserves HP's owner/cause and distinguishes a delayed spike during another boss attack.</summary>
        public void RecordNativeFatalSource(Component source)
        {
            if (health == null || !health.IsDead || source == null) return;
            nativeFatalSource = source; nativeDeathRecorded = false; ObserveNativeDeath();
        }

        private static string PickDeathScene(int character, string cause)
        {
            var found = new System.Collections.Generic.List<string>();
            for (int i = 0; i <= 4; i++)
            {
                string kind = "death_" + cause + (i == 0 ? "" : i.ToString());
                if (SKAppliedSheets.Character(character, kind, 0) != null) found.Add(kind);
            }
            return found.Count == 0 ? "" : found[UnityEngine.Random.Range(0, found.Count)];
        }

        private void ShowKiller()
        {
            if (hiddenKiller != null) hiddenKiller.enabled = true;
            hiddenKiller = null;
        }

        private static float Duration(SKCharacterMotionLibrary motion, SKSpriteMotion clip) => motion?.Duration(clip) ?? 0f;
    }
}
