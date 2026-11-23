using UnityEngine;

namespace SaintKnight.Day02
{
    /// <summary>A trigger for receiving damage, separate from the solid movement collider.</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class SKHurtbox : MonoBehaviour
    {
        [SerializeField] private SKHealth owner;
        public SKHealth Owner => owner;

        private void Awake()
        {
            if (owner == null) owner = GetComponentInParent<SKHealth>();
            GetComponent<BoxCollider2D>().isTrigger = true;
        }
    }
}
