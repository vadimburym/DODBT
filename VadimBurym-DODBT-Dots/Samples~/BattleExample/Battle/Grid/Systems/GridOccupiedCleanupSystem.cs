using DODBT.Example.Setup;
using DODBT.Example.Battle.Grid;
using DODBT.Example.Battle.Components;
using DODBT.Example.Battle.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;

namespace DODBT.Example.Battle.Grid
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(BattleSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(TakeDamageSystem))]
    [UpdateBefore(typeof(EntityCleanupSystem))]
    public partial struct GridOccupiedCleanupSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridRuntimeMapSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gridRef = SystemAPI.GetSingleton<GridRuntimeMapSingleton>();
            var occupiedMap = gridRef.OccupiedMap;

            foreach (var (navState, footprint, entity) in
                     SystemAPI.Query<
                         RefRO<GridNavigationState>,
                         RefRO<Footprint>>()
                         .WithEntityAccess()
                         .WithAll<CleanupTag>())
            {
                var footprintX = footprint.ValueRO.FootprintX;
                var footprintY = footprint.ValueRO.FootprintY;
                var sourceCell = navState.ValueRO.OccupiedCell;
                for (int x = 0; x < footprintX; x++)
                {
                    for (int y = 0; y < footprintY; y++)
                    {
                        if (occupiedMap.ContainsKey(sourceCell + new int2(x, y)))
                            occupiedMap.Remove(sourceCell + new int2(x, y));
                    }
                }
            }
        }
    }
}
