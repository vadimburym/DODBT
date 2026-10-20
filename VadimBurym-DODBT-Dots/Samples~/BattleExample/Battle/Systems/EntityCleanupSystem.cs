using DODBT.Example.Battle.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using DODBT.Example.Setup;

namespace DODBT.Example.Battle.Systems
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(BattleSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(TakeDamageSystem))]
    public partial struct EntityCleanupSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            using var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (_, entity) in SystemAPI.Query<EnabledRefRO<CleanupTag>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
            }
            ecb.Playback(state.EntityManager);
        }
    }
}
