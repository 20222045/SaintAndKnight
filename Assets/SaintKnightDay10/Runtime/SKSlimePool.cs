using System.Collections.Generic;
using SaintKnight.Day01;
using SaintKnight.Day02;
using SaintKnight.Day04;
using UnityEngine;

namespace SaintKnight.Day10
{
    /// <summary>
    /// Day 10 slime pool: while the player walks on it (grounded, feet over its width) walking speed is
    /// multiplied by <c>slow</c>. Staying on the ground in it lets the mass restrain and absorb the player.
    /// A stage pool is permanent; the boss spawns timed ones.
    /// Every enabled pool writes the same answer (slowest covering pool), so several pools never fight.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SKSlimePool : MonoBehaviour
    {
        private const float FrameSeconds = .18f;
        private const float CaptureDwellSeconds = 1.25f;
        private static readonly List<SKSlimePool> Pools = new List<SKSlimePool>();

        [SerializeField] private SKDay01Player player;
        [SerializeField] private float halfWidth = .65f;
        [SerializeField, Range(0f, 1f)] private float slow = .5f;
        [SerializeField] private bool permanent;
        [SerializeField] private Sprite[] loopFrames;

        private SpriteRenderer view;
        private float spawnedAt;
        private float until = float.NegativeInfinity;
        private float dwellSince = float.NegativeInfinity;
        private SKEnvironmentalCapture capture;
        private SKEnemyTrainingSession session;

        public bool Active => permanent || Time.time < until ||
            (capture != null && (capture.IsHoldingPlayer || capture.IsPlayerConsumed));
        /// <summary>False for the boss's timed spit pool.</summary>
        public bool Permanent => permanent;
        public float HalfWidth => halfWidth;
        public float Slow => slow;
        public float DwellSeconds => CaptureDwellSeconds;
        public SKEnvironmentalCapture Capture => capture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Pools.Clear();

        private void Awake()
        {
            view = GetComponent<SpriteRenderer>();
            capture = GetComponent<SKEnvironmentalCapture>();
            if (capture == null) capture = gameObject.AddComponent<SKEnvironmentalCapture>();
            capture.Configure(SKEnvironmentalCaptureKind.Slime);
        }

        private void Start()
        {
            session = FindFirstObjectByType<SKEnemyTrainingSession>();
            if (session != null) session.TrainingReset += ResetPool;
        }

        private void OnEnable() => Pools.Add(this);

        private void OnDisable()
        {
            Pools.Remove(this);
            ResetPool();
            Apply(player);
        }

        private void OnDestroy()
        {
            if (session != null) session.TrainingReset -= ResetPool;
        }

        private void ResetPool()
        {
            dwellSince = float.NegativeInfinity;
            if (capture != null) capture.ResetCapture();
        }

        /// <summary>Lands a pool at <paramref name="floorPoint"/> for <paramref name="seconds"/>.</summary>
        public void Spawn(Vector2 floorPoint, float seconds)
        {
            ResetPool();
            transform.position = new Vector3(floorPoint.x, floorPoint.y, transform.position.z);
            spawnedAt = Time.time;
            until = spawnedAt + seconds;
        }

        public void Clear()
        {
            ResetPool();
            until = float.NegativeInfinity;
            Apply(player);
        }

        public bool Covers(SKDay01Player who)
        {
            if (!Active || who == null || !who.IsGrounded) return false;
            Vector2 d = who.transform.position - transform.position;
            return Mathf.Abs(d.x) <= halfWidth && d.y > -.2f && d.y < 1.2f;   // feet on this floor, not a ledge above
        }

        /// <summary>Day 14 display query: is <paramref name="who"/> standing in any live pool?</summary>
        public static bool AnyCovers(SKDay01Player who)
        {
            foreach (var pool in Pools)
                if (pool.Covers(who)) return true;
            return false;
        }

        private void FixedUpdate()
        {
            if (SKHitStop.IsActive) return;
            Apply(player);
            if (capture == null || capture.IsHoldingPlayer || capture.IsPlayerConsumed)
            {
                dwellSince = float.NegativeInfinity;
                return;
            }
            var health = player != null ? player.GetComponent<SKHealth>() : null;
            var grab = player != null ? player.GetComponent<SKGrabState>() : null;
            if (!Covers(player) || !player.isActiveAndEnabled || health == null ||
                !health.isActiveAndEnabled || health.IsDead || (grab != null && grab.IsHeld))
            {
                dwellSince = float.NegativeInfinity;
                return;
            }
            if (float.IsNegativeInfinity(dwellSince)) dwellSince = Time.time;
            if (Time.time - dwellSince >= CaptureDwellSeconds && capture.TryCapture(player))
                dwellSince = float.NegativeInfinity;
        }

        private static void Apply(SKDay01Player who)
        {
            if (who == null) return;
            float speed = 1f;
            foreach (var pool in Pools)
                if (pool.player == who && pool.Covers(who)) speed = Mathf.Min(speed, pool.slow);
            who.SpeedMultiplier = speed;
        }

        private void LateUpdate()
        {
            view.enabled = Active;
            if (!view.enabled || loopFrames == null || loopFrames.Length == 0) return;
            int index = Mathf.FloorToInt((Time.time - spawnedAt) / FrameSeconds) % loopFrames.Length;
            if (loopFrames[index] != null) view.sprite = loopFrames[index];
        }
    }
}
