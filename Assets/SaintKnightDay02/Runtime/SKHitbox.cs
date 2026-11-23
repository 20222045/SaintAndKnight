using System.Collections.Generic;
using UnityEngine;

namespace SaintKnight.Day02
{
    [RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public sealed class SKHitbox : MonoBehaviour
    {
        [SerializeField] private SKHealth owner;
        [SerializeField, Range(0f, .1f)] private float hitStopSeconds = .045f;
        private readonly HashSet<SKHealth> hitThisSwing = new HashSet<SKHealth>();
        private readonly List<Collider2D> overlaps = new List<Collider2D>();
        private BoxCollider2D area;
        private SpriteRenderer visual;
        private float damage;
        private float knockback;
        private bool breaksArmor;
        private float interruptDelay;
        private float endsAt;
        public bool IsActive { get; private set; }
        /// <summary>Only an accepted hit, once per victim per swing, before that hit requests hit-stop.</summary>
        public event System.Action<SKHealth> HitAccepted;

        private void Awake()
        {
            if (owner == null) owner = GetComponentInParent<SKHealth>();
            area = GetComponent<BoxCollider2D>();
            visual = GetComponent<SpriteRenderer>();
            area.isTrigger = true;
            Cancel();
        }

        public void Begin(float facing, Vector2 size, float offset, float amount,
            float duration, float push, Color color, float height = 0f,
            bool cutsArmor = false, float tellDelay = 0f)
        {
            if (!isActiveAndEnabled || owner == null || !owner.isActiveAndEnabled ||
                owner.IsDead || owner.IsStunned || IsActive) return;
            SetShape(facing, size, offset, color, height);
            damage = amount;
            knockback = push;
            breaksArmor = cutsArmor;
            interruptDelay = tellDelay;
            endsAt = Time.time + duration;
            hitThisSwing.Clear();
            IsActive = true;
            area.enabled = visual.enabled = true;
        }

        // Reuse the existing training rectangle. A faint preview never enables damage.
        public void ShowWindup(float facing, Vector2 size, float offset, Color color)
        {
            if (!isActiveAndEnabled || IsActive) return;
            color.a *= .3f;
            SetShape(facing, size, offset, color);
            area.enabled = false;
            visual.enabled = true;
        }

        private void SetShape(float facing, Vector2 size, float offset, Color color, float height = 0f)
        {
            transform.localPosition = new Vector3(facing * offset, height, 0f);
            area.size = size;
            // Size the visual without scaling the hitbox collider with it.
            visual.drawMode = SpriteDrawMode.Sliced;
            visual.size = size;
            visual.color = color;
        }

        private void FixedUpdate()
        {
            if (!IsActive) return;
            if (owner == null || !owner.isActiveAndEnabled || owner.IsDead || owner.IsStunned || Time.time >= endsAt)
            {
                Cancel();
                return;
            }
            // Explicit trigger filter also works if the project's global query setting changes.
            var filter = new ContactFilter2D { useTriggers = true };
            area.Overlap(filter, overlaps);
            foreach (var hit in overlaps)
            {
                var hurtbox = hit.GetComponent<SKHurtbox>();
                var victim = hurtbox != null ? hurtbox.Owner : null;
                if (victim == null || victim == owner || victim.Team == owner.Team ||
                    !hitThisSwing.Add(victim)) continue;
                if (victim.TryTakeDamage(damage, owner.transform.position, knockback, breaksArmor, interruptDelay, owner))
                {
                    HitAccepted?.Invoke(victim);
                    SKHitStop.Request(hitStopSeconds, gameObject.scene);
                }
            }
        }

        public void Cancel()
        {
            IsActive = false;
            if (area != null) area.enabled = false;
            if (visual != null) visual.enabled = false;
        }

        private void OnDisable() => Cancel();
    }
}
