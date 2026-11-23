using System.Collections.Generic;
using SaintKnight.Day02;
using SaintKnight.Day04;
using SaintKnight.Day10;
using UnityEngine;

namespace SaintKnight.Day09
{
    public enum SKBossAttack { None, Sweep, Spikes, Grab, Spit }
    public enum SKBossState { Intro, Idle, Tell, Strike, Recovery, Holding, Exposed, Transition, Dead }

    /// <summary>
    /// First boss "Rose Window Madonna" (Docs/Design/Boss/BOSS_ROSE_MADONNA.md). Fixed on the altar wall; its
    /// heart takes reduced damage until two armor-breaking (Adela) hits open it. Attacks reuse the existing
    /// combat pieces: SKHitbox strikes, the SKGrabState hold, crouch-height swings. Cecilia's chain cuts the
    /// sweep tell, Adela's sword cuts the grab tell (both by hitting the tentacle while it winds up).
    /// Timers use scaled time and stop during hit-stop; a cancelled attack never fires later.
    /// </summary>
    [RequireComponent(typeof(SKHealth), typeof(SpriteRenderer))]
    [DefaultExecutionOrder(100)]
    public sealed class SKBossAgent : MonoBehaviour
    {
        [Header("Links")]
        [SerializeField] private SKHealth player;
        [SerializeField] private SKHitbox sweepHitbox;
        [SerializeField] private SKFloorSpike[] spikes;     // Day 10 shared floor tentacles
        [SerializeField] private SKHealth tentacle;
        [SerializeField] private BoxCollider2D tentacleArea;
        [SerializeField] private SpriteRenderer warning;
        [SerializeField] private SKEnemyAgent summon;
        [SerializeField] private Vector2 arenaX = new Vector2(-6f, 6f);
        [SerializeField] private float floorOffset = .575f;

        [Header("Heart shell")]
        [SerializeField, Range(0f, 1f)] private float shellDamageScale = .3f;   // 0.2 -> 0.3: chip damage shows (first boss)
        [SerializeField, Min(1)] private int breaksToOpen = 2;
        [SerializeField] private float openSeconds = 4f, openSecondsPhase2 = 3f, exposedStagger = 1f;

        [Header("Rhythm")]
        [SerializeField] private float introSeconds = 1.5f, transitionSeconds = 2f;
        [SerializeField] private Vector2 gapPhase1 = new Vector2(1.4f, 1.8f), gapPhase2 = new Vector2(1f, 1.4f);

        [Header("High sweep (crouch under it)")]
        [SerializeField] private float sweepTell = .9f, sweepActive = .25f, sweepRecovery = .8f;
        [SerializeField] private Vector2 sweepSize = new Vector2(4.5f, .5f);
        [SerializeField] private float sweepOffset = 2.45f, sweepHeight = .475f, sweepDamage = 20f, sweepKnockback = 3f;

        [Header("Floor tentacles (step away or jump; tell/active/damage live on each SKFloorSpike)")]
        [SerializeField] private float spikeGap = .3f, spikeSpacing = .6f, spikeRecovery = .6f;

        [Header("Tentacle grab (jump it, mash out)")]
        [SerializeField] private float grabTell = .6f, grabRange = 3.2f, grabMissRecovery = 1f;
        [SerializeField] private float grabTickDamage = 4f, grabTickSeconds = .5f, grabMaxHold = 4f, grabThrowDamage = 12f;
        [SerializeField] private float struggleThreshold = 6f, struggleDecay = 2.5f, escapeStagger = 1.2f, escapeImmunity = .6f;

        [Header("Slime spit (phase 2)")]
        [SerializeField] private float spitTell = .6f, spitFlight = .8f, puddleSeconds = 3f;

        [Header("Drawing (display only)")]
        [SerializeField] private Sprite[] bodyPhase1, bodyPhase2, bodyDead, coreClosed, coreCracked, coreOpen;
        [SerializeField] private Sprite sweepTellSprite, sweepStrikeSprite, grabReach, grabCoil;
        [SerializeField] private SpriteRenderer core, sweepVisual, grabVisual, coilVisual, slimeBall;
        [SerializeField] private SKSlimePool puddle;       // Day 10 shared slime pool
        [SerializeField] private Transform hpFill;
        [SerializeField] private TextMesh hpLabel;

        private SKHealth health;
        private SpriteRenderer body;
        private SKGrabState playerGrab;
        private Rigidbody2D playerBody;
        private SKEnemyTrainingSession session;
        private readonly List<SKBossAttack> history = new List<SKBossAttack>();
        private SKBossAttack forced = SKBossAttack.None;
        private float readyAt, strikeAt, strikeEnds, staggerEnds, transitionEnds, coreOpenUntil = float.NegativeInfinity;
        private float holdStartedAt, nextDrainAt, spitStart, spitTarget;
        private Vector2 holdAt;
        private bool spitFlying, summoned;
        private int breaks;

        public SKBossState State { get; private set; } = SKBossState.Intro;
        public SKBossAttack Attack { get; private set; } = SKBossAttack.None;
        public int Phase { get; private set; } = 1;
        public bool CoreOpen => Time.time < coreOpenUntil;
        public int ShellBreaks => breaks;
        public float Facing { get; private set; } = -1f;
        public SKHealth Health => health;
        public bool IsHoldingPlayer => playerGrab != null && playerGrab.IsHeld && playerGrab.Holder == this;
        public bool PuddleActive => puddle != null && puddle.Active;
        public float PuddleX => puddle != null ? puddle.transform.position.x : 0f;
        private float FloorY => transform.position.y - floorOffset;

        /// <summary>Next choice ignores distance and randomness (tests, scripted openings).</summary>
        public void ForceNext(SKBossAttack attack) => forced = attack;

        private void Awake()
        {
            health = GetComponent<SKHealth>();
            body = GetComponent<SpriteRenderer>();
            if (player != null)
            {
                playerBody = player.GetComponent<Rigidbody2D>();
                var combat = player.GetComponent<SKPlayerCombat>();
                playerGrab = combat != null ? combat.Grab : player.GetComponent<SKGrabState>();
            }
        }

        private void OnEnable()
        {
            health.Damaged += OnHeartHit;
            if (tentacle != null) tentacle.Damaged += OnTentacleHit;
        }

        private void Start()
        {
            session = FindFirstObjectByType<SKEnemyTrainingSession>();
            if (session != null) session.TrainingReset += ResetBoss;
            ResetBoss();
        }

        private void OnDisable()
        {
            health.Damaged -= OnHeartHit;
            if (tentacle != null) tentacle.Damaged -= OnTentacleHit;
            CancelAll();
        }

        private void OnDestroy()
        {
            if (session != null) session.TrainingReset -= ResetBoss;
        }

        public void ResetBoss()
        {
            CancelAll();
            health.ResetHealth();
            health.DamageScale = shellDamageScale;
            coreOpenUntil = float.NegativeInfinity;
            breaks = 0;
            Phase = 1;
            history.Clear();
            forced = SKBossAttack.None;
            State = SKBossState.Intro;
            readyAt = Time.time + introSeconds;
            summoned = false;
            if (summon != null && summon.gameObject.activeSelf)
            {
                summon.Health.ResetHealth();
                summon.ResetAgent();
                summon.gameObject.SetActive(false);
            }
        }

        // ------------------------------------------------------------------ brain
        private void FixedUpdate()
        {
            if (SKHitStop.IsActive) return;
            if (health.IsDead)
            {
                if (State != SKBossState.Dead) { CancelAll(); State = SKBossState.Dead; }
                return;
            }
            if (player == null || !player.isActiveAndEnabled || player.IsDead)
            {
                if (State != SKBossState.Idle || Attack != SKBossAttack.None) CancelAttack();
                State = SKBossState.Idle;
                readyAt = Time.time + 1f;
                return;
            }
            if (Phase == 1 && health.CurrentHp <= health.MaximumHp * .5f) BeginTransition();
            if (coreOpenUntil > float.NegativeInfinity && !CoreOpen) CloseCore();
            UpdateSpit();

            switch (State)
            {
                case SKBossState.Intro:
                case SKBossState.Idle:
                case SKBossState.Recovery:
                    if (Time.time >= readyAt) StartTell(Choose());
                    break;
                case SKBossState.Transition:
                    if (Time.time < transitionEnds) break;
                    State = SKBossState.Idle;
                    readyAt = Time.time + .5f;
                    if (summon != null && !summoned) { summoned = true; summon.gameObject.SetActive(true); }
                    break;
                case SKBossState.Exposed:
                    if (Time.time >= staggerEnds) { State = SKBossState.Idle; readyAt = Time.time + .3f; }
                    break;
                case SKBossState.Tell:
                    if (Time.time >= strikeAt) Strike();
                    break;
                case SKBossState.Strike:
                    if (Time.time >= strikeEnds) EndAttack(RecoveryOf(Attack));
                    break;
                case SKBossState.Holding:
                    UpdateHold();
                    break;
            }
        }

        private SKBossAttack Choose()
        {
            if (forced != SKBossAttack.None) { var f = forced; forced = SKBossAttack.None; return f; }
            float d = Mathf.Abs(player.transform.position.x - transform.position.x);
            var options = new List<SKBossAttack>();
            if (d <= 1.5f) { options.Add(SKBossAttack.Sweep); options.Add(SKBossAttack.Grab); }
            else if (d <= 4f) { options.Add(SKBossAttack.Sweep); options.Add(SKBossAttack.Spikes); }
            else options.Add(SKBossAttack.Spikes);
            if (Phase == 2 && d > 1.5f) options.Add(SKBossAttack.Spit);
            // Never the same attack three times in a row.
            if (history.Count >= 2 && history[^1] == history[^2] && options.Count > 1) options.Remove(history[^1]);
            return options[Random.Range(0, options.Count)];
        }

        private void StartTell(SKBossAttack attack)
        {
            Attack = attack;
            history.Add(attack);
            if (history.Count > 4) history.RemoveAt(0);
            State = SKBossState.Tell;
            Facing = player.transform.position.x >= transform.position.x ? 1f : -1f;
            switch (attack)
            {
                case SKBossAttack.Sweep:
                    strikeAt = Time.time + sweepTell;
                    ShowWarning(new Vector2(Facing * sweepOffset, sweepHeight), sweepSize, new Color(1f, .2f, .15f, .22f));
                    ShowTentacle(new Vector2(Facing * 1f, sweepHeight), new Vector2(1.2f, .9f));
                    break;
                case SKBossAttack.Grab:
                    strikeAt = Time.time + grabTell;
                    ShowWarning(new Vector2(Facing * grabRange * .5f, -floorOffset + .3f), new Vector2(grabRange, .6f),
                        new Color(.65f, .25f, 1f, .3f));
                    ShowTentacle(new Vector2(Facing * 1f, -floorOffset + .3f), new Vector2(1.4f, .6f));
                    break;
                case SKBossAttack.Spikes:
                    float dir = Mathf.Sign(player.transform.position.x - transform.position.x);
                    int count = StartSpikes(player.transform.position.x, dir == 0f ? 1f : dir, 3, 0f);
                    strikeAt = Time.time + (count - 1) * spikeGap + spikes[0].TellSeconds;
                    break;
                case SKBossAttack.Spit:
                    strikeAt = Time.time + spitTell;
                    break;
            }
        }

        private void Strike()
        {
            HideWarning();
            HideTentacle();
            State = SKBossState.Strike;
            strikeEnds = Time.time;
            switch (Attack)
            {
                case SKBossAttack.Sweep:
                    sweepHitbox.Begin(Facing, sweepSize, sweepOffset, sweepDamage, sweepActive, sweepKnockback,
                        new Color(1f, .2f, .15f, .5f), sweepHeight);
                    Physics2D.SyncTransforms();
                    strikeEnds = Time.time + sweepActive;
                    if (Phase == 2) StartSpikes(player.transform.position.x, Facing, 1, .6f - spikes[0].TellSeconds); // combo
                    break;
                case SKBossAttack.Spikes:
                    strikeEnds = Time.time + spikes[0].ActiveSeconds;
                    break;
                case SKBossAttack.Grab:
                    if (TryGrab()) return;
                    EndAttack(grabMissRecovery);
                    return;
                case SKBossAttack.Spit:
                    float half = puddle != null ? puddle.HalfWidth : 0f;
                    spitTarget = Mathf.Clamp(player.transform.position.x, arenaX.x + half, arenaX.y - half);
                    spitStart = Time.time;
                    spitFlying = true;
                    break;
            }
        }

        private float RecoveryOf(SKBossAttack attack) => attack switch
        {
            SKBossAttack.Sweep => sweepRecovery,
            SKBossAttack.Spikes => spikeRecovery,
            _ => .6f
        };

        private void EndAttack(float recovery)
        {
            Attack = SKBossAttack.None;
            State = SKBossState.Recovery;
            var gap = Phase == 1 ? gapPhase1 : gapPhase2;
            readyAt = Time.time + recovery + Random.Range(gap.x, gap.y);
        }

        // ------------------------------------------------------------------ heart, tentacle cuts, phases
        private void OnHeartHit()
        {
            if (health.IsDead || State == SKBossState.Transition || CoreOpen || !health.LastHitBreaksArmor) return;
            if (++breaks < breaksToOpen) return;
            CancelAttack();
            breaks = 0;
            coreOpenUntil = Time.time + (Phase == 1 ? openSeconds : openSecondsPhase2);
            health.DamageScale = 1f;
            State = SKBossState.Exposed;
            staggerEnds = Time.time + exposedStagger;
        }

        private void CloseCore()
        {
            coreOpenUntil = float.NegativeInfinity;
            health.DamageScale = shellDamageScale;
        }

        private void OnTentacleHit()
        {
            tentacle.ResetHealth(); // the tentacle is a target for cutting, never a health pool
            if (State != SKBossState.Tell) return;
            bool cut = (Attack == SKBossAttack.Sweep && tentacle.LastHitInterruptDelay > 0f) ||
                (Attack == SKBossAttack.Grab && tentacle.LastHitBreaksArmor);
            if (!cut) return;
            float delay = tentacle.LastHitInterruptDelay;
            CancelAttack();
            EndAttack(.6f + delay);
        }

        private void BeginTransition()
        {
            CancelAttack();
            CloseCore();
            breaks = 0;
            Phase = 2;
            State = SKBossState.Transition;
            transitionEnds = Time.time + transitionSeconds;
            health.GrantInvulnerability(transitionSeconds);
        }

        // ------------------------------------------------------------------ floor tentacles
        private int StartSpikes(float x, float dir, int count, float delay)
        {
            int started = 0;
            for (int i = 0; i < spikes.Length && started < count; i++)
            {
                if (spikes[i].IsArmed) continue; // busy
                float at = Mathf.Clamp(x + dir * started * spikeSpacing, arenaX.x + .3f, arenaX.y - .3f);
                spikes[i].Arm(new Vector2(at, FloorY), delay + started * spikeGap);
                started++;
            }
            return Mathf.Max(1, started);
        }

        // ------------------------------------------------------------------ grab
        private bool TryGrab()
        {
            Vector2 d = player.transform.position - transform.position;
            float playerFeet = player.TryGetComponent<BoxCollider2D>(out var box)
                ? box.bounds.min.y : player.transform.position.y;
            bool onFloor = playerFeet - FloorY < .3f;   // jumping over the crawling tentacle avoids it
            bool inReach = Mathf.Abs(d.x) <= grabRange && (Mathf.Sign(d.x) == Facing || Mathf.Abs(d.x) < .3f);
            if (playerGrab == null || !onFloor || !inReach ||
                !playerGrab.TryBegin(this, struggleThreshold, struggleDecay)) return false;
            State = SKBossState.Holding;
            holdAt = player.transform.position;
            holdStartedAt = Time.time;
            nextDrainAt = Time.time + grabTickSeconds;
            Pin();
            return true;
        }

        private void UpdateHold()
        {
            if (!IsHoldingPlayer) { ReleaseHold(); EndAttack(.6f); return; }
            Pin();
            if (playerGrab.EscapeReady)
            {
                ReleaseHold(true);
                player.GrantInvulnerability(escapeImmunity);
                if (playerBody != null) playerBody.linearVelocity = new Vector2(-Facing * 3f, 2f);
                EndAttack(escapeStagger);
                return;
            }
            if (Time.time >= holdStartedAt + grabMaxHold)
            {
                ReleaseHold();
                if (player.TryTakeDamage(grabThrowDamage, transform.position, 4.5f, cause: this))
                    SKHitStop.Request(.045f, gameObject.scene);
                EndAttack(.8f);
                return;
            }
            while (Time.time >= nextDrainAt && !player.IsDead)
            {
                player.Drain(grabTickDamage, this);
                nextDrainAt += grabTickSeconds;
            }
        }

        private void Pin()
        {
            if (playerBody != null) { playerBody.position = holdAt; playerBody.linearVelocity = Vector2.zero; }
            player.transform.position = new Vector3(holdAt.x, holdAt.y, player.transform.position.z);
        }

        private void ReleaseHold(bool escaped = false)
        {
            if (IsHoldingPlayer) playerGrab.Release(this, escaped);
        }

        // ------------------------------------------------------------------ slime
        private void UpdateSpit()
        {
            if (!spitFlying || Time.time < spitStart + spitFlight) return;
            spitFlying = false;
            if (puddle != null) puddle.Spawn(new Vector2(spitTarget, FloorY), puddleSeconds);
        }

        // ------------------------------------------------------------------ cancel
        private void CancelAttack()
        {
            if (sweepHitbox != null) sweepHitbox.Cancel();
            if (spikes != null) foreach (var spike in spikes) if (spike != null) spike.Cancel();
            HideWarning();
            HideTentacle();
            ReleaseHold();
            spitFlying = false;
            Attack = SKBossAttack.None;
        }

        private void CancelAll()
        {
            CancelAttack();
            if (puddle != null) puddle.Clear();
        }

        private void ShowWarning(Vector2 local, Vector2 size, Color color)
        {
            if (warning == null) return;
            warning.transform.localPosition = local;
            warning.size = size;
            warning.color = color;
            warning.enabled = true;
        }

        private void HideWarning() { if (warning != null) warning.enabled = false; }

        private void ShowTentacle(Vector2 local, Vector2 size)
        {
            if (tentacleArea == null) return;
            tentacleArea.transform.localPosition = local;
            tentacleArea.size = size;
            tentacleArea.enabled = true;
            Physics2D.SyncTransforms();
        }

        private void HideTentacle() { if (tentacleArea != null) tentacleArea.enabled = false; }

        // ------------------------------------------------------------------ drawing (reads state only)
        // Runs after SKHealth.Update (execution order 100): the withered sprite carries its own colour, so
        // undo the generic dead tint here - before coroutines and LateUpdate, so every view sees the same.
        private void Update()
        {
            if (health.IsDead) body.color = Color.white;
            // CoreOpen follows frame time; close the shell here too so no reader sees "closed" at full damage
            // until the next physics step.
            if (coreOpenUntil > float.NegativeInfinity && !CoreOpen) CloseCore();
        }

        private void LateUpdate()
        {
            float t = Time.time;
            body.sprite = State == SKBossState.Dead ? First(bodyDead)
                : State == SKBossState.Transition ? Loop((t * 8f) % 2f < 1f ? bodyPhase1 : bodyPhase2, t / .4f)
                : Loop(Phase == 1 ? bodyPhase1 : bodyPhase2, t / .45f);
            if (core != null)
            {
                core.enabled = State != SKBossState.Dead;
                core.sprite = CoreOpen ? Loop(coreOpen, t / .15f) : breaks > 0 ? First(coreCracked) : First(coreClosed);
            }
            Place(sweepVisual, Attack == SKBossAttack.Sweep && (State == SKBossState.Tell || State == SKBossState.Strike),
                State == SKBossState.Strike ? sweepStrikeSprite : sweepTellSprite, transform.position.x, Facing);
            Place(grabVisual, Attack == SKBossAttack.Grab && State == SKBossState.Tell, grabReach, transform.position.x, Facing);
            Place(coilVisual, State == SKBossState.Holding, grabCoil, holdAt.x, 1f);
            if (slimeBall != null)
            {
                slimeBall.enabled = spitFlying;
                if (spitFlying)
                {
                    float k = Mathf.Clamp01((t - spitStart) / spitFlight);
                    var from = new Vector2(transform.position.x, FloorY + 3f);
                    slimeBall.transform.position = new Vector3(Mathf.Lerp(from.x, spitTarget, k),
                        Mathf.Lerp(from.y, FloorY + .1f, k) + 2f * k * (1f - k) * 2f, 0f);
                }
            }
            if (hpFill != null)
                hpFill.localScale = new Vector3(Mathf.Clamp01(health.CurrentHp / health.MaximumHp), 1f, 1f);
            if (hpLabel != null)
                hpLabel.text = State == SKBossState.Dead ? "ROSE WINDOW MADONNA - DEFEATED"
                    : "ROSE WINDOW MADONNA" + (Phase == 2 ? "  II" : "") + (CoreOpen ? "   HEART OPEN!" : "");
        }

        private void Place(SpriteRenderer r, bool show, Sprite sprite, float x, float facing)
        {
            if (r == null) return;
            r.enabled = show;
            if (!show) return;
            r.sprite = sprite;
            r.flipX = facing < 0f;
            r.transform.position = new Vector3(x, FloorY, r.transform.position.z);
        }

        private static Sprite First(Sprite[] s) => s != null && s.Length > 0 ? s[0] : null;
        private static Sprite Loop(Sprite[] s, float f) =>
            s != null && s.Length > 0 ? s[(int)Mathf.Repeat(Mathf.Floor(f), s.Length)] : null;
    }
}
