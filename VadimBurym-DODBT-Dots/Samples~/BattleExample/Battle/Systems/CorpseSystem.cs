using DODBT.Example.Battle.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace DODBT.Example.Battle.Systems
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(TakeDamageSystem))]
    public partial struct CorpseSystem : ISystem
    {
        private const float Lifetime = 45f;
        private const float SinkDelay = 5f;
        private const float SinkSpeed = 0.05f;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (corpse, transform, entity) in SystemAPI
                         .Query<RefRW<CorpseTag>, RefRW<LocalTransform>>()
                         .WithEntityAccess())
            {
                corpse.ValueRW.Time += dt;
                if (corpse.ValueRO.Time >= SinkDelay)
                {
                    var localTransform = transform.ValueRO;
                    localTransform.Position.y -= SinkSpeed * dt;
                    transform.ValueRW = localTransform;
                }
                if (corpse.ValueRO.Time >= Lifetime)
                {
                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}
