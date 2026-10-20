using DODBT.Example.Battle.Grid;
using DODBT.Example.Battle.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DODBT.Example.Battle.Systems
{
    [DisableAutoCreation]
    [BurstCompile]
    public partial struct MoveCommandSystem : ISystem
    {
        private const float DefaultStoppingRadius = 0.1f;

        private const int SlotSpacingCells = 1;
        private const int RowSpacingCells = 1;
        private const int MaxValidationRings = 128;
        private const float PreserveShapeThreshold = 6f;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattlefieldGridSingleton>();
            state.RequireForUpdate<GridRuntimeMapSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var entityManager = state.EntityManager;
            ref var gridBlob = ref SystemAPI.GetSingleton<BattlefieldGridSingleton>().Value.Value;
            ref var occupiedMap = ref SystemAPI.GetSingletonRW<GridRuntimeMapSingleton>().ValueRW.OccupiedMap;
            var requestsToDestroy = new NativeList<Entity>(Allocator.Temp);
            
            foreach (var (
                         request,
                         targets,
                         requestEntity) in
                     SystemAPI.Query<
                             RefRO<MoveCommandRequest>,
                             DynamicBuffer<MoveCommandTarget>>()
                         .WithEntityAccess())
            {
                int requestedCount = targets.Length;

                if (requestedCount == 0)
                {
                    requestsToDestroy.Add(requestEntity);
                    continue;
                }

                var entities = new NativeArray<Entity>(requestedCount, Allocator.Temp);
                var transforms = new NativeArray<LocalTransform>(requestedCount, Allocator.Temp);
                var targetPositions = new NativeArray<TargetPosition>(requestedCount, Allocator.Temp);
                var footprints = new NativeArray<Footprint>(requestedCount, Allocator.Temp);
                var gridStates = new NativeArray<GridNavigationState>(requestedCount, Allocator.Temp);

                int validCount = 0;

                for (int i = 0; i < targets.Length; i++)
                {
                    var entity = targets[i].Value;

                    if (!entityManager.Exists(entity)) continue;
                    if (!entityManager.HasComponent<LocalTransform>(entity)) continue;
                    if (!entityManager.HasComponent<TargetPosition>(entity)) continue;
                    if (!entityManager.HasComponent<Footprint>(entity)) continue;
                    if (!entityManager.HasComponent<GridNavigationState>(entity)) continue;
                    
                    entities[validCount] = entity;
                    transforms[validCount] = entityManager.GetComponentData<LocalTransform>(entity);
                    targetPositions[validCount] = entityManager.GetComponentData<TargetPosition>(entity);
                    footprints[validCount] = entityManager.GetComponentData<Footprint>(entity);
                    gridStates[validCount] = entityManager.GetComponentData<GridNavigationState>(entity);
                    
                    validCount++;
                }

                if (validCount == 0)
                {
                    entities.Dispose();
                    transforms.Dispose();
                    targetPositions.Dispose();
                    footprints.Dispose();
                    gridStates.Dispose();

                    requestsToDestroy.Add(requestEntity);
                    continue;
                }

                var destination = request.ValueRO.Destination;
                var destinationCell = BattlefieldGridUtils.WorldToCell(ref gridBlob, destination);

                var groupCenter = float3.zero;
                for (int i = 0; i < validCount; i++)
                    groupCenter += transforms[i].Position;
                groupCenter /= validCount;

                float selectionRadius = 0f;
                for (int i = 0; i < validCount; i++)
                {
                    float d = math.distance(groupCenter, transforms[i].Position);
                    selectionRadius = math.max(selectionRadius, d / gridBlob.CellSize);
                }

                for (int i = 0; i < validCount; i++)
                {
                    int footprintX = math.max(1, footprints[i].FootprintX);
                    int footprintY = math.max(1, footprints[i].FootprintY);

                    float preserveFactor =
                        selectionRadius <= PreserveShapeThreshold
                            ? math.saturate(1f - (selectionRadius / PreserveShapeThreshold))
                            : 0f;

                    var currentOffsetWorld = new float2(
                        transforms[i].Position.x - groupCenter.x,
                        transforms[i].Position.z - groupCenter.z);

                    var currentOffsetCells = currentOffsetWorld / gridBlob.CellSize;
                    var preferredOffsetCells = currentOffsetCells * preserveFactor;

                    var preferredCellPos =
                        new float2(destinationCell.x, destinationCell.y) + preferredOffsetCells;

                    if (!TryFindNearestValidOccupiedCellForUnit(
                            ref gridBlob,
                            occupiedMap,
                            entities[i],
                            destinationCell,
                            preferredCellPos,
                            footprintX,
                            footprintY,
                            out int2 newOccupiedCell))
                    {
                        continue;
                    }

                    var gs = gridStates[i];
                    bool hadOld = gs.HasOccupiedCell != 0;
                    int2 oldOccupiedCell = gs.OccupiedCell;

                    if (!hadOld || !math.all(oldOccupiedCell == newOccupiedCell))
                    {
                        if (hadOld)
                        {
                            BattlefieldGridUtils.ReleaseAreaDirect(
                                occupiedMap,
                                entities[i],
                                oldOccupiedCell,
                                footprintX,
                                footprintY);
                        }

                        BattlefieldGridUtils.OccupyAreaDirect(
                            occupiedMap,
                            entities[i],
                            newOccupiedCell,
                            footprintX,
                            footprintY);

                        gs.OccupiedCell = newOccupiedCell;
                        gs.HasOccupiedCell = 1;
                        gridStates[i] = gs;
                    }

                    float3 worldCenter = BattlefieldGridUtils.FootprintToWorldCenter(
                        ref gridBlob,
                        newOccupiedCell,
                        footprintX,
                        footprintY,
                        transforms[i].Position.y);

                    targetPositions[i] = new TargetPosition
                    {
                        Position = worldCenter,
                        StoppingRadius = DefaultStoppingRadius
                    };
                }

                for (int i = 0; i < validCount; i++)
                {
                    entityManager.SetComponentData(entities[i], targetPositions[i]);
                    entityManager.SetComponentData(entities[i], gridStates[i]);
                }

                entities.Dispose();
                transforms.Dispose();
                targetPositions.Dispose();
                footprints.Dispose();
                gridStates.Dispose();

                requestsToDestroy.Add(requestEntity);
            }

            for (int i = 0; i < requestsToDestroy.Length; i++)
            {
                if (entityManager.Exists(requestsToDestroy[i]))
                    entityManager.DestroyEntity(requestsToDestroy[i]);
            }

            requestsToDestroy.Dispose();
        }

        [BurstCompile]
        private bool TryFindNearestValidOccupiedCellForUnit(
            ref BattlefieldGridBlob grid,
            NativeParallelHashMap<int2, Entity> occupiedMap,
            Entity entity,
            int2 destinationCell,
            float2 preferredCellPos,
            int footprintX,
            int footprintY,
            out int2 occupiedCell)
        {
            occupiedCell = default;

            if (BattlefieldGridUtils.IsAreaFree(ref grid, occupiedMap, destinationCell, footprintX, footprintY, entity))
            {
                occupiedCell = destinationCell;
                return true;
            }
            
            for (int ring = 1; ring <= MaxValidationRings; ring++)
            {
                bool found = false;
                int2 bestCell = default;
                float bestDistSq = float.MaxValue;

                int x = -ring;
                int y = -ring;

                for (; x < ring; x++)
                {
                    EvaluateCellInRing(
                        ref grid,
                        occupiedMap,
                        entity,
                        destinationCell + new int2(x * SlotSpacingCells, y * RowSpacingCells),
                        preferredCellPos,
                        footprintX,
                        footprintY,
                        ref found,
                        ref bestCell,
                        ref bestDistSq);
                }

                for (; y < ring; y++)
                {
                    EvaluateCellInRing(
                        ref grid,
                        occupiedMap,
                        entity,
                        destinationCell + new int2(x * SlotSpacingCells, y * RowSpacingCells),
                        preferredCellPos,
                        footprintX,
                        footprintY,
                        ref found,
                        ref bestCell,
                        ref bestDistSq);
                }

                for (; x > -ring; x--)
                {
                    EvaluateCellInRing(
                        ref grid,
                        occupiedMap,
                        entity,
                        destinationCell + new int2(x * SlotSpacingCells, y * RowSpacingCells),
                        preferredCellPos,
                        footprintX,
                        footprintY,
                        ref found,
                        ref bestCell,
                        ref bestDistSq);
                }

                for (; y > -ring; y--)
                {
                    EvaluateCellInRing(
                        ref grid,
                        occupiedMap,
                        entity,
                        destinationCell + new int2(x * SlotSpacingCells, y * RowSpacingCells),
                        preferredCellPos,
                        footprintX,
                        footprintY,
                        ref found,
                        ref bestCell,
                        ref bestDistSq);
                }

                if (found)
                {
                    occupiedCell = bestCell;
                    return true;
                }
            }

            return false;
        }
        
        [BurstCompile]
        private void EvaluateCellInRing(
            ref BattlefieldGridBlob grid,
            NativeParallelHashMap<int2, Entity> occupiedMap,
            Entity entity,
            int2 candidateCell,
            float2 preferredCellPos,
            int footprintX,
            int footprintY,
            ref bool found,
            ref int2 bestCell,
            ref float bestDistSq)
        {
            if (!BattlefieldGridUtils.IsAreaFree(ref grid, occupiedMap, candidateCell, footprintX, footprintY, entity))
                return;
            float2 candidatePos = candidateCell;
            float d = math.lengthsq(preferredCellPos - candidatePos);

            if (d < bestDistSq)
            {
                bestDistSq = d;
                bestCell = candidateCell;
                found = true;
            }
        }
    }
}
