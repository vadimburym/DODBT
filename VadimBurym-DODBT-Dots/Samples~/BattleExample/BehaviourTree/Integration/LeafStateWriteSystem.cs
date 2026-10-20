using DODBT.Example.BehaviourTree.Systems;
using DODBT.Example.Setup;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using VadimBurym.DodBehaviourTree.Dots;

namespace DODBT.Example.BehaviourTree
{
    [DisableAutoCreation]
    [BurstCompile]
    [UpdateBefore(typeof(BehaviourTreeTickSystem))]
    //[UpdateInGroup(typeof(BattleSystemGroup), OrderFirst = true)]
    public partial struct LeafStateWriteSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            using var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (
                         request,
                         entity) in
                     SystemAPI.Query<
                         RefRO<LeafStateWriteRequest>>()
                         .WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
                if (!SystemAPI.HasBuffer<LeafStateElement>(request.ValueRO.Entity))
                    continue;
                var buffer = SystemAPI.GetBuffer<LeafStateElement>(request.ValueRO.Entity);
                var index = request.ValueRO.Index;
                var data = buffer[index];
                data.StateEntity = request.ValueRO.Value;
                buffer[index] = data;
            }
            ecb.Playback(state.EntityManager);
        }
    }
}
