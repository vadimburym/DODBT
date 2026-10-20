using DODBT.Example.Battle.Grid;
using DODBT.Example.Battle.Components;
using DODBT.Example.Battle.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DODBT.Example.BehaviourTree.Systems
{
    [BurstCompile]
    [DisableAutoCreation]
    [UpdateAfter(typeof(EntityCleanupSystem))]
    public partial struct ChaseStateSystem : ISystem
    {
        private ComponentLookup<GridNavigationState> _gridLookup;
        private ComponentLookup<TargetPosition> _targetPositionLookup;
        private ComponentLookup<AttackStats> _attackStatsLookup;
        private ComponentLookup<Footprint> _footprintLookup;
        private ComponentLookup<LocalTransform> _transformLookup;

        private EntityStorageInfoLookup _entityStorageInfoLookup;
        
        private const float DefaultStoppingRadius = 0.1f;
        private const int MaxOuterSearchRings = 64;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BattlefieldGridSingleton>();
            state.RequireForUpdate<GridRuntimeMapSingleton>();

            _gridLookup = state.GetComponentLookup<GridNavigationState>(false);
            _targetPositionLookup = state.GetComponentLookup<TargetPosition>(false);
            _attackStatsLookup = state.GetComponentLookup<AttackStats>(true);
            _footprintLookup = state.GetComponentLookup<Footprint>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);

            _entityStorageInfoLookup = state.GetEntityStorageInfoLookup();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _gridLookup.Update(ref state);
            _targetPositionLookup.Update(ref state);
            _attackStatsLookup.Update(ref state);
            _footprintLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _entityStorageInfoLookup.Update(ref state);
            
            var gridRef = SystemAPI.GetSingleton<BattlefieldGridSingleton>().Value;
            var mapsRw = SystemAPI.GetSingletonRW<GridRuntimeMapSingleton>();

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            state.Dependency = new ChaseStateJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                GridRef = gridRef,
                OccupiedMap = mapsRw.ValueRW.OccupiedMap,
                GridLookup = _gridLookup,
                TargetPositionLookup = _targetPositionLookup,
                AttackStatsLookup = _attackStatsLookup,
                FootprintLookup = _footprintLookup,
                TransformLookup = _transformLookup,
                Ecb = ecb,
                EntityInfoLookup = _entityStorageInfoLookup
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        public partial struct ChaseStateJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer Ecb;
            [ReadOnly] public EntityStorageInfoLookup EntityInfoLookup;
            
            [ReadOnly] public BlobAssetReference<BattlefieldGridBlob> GridRef;
            public NativeParallelHashMap<int2, Entity> OccupiedMap;

            public ComponentLookup<GridNavigationState> GridLookup;
            public ComponentLookup<TargetPosition> TargetPositionLookup;

            [ReadOnly] public ComponentLookup<AttackStats> AttackStatsLookup;
            [ReadOnly] public ComponentLookup<Footprint> FootprintLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

            [BurstCompile]
            private void Execute(ref ChaseState chaseState, Entity stateEntity)
            {
                var owner = chaseState.Owner;
                var target = chaseState.Target;

                if (!GridLookup.HasComponent(owner))
                {
                    Ecb.DestroyEntity(stateEntity);
                    return;
                }
                if (!GridLookup.HasComponent(target))
                    return;
                
                chaseState.UpdateTimer -= DeltaTime;
                if (chaseState.UpdateTimer > 0f)
                    return;

                chaseState.UpdateTimer += chaseState.UpdateInterval;

                var ownerGrid = GridLookup[owner];
                var targetGrid = GridLookup[target];

                if (ownerGrid.HasOccupiedCell == 0)
                    return;

                int range = math.max(1, AttackStatsLookup[owner].AttackRangeCells);

                int currentDistance = BattlefieldGridUtils.CellDistanceChebyshev(
                    ownerGrid.OccupiedCell,
                    targetGrid.MovingCell);

                if (currentDistance <= range)
                    return;

                var footprint = FootprintLookup[owner];
                int footprintX = math.max(1, footprint.FootprintX);
                int footprintY = math.max(1, footprint.FootprintY);

                float3 ownerPos3 = TransformLookup[owner].Position;
                float3 targetPos3 = TransformLookup[target].Position;

                float2 ownerPosXZ = new float2(ownerPos3.x, ownerPos3.z);
                float2 targetPosXZ = new float2(targetPos3.x, targetPos3.z);

                ref var grid = ref GridRef.Value;

                if (!TryFindBestChaseCell(
                        ref grid,
                        OccupiedMap,
                        owner,
                        ownerGrid.OccupiedCell,
                        targetGrid.MovingCell,
                        ownerPosXZ,
                        targetPosXZ,
                        range,
                        footprintX,
                        footprintY,
                        out int2 desiredCell))
                {
                    return;
                }

                if (math.all(ownerGrid.OccupiedCell == desiredCell))
                    return;

                BattlefieldGridUtils.ReleaseAreaDirect(
                    OccupiedMap,
                    owner,
                    ownerGrid.OccupiedCell,
                    footprintX,
                    footprintY);

                BattlefieldGridUtils.OccupyAreaDirect(
                    OccupiedMap,
                    owner,
                    desiredCell,
                    footprintX,
                    footprintY);

                ownerGrid.OccupiedCell = desiredCell;
                ownerGrid.HasOccupiedCell = 1;
                GridLookup[owner] = ownerGrid;

                TargetPositionLookup[owner] = new TargetPosition
                {
                    Position = BattlefieldGridUtils.FootprintToWorldCenter(
                        ref grid,
                        desiredCell,
                        footprintX,
                        footprintY,
                        ownerPos3.y),
                    StoppingRadius = DefaultStoppingRadius
                };
            }

            [BurstCompile]
            private bool TryFindBestChaseCell(
                ref BattlefieldGridBlob grid,
                NativeParallelHashMap<int2, Entity> occupiedMap,
                Entity self,
                int2 ownerOccupiedCell,
                int2 targetMovingCell,
                float2 ownerPosXZ,
                float2 targetPosXZ,
                int attackRange,
                int footprintX,
                int footprintY,
                out int2 foundCell)
            {
                foundCell = default;

                float2 targetToOwnerDir = ownerPosXZ - targetPosXZ;
                if (math.lengthsq(targetToOwnerDir) < 0.0001f)
                {
                    float2 fallback = (float2)ownerOccupiedCell - (float2)targetMovingCell;
                    if (math.lengthsq(fallback) < 0.0001f)
                        fallback = new float2(0f, 1f);

                    targetToOwnerDir = math.normalizesafe(fallback, new float2(0f, 1f));
                }
                else
                {
                    targetToOwnerDir = math.normalize(targetToOwnerDir);
                }

                for (int ring = attackRange; ring >= 1; ring--)
                {
                    if (TryFindFrontCellOnRing(
                            ref grid,
                            occupiedMap,
                            self,
                            targetMovingCell,
                            targetToOwnerDir,
                            ring,
                            footprintX,
                            footprintY,
                            out foundCell))
                    {
                        return true;
                    }
                }

                for (int ring = attackRange + 1; ring <= attackRange + MaxOuterSearchRings; ring++)
                {
                    if (TryFindFrontCellOnRing(
                            ref grid,
                            occupiedMap,
                            self,
                            targetMovingCell,
                            targetToOwnerDir,
                            ring,
                            footprintX,
                            footprintY,
                            out foundCell))
                    {
                        return true;
                    }
                }

                return false;
            }

            [BurstCompile]
            private bool TryFindFrontCellOnRing(
                ref BattlefieldGridBlob grid,
                NativeParallelHashMap<int2, Entity> occupiedMap,
                Entity self,
                int2 targetMovingCell,
                float2 targetToOwnerDir,
                int ring,
                int footprintX,
                int footprintY,
                out int2 foundCell)
            {
                foundCell = default;

                bool found = false;
                int2 bestCell = default;
                float bestAlignment = -2f;
                float bestPreferredDistSq = float.MaxValue;

                int x = -ring;
                int y = -ring;

                for (; x < ring; x++)
                {
                    EvaluateFrontCell(
                        ref grid, occupiedMap, self,
                        targetMovingCell + new int2(x, y),
                        targetMovingCell,
                        targetToOwnerDir,
                        ring,
                        footprintX,
                        footprintY,
                        ref found,
                        ref bestCell,
                        ref bestAlignment,
                        ref bestPreferredDistSq);
                }

                for (; y < ring; y++)
                {
                    EvaluateFrontCell(
                        ref grid, occupiedMap, self,
                        targetMovingCell + new int2(x, y),
                        targetMovingCell,
                        targetToOwnerDir,
                        ring,
                        footprintX,
                        footprintY,
                        ref found,
                        ref bestCell,
                        ref bestAlignment,
                        ref bestPreferredDistSq);
                }

                for (; x > -ring; x--)
                {
                    EvaluateFrontCell(
                        ref grid, occupiedMap, self,
                        targetMovingCell + new int2(x, y),
                        targetMovingCell,
                        targetToOwnerDir,
                        ring,
                        footprintX,
                        footprintY,
                        ref found,
                        ref bestCell,
                        ref bestAlignment,
                        ref bestPreferredDistSq);
                }

                for (; y > -ring; y--)
                {
                    EvaluateFrontCell(
                        ref grid, occupiedMap, self,
                        targetMovingCell + new int2(x, y),
                        targetMovingCell,
                        targetToOwnerDir,
                        ring,
                        footprintX,
                        footprintY,
                        ref found,
                        ref bestCell,
                        ref bestAlignment,
                        ref bestPreferredDistSq);
                }

                if (found)
                {
                    foundCell = bestCell;
                    return true;
                }

                return false;
            }

            [BurstCompile]
            private void EvaluateFrontCell(
                ref BattlefieldGridBlob grid,
                NativeParallelHashMap<int2, Entity> occupiedMap,
                Entity self,
                int2 candidateCell,
                int2 targetMovingCell,
                float2 targetToOwnerDir,
                int ring,
                int footprintX,
                int footprintY,
                ref bool found,
                ref int2 bestCell,
                ref float bestAlignment,
                ref float bestPreferredDistSq)
            {
                if (!BattlefieldGridUtils.IsAreaFree(ref grid, occupiedMap, candidateCell, footprintX, footprintY, self))
                    return;
                
                float2 offset = (float2)candidateCell - (float2)targetMovingCell;
                float offsetLenSq = math.lengthsq(offset);
                if (offsetLenSq < 0.0001f)
                    return;

                float2 offsetDir = offset / math.sqrt(offsetLenSq);
                float alignment = math.dot(offsetDir, targetToOwnerDir);

                if (alignment <= 0f)
                    return;

                float2 preferredPoint = (float2)targetMovingCell + targetToOwnerDir * ring;
                float preferredDistSq = math.lengthsq((float2)candidateCell - preferredPoint);

                if (!found ||
                    alignment > bestAlignment + 0.0001f ||
                    (math.abs(alignment - bestAlignment) <= 0.0001f && preferredDistSq < bestPreferredDistSq))
                {
                    bestAlignment = alignment;
                    bestPreferredDistSq = preferredDistSq;
                    bestCell = candidateCell;
                    found = true;
                }
            }
        }
    }
}
