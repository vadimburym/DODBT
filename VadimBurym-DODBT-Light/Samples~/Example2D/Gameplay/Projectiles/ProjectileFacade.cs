using System.Collections.Generic;
using DODBT.Light.Example.Gameplay.Scene;
using UnityEngine;

namespace DODBT.Light.Example.Gameplay.Projectiles
{
    public sealed class ProjectileFacade : MonoBehaviour
    {
        public EcsEntity EcsEntity;
        public EcsCollider EcsCollider;
        public Rigidbody2D Rigidbody;

        private Collider2D _collider;
        private readonly List<Collider2D> _ownerColliders = new(4);

        public void Launch(Vector2 position, Vector2 velocity, Rigidbody2D owner)
        {
            RestoreOwnerCollisions();
            if (_collider == null) _collider = GetComponent<Collider2D>();
            // Teleport the rendered transform too, before interpolation's next update.
            transform.position = position;
            Rigidbody.position = position;
            Rigidbody.linearVelocity = velocity;
            Rigidbody.angularVelocity = 0;

            // Apply this on every spawn: disabling a pooled collider resets ignored pairs.
            owner.GetComponentsInChildren(_ownerColliders);
            for (int i = _ownerColliders.Count - 1; i >= 0; i--)
            {
                var ownerCollider = _ownerColliders[i];
                if (ownerCollider.attachedRigidbody != owner)
                {
                    _ownerColliders.RemoveAt(i);
                    continue;
                }
                Physics2D.IgnoreCollision(_collider, ownerCollider, true);
            }
        }

        private void OnDisable()
        {
            RestoreOwnerCollisions();
            Rigidbody.linearVelocity = Vector2.zero;
            Rigidbody.angularVelocity = 0;
        }

        private void RestoreOwnerCollisions()
        {
            if (_collider != null)
                foreach (var ownerCollider in _ownerColliders)
                    if (ownerCollider != null)
                        Physics2D.IgnoreCollision(_collider, ownerCollider, false);
            _ownerColliders.Clear();
        }
    }
}
