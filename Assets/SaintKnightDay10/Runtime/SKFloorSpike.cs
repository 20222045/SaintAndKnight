using SaintKnight.Day02;
using SaintKnight.Day04;
using SaintKnight.Day01;
using UnityEngine;

namespace SaintKnight.Day10
{
    /// <summary>
    /// Day 10 floor tentacle: a crack swells for <c>tell</c> seconds, then a tentacle erupts (SKHitbox) for
    /// <c>active</c>. Shared by the stage trap (repeats by itself when <c>period</c> &gt; 0) and the boss
    /// (calls <see cref="Arm"/>). Scaled time, so hit-stop pauses it; a cancelled strike never fires later.
    /// The hitbox owner decides who it can hurt (team) and where knockback comes from.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SKFloorSpike : MonoBehaviour
    {
        private const float RetractSeconds = .2f;

        [SerializeField] private SKHitbox hitbox;
        [SerializeField] private float tell = .7f, active = .3f, damage = 15f, knockback = 1.5f;
        [SerializeField] private Vector2 size = new Vector2(.6f, 1.4f);
        [SerializeField] private Sprite bulge, strike;
        [SerializeField] private Sprite[] tellFrames, riseFrames, retractFrames;
        [Header("Stage trap: repeats by itself (0 = only when armed)")]
        [SerializeField, Min(0f)] private float period;
        [SerializeField, Min(0f)] private float firstDelay;

        private SpriteRenderer view;
        private SKEnemyTrainingSession session;
        private float strikeAt = float.PositiveInfinity;
        private bool begun;
        private SKEnvironmentalCapture capture;

        public float TellSeconds => tell;
        public float ActiveSeconds => active;
        public float Damage => damage;
        public SKEnvironmentalCapture Capture => capture;
        public bool IsArmed => strikeAt < float.PositiveInfinity;
        /// <summary>Time the tentacle erupts (PositiveInfinity when idle).</summary>
        public float StrikeAt => strikeAt;
        public bool IsTelling => IsArmed && !begun && Time.time >= strikeAt - tell;

        private void Awake()
        {
            view = GetComponent<SpriteRenderer>();
            capture = GetComponent<SKEnvironmentalCapture>();
            if (capture == null) capture = gameObject.AddComponent<SKEnvironmentalCapture>();
            capture.Configure(SKEnvironmentalCaptureKind.Tentacle);
        }

        private void Start()
        {
            if (hitbox == null) hitbox = GetComponentInChildren<SKHitbox>();
            BindAcceptedHit();
            session = FindFirstObjectByType<SKEnemyTrainingSession>();
            if (session != null) session.TrainingReset += ResetCycle;
            ResetCycle();
        }

        private void OnDestroy()
        {
            if (session != null) session.TrainingReset -= ResetCycle;
            if (hitbox != null) hitbox.HitAccepted -= OnHitAccepted;
        }

        private static readonly System.Collections.Generic.List<SKFloorSpike> Spikes =
            new System.Collections.Generic.List<SKFloorSpike>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Spikes.Clear();

        private void OnEnable()
        {
            Spikes.Add(this);
            BindAcceptedHit();
        }

        private void BindAcceptedHit()
        {
            if (hitbox == null) return;
            hitbox.HitAccepted -= OnHitAccepted;
            hitbox.HitAccepted += OnHitAccepted;
        }

        private void OnHitAccepted(SKHealth victim)
        {
            if (victim != null && victim.IsDead)
                victim.GetComponent<SaintKnight.Day06.SKCharacterSpriteView>()?.RecordNativeFatalSource(this);
            // The event is emitted only after this hitbox's damage was accepted, before hit-stop begins.
            var who = victim != null ? victim.GetComponent<SKDay01Player>() : null;
            if (capture != null && capture.TryCapture(who, allowInvulnerabilityFromAcceptedHit: true))
                hitbox.Cancel();
        }

        private void OnDisable()
        {
            Spikes.Remove(this);
            if (hitbox != null) hitbox.HitAccepted -= OnHitAccepted;
            Cancel();
            if (capture != null) capture.ResetCapture();
        }

        /// <summary>Day 14 display query: is a tentacle erupting under <paramref name="point"/> right now?</summary>
        // ponytail: position test, not a damage-source id. A different hit landing in the same instant on an
        // erupting crack shows the spike pose too. Pass the attacker through SKHealth if that ever matters.
        public static bool AnyStrikingAt(Vector2 point)
        {
            foreach (var spike in Spikes)
                if (spike.hitbox != null && spike.hitbox.IsActive &&
                    Mathf.Abs(point.x - spike.transform.position.x) <= spike.size.x * .5f + .5f) return true;
            return false;
        }

        /// <summary>Swell at <paramref name="floorPoint"/> after <paramref name="delay"/>, erupt <c>tell</c> later.</summary>
        public void Arm(Vector2 floorPoint, float delay)
        {
            if (capture != null && (capture.IsHoldingPlayer || capture.IsPlayerConsumed)) return;
            Cancel();
            transform.position = new Vector3(floorPoint.x, floorPoint.y, transform.position.z);
            strikeAt = Time.time + Mathf.Max(0f, delay) + tell;
        }

        public void Cancel()
        {
            if (hitbox != null) hitbox.Cancel();
            strikeAt = float.PositiveInfinity;
            begun = false;
        }

        /// <summary>R / retry: a looping trap restarts its rhythm; a boss-armed one just stops.</summary>
        public void ResetCycle()
        {
            Cancel();
            if (capture != null) capture.ResetCapture();
            if (period > 0f) strikeAt = Time.time + firstDelay + tell;
        }

        private void FixedUpdate()
        {
            if (capture != null && (capture.IsHoldingPlayer || capture.IsPlayerConsumed))
            {
                if (hitbox != null && hitbox.IsActive) hitbox.Cancel();
                return;
            }
            if (SKHitStop.IsActive || !IsArmed) return;
            if (!begun && Time.time >= strikeAt)
            {
                begun = true;
                // Clear colour: the tentacle art shows the strike, the training box would cover it (user, 2026-11-11).
                hitbox.Begin(1f, size, 0f, damage, active, knockback, Color.clear, size.y * .5f);
                Physics2D.SyncTransforms();
            }
            else if (begun && Time.time >= strikeAt + active + RetractSeconds)
            {
                begun = false;
                // A short period starts the next tell as soon as retraction ends.
                strikeAt = period > 0f ? Mathf.Max(strikeAt + period, Time.time + tell) : float.PositiveInfinity;
            }
        }

        private void LateUpdate()
        {
            float elapsed = Time.time - strikeAt;
            view.enabled = elapsed >= -tell;
            if (!view.enabled) return;

            if (elapsed < 0f)
                view.sprite = PhaseFrame(tellFrames, (elapsed + tell) / tell, bulge);
            else if (elapsed < active)
                view.sprite = PhaseFrame(riseFrames, elapsed / active, strike);
            else
                view.sprite = PhaseFrame(retractFrames, (elapsed - active) / RetractSeconds, bulge);
        }

        private static Sprite PhaseFrame(Sprite[] frames, float progress, Sprite fallback)
        {
            if (frames == null || frames.Length == 0) return fallback;
            int index = Mathf.Clamp((int)(progress * frames.Length), 0, frames.Length - 1);
            return frames[index] != null ? frames[index] : fallback;
        }
    }
}
