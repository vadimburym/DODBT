using DODBT.Light.Example.Gameplay.Components;
using DODBT.Light.Example.Gameplay.Settings;
using DODBT.Light.Example.Services;
using Leopotam.EcsLite;
using UnityEngine;

namespace DODBT.Light.Example.Gameplay.Scene
{
    public sealed class EcsCollider : MonoBehaviour
    {
        [SerializeField] private EcsEntity _entity;
        private EcsWorld _eventWorld;

        public void Construct(EcsWorld eventWorld)
        {
            _eventWorld = eventWorld;
        }
        
        private void OnCollisionEnter2D(Collision2D collision)
        {
            var entity = _eventWorld.NewEntity();
            _eventWorld.GetPool<CollisionEvent>().Add(entity).Setup(
                entity: _entity.Index,
                collisionRef: collision.collider.gameObject);
            _eventWorld.GetPool<OneFrameTag>().Add(entity);
        }
    }
}
