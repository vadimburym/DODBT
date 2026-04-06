using _ExampleProject.Code.Scene;
using _Project.Code.Core.Abstractions.Contracts;
using _Project.Code.Features.Test;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using UnityEngine;

namespace _ExampleProject.Code.Features._Core.Systems
{
    public sealed class CameraFollowSystem : ILateTick, IInit
    {
        private EcsFilter _filter;
        private EcsPool<UnityTransform> _transformPool;
        private CameraProvider _cameraProvider;
        
        private Vector3 _smoothVelocity;
        
        void IInit.Init()
        {
            var world = EcsWorlds.GetWorld(EcsWorlds.DEFAULT);
            _transformPool = world.GetPool<UnityTransform>();
            _filter = world.Filter<UnityRigidbody>().Inc<CameraFollowTag>().End();
            _cameraProvider = ServiceLocator.Resolve<CameraProvider>();
        }
        
        void ILateTick.LateTick()
        {
            foreach (var entity in _filter)
            {
                ref var transform = ref _transformPool.Get(entity).Ref;
                var targetPosition = transform.position;
                var camera = _cameraProvider.Camera.transform;
                var cameraPosition = camera.position;
                
                var half = _cameraProvider.DeadZoneSize * 0.5f;
                float dx = targetPosition.x - cameraPosition.x;
                float dy = targetPosition.y - cameraPosition.y;

                float shiftX = 0f;
                if (dx > half.x) shiftX = dx - half.x;
                else if (dx < -half.x) shiftX = dx + half.x;

                float shiftY = 0f;
                if (dy > half.y) shiftY = dy - half.y;
                else if (dy < -half.y) shiftY = dy + half.y;
                var desired = new Vector3(cameraPosition.x + shiftX, cameraPosition.y + shiftY, cameraPosition.z);
                
                if (_cameraProvider.IsSmoothing)
                {
                    camera.position = Vector3.SmoothDamp(
                        cameraPosition,
                        desired,
                        ref _smoothVelocity,
                        _cameraProvider.SmoothTime,
                        _cameraProvider.MaxSpeed,
                        Time.deltaTime
                    );
                }
                else
                {
                    camera.position = desired;
                    _smoothVelocity = Vector3.zero;
                }
                return;
            }
        }
    }
}