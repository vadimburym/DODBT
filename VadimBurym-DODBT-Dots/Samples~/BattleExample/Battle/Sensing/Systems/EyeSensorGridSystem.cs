using DODBT.Example.Battle.Grid;
using DODBT.Example.Battle.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DODBT.Example.Battle.Sensing
{
    [BurstCompile]
    [DisableAutoCreation]
    public partial struct EyeSensorGridSystem : ISystem
    {
        public const float CELL_SIZE = 4f;
        
        public void OnCreate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonEntity<EyeSensorGridSingleton>(out _))
            {
                var e = state.EntityManager.CreateEntity(typeof(EyeSensorGridSingleton));
                state.EntityManager.SetComponentData(e, new EyeSensorGridSingleton {
                    Command0Grid = new NativeParallelMultiHashMap<int2, Entity>(32768, Allocator.Persistent),
                    Command1Grid = new NativeParallelMultiHashMap<int2, Entity>(32768, Allocator.Persistent),
                    CellSize = CELL_SIZE
                });
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonRW<EyeSensorGridSingleton>(out var mapRw))
                return;
            if (mapRw.ValueRO.Command0Grid.IsCreated)
                mapRw.ValueRW.Command0Grid.Dispose();
            if (mapRw.ValueRO.Command1Grid.IsCreated)
                mapRw.ValueRW.Command1Grid.Dispose();
            if (SystemAPI.TryGetSingletonEntity<EyeSensorGridSingleton>(out var e))
                state.EntityManager.DestroyEntity(e);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gridRw = SystemAPI.GetSingletonRW<EyeSensorGridSingleton>();

            int unitCount = SystemAPI.QueryBuilder()
                .WithAll<
                    LocalTransform,
                    Team>()
                .Build()
                .CalculateEntityCount();

            int desiredCapacity = math.max(1024, unitCount);
            if (gridRw.ValueRO.Command0Grid.Capacity < desiredCapacity)
                gridRw.ValueRW.Command0Grid.Capacity = desiredCapacity;
            if (gridRw.ValueRO.Command1Grid.Capacity < desiredCapacity)
                gridRw.ValueRW.Command1Grid.Capacity = desiredCapacity;

            gridRw.ValueRW.Command0Grid.Clear();
            gridRw.ValueRW.Command1Grid.Clear();

            state.Dependency = new EyeSensorGridJob {
                CellSize = gridRw.ValueRO.CellSize,
                Command0Writer = gridRw.ValueRW.Command0Grid.AsParallelWriter(),
                Command1Writer = gridRw.ValueRW.Command1Grid.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct EyeSensorGridJob : IJobEntity
        {
            public float CellSize;
            public NativeParallelMultiHashMap<int2, Entity>.ParallelWriter Command0Writer;
            public NativeParallelMultiHashMap<int2, Entity>.ParallelWriter Command1Writer;
            
            private void Execute(
                Entity entity,
                in LocalTransform transform,
                in Team team)
            {
                var position = transform.Position;
                var bucket = (int2)math.floor(new float2(position.x, position.z) / CellSize);
                switch (team.Value)
                {
                    case 0:
                        Command0Writer.Add(bucket, entity);
                        break;
                    case 1:
                        Command1Writer.Add(bucket, entity);
                        break;
                }
            }
        }
    }
}
