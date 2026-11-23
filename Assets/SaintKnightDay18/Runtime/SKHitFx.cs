using SaintKnight.Day02;
using SaintKnight.Day04;
using SaintKnight.Day08;
using SaintKnight.Day09;
using UnityEngine;

namespace SaintKnight.Day18
{
    /// <summary>
    /// Day 18 hit effects (display only, scaled time so hit-stop freezes them too):
    /// a spark where a hit lands, white shards when Adela breaks an armored tell, a violet chain ring
    /// when Cecilia's chain delays an enemy, and a short vertical shake when the player is hurt.
    /// Day 24: a flash when the boss heart opens and a big shake on her phase change.
    /// ponytail: code-drawn pixel bursts; swap for sprite sheets when the art exists.
    /// </summary>
    public sealed class SKHitFx : MonoBehaviour
    {
        public enum Kind { Spark, ArmorBreak, Chain, Heart, Phase }
        public static int Spawned { get; private set; }

        private SpriteRenderer[] bits;
        private Vector2[] velocity;
        private float born, life;

        /// <summary>Hooks every health in the scene (inactive ones too, e.g. the boss's summon).</summary>
        public static void Watch(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var h in FindObjectsByType<SKHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (h.gameObject.scene != scene) continue;
                var target = h;
                target.Damaged += () => OnDamaged(target);
            }
            var boss = FindAnyObjectByType<SKBossAgent>();
            if (boss != null) boss.gameObject.AddComponent<BossWatch>().boss = boss;
        }

        private static void OnDamaged(SKHealth h)
        {
            Vector2 at = Centre(h);
            at.x += Mathf.Clamp(h.LastDamageSource.x - at.x, -.25f, .25f);
            bool player = h.Team == SKTeam.Player;
            Spawn(player ? new Color(1f, .35f, .3f) : new Color(1f, .9f, .6f), Kind.Spark, at);
            if (player) SKScreenShake.Kick(.06f, .14f);

            var agent = h.GetComponent<SKEnemyAgent>();
            if (agent == null) return;
            if (agent.ArmorBrokenAt == Time.time) Spawn(Color.white, Kind.ArmorBreak, Centre(h) + Vector2.up * .3f);
            else if (h.LastHitInterruptDelay > 0f && agent.StaggeredUntil > Time.time)
                Spawn(new Color(.72f, .5f, 1f), Kind.Chain, Centre(h) + Vector2.up * .35f);
        }

        private static Vector2 Centre(SKHealth h)
        {
            var c = h.GetComponent<Collider2D>();
            return c != null ? (Vector2)c.bounds.center : (Vector2)h.transform.position;
        }

        public static SKHitFx Spawn(Color color, Kind kind, Vector2 at)
        {
            Spawned++;
            var fx = new GameObject("Fx_" + kind).AddComponent<SKHitFx>();
            fx.transform.position = at;
            int count = kind switch { Kind.Spark => 6, Kind.ArmorBreak => 9, Kind.Chain => 10, Kind.Heart => 12, _ => 18 };
            float speed = kind switch { Kind.Spark => 2.2f, Kind.ArmorBreak => 3.2f, Kind.Chain => 0f, Kind.Heart => 2.6f, _ => 4f };
            fx.life = kind switch { Kind.Spark => .16f, Kind.Chain => .4f, Kind.Phase => .7f, _ => .35f };
            fx.born = Time.time;
            fx.bits = new SpriteRenderer[count];
            fx.velocity = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                float a = (i + (kind == Kind.Spark ? Random.value * .5f : 0f)) / count * Mathf.PI * 2f;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var r = new GameObject("p").AddComponent<SpriteRenderer>();
                r.transform.SetParent(fx.transform, false);
                r.sprite = Pixel;
                r.color = color;
                r.sortingOrder = 40;
                float size = kind == Kind.ArmorBreak ? 3f : 2f;
                r.transform.localScale = new Vector3(size, size, 1f);
                // the chain ring sits still on a circle; everything else flies out
                r.transform.localPosition = kind == Kind.Chain ? (Vector3)(dir * .32f) : Vector3.zero;
                fx.bits[i] = r;
                fx.velocity[i] = dir * speed * (kind == Kind.Spark ? Random.Range(.6f, 1.2f) : 1f);
            }
            return fx;
        }

        private void Update()
        {
            float t = (Time.time - born) / life;
            if (t >= 1f) { Destroy(gameObject); return; }
            for (int i = 0; i < bits.Length; i++)
            {
                bits[i].transform.localPosition += (Vector3)(velocity[i] * Time.deltaTime);
                var c = bits[i].color;
                c.a = 1f - t * t;
                bits[i].color = c;
            }
            if (velocity.Length > 0 && velocity[0] == Vector2.zero)   // chain ring: slowly closes in
                transform.localScale = Vector3.one * (1f - .4f * t);
        }

        private static Sprite pixel;
        private static Sprite Pixel
        {
            get
            {
                if (pixel != null) return pixel;
                var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                return pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 48f);
            }
        }

        /// <summary>Day 24: watches the boss for the heart opening and the phase change.</summary>
        private sealed class BossWatch : MonoBehaviour
        {
            public SKBossAgent boss;
            private bool wasOpen;
            private int phase = 1;

            private void Update()
            {
                if (boss.CoreOpen && !wasOpen) Spawn(new Color(1f, .45f, .55f), Kind.Heart, Centre(boss.Health) + Vector2.down * .2f);
                wasOpen = boss.CoreOpen;
                if (boss.Phase != phase)
                {
                    phase = boss.Phase;
                    if (phase == 2)
                    {
                        Spawn(new Color(.95f, .3f, .4f), Kind.Phase, Centre(boss.Health));
                        SKScreenShake.Kick(.12f, .5f);
                    }
                }
            }
        }
    }

    /// <summary>Vertical-only camera shake (x stays exactly where the follow camera puts it).</summary>
    public static class SKScreenShake
    {
        public static float Until { get; private set; } = float.NegativeInfinity;
        private static float amplitude;

        public static void Kick(float amp, float seconds)
        {
            amplitude = Mathf.Max(Time.time < Until ? amplitude : 0f, amp);
            Until = Mathf.Max(Until, Time.time + seconds);
        }

        /// <summary>Offset to add to the camera's resting y this frame.</summary>
        public static float Offset => Time.time < Until
            ? Mathf.Round(Mathf.Sin(Time.time * 90f) * amplitude * 48f) / 48f : 0f;
    }
}
