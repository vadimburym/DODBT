using DODBT.Example.Battle.Grid;
using DODBT.Example.Battle.Components;
using DODBT.Example.Battle.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DODBT.Example.BehaviourTree.Systems
{
    [BurstCompile]
    [DisableAutoCreation]
    [UpdateAfter(typeof(EntityCleanupSystem))]
    public partial struct AttackStateSystem : ISystem
    {
        private ComponentLookup<GridNavigationState> _gridLookup;
        private ComponentLookup<IsMovingTag> _movementLookup;
        private ComponentLookup<AttackStats> _attackStatsLookup;

        private EntityStorageInfoLookup _entityStorageInfoLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            _gridLookup = state.GetComponentLookup<GridNavigationState>(true);
            _movementLookup = state.GetComponentLookup<IsMovingTag>(true);
            _attackStatsLookup = state.GetComponentLookup<AttackStats>(true);
            _entityStorageInfoLookup = state.GetEntityStorageInfoLookup();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _gridLookup.Update(ref state);
            _movementLookup.Update(ref state);
            _attackStatsLookup.Update(ref state);
            _entityStorageInfoLookup.Update(ref state);
            
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            state.Dependency = new AttackStateJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Ecb = ecb,
                GridLookup = _gridLookup,
                MovementLookup = _movementLookup,
                AttackStatsLookup = _attackStatsLookup,
                EntityInfoLookup = _entityStorageInfoLookup
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct AttackStateJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public EntityStorageInfoLookup EntityInfoLookup;
            
            [ReadOnly] public ComponentLookup<GridNavigationState> GridLookup;
            [ReadOnly] public ComponentLookup<IsMovingTag> MovementLookup;
            [ReadOnly] public ComponentLookup<AttackStats> AttackStatsLookup;

            [BurstCompile]
            private void Execute(
                [ChunkIndexInQuery] int sortKey,
                Entity stateEntity,
                ref AttackState attackState)
            {
                attackState.RemainingTime = math.max(0f, attackState.RemainingTime - DeltaTime);
                if (attackState.RemainingTime > 0f)
                    return;
                var owner = attackState.Owner;
                var target = attackState.Target;

                if (!GridLookup.HasComponent(owner))
                {
                    Ecb.DestroyEntity(sortKey, stateEntity);
                    return;
                }
                if (!GridLookup.HasComponent(target))
                    return;
                
                var ownerGrid = GridLookup[owner];
                var targetGrid = GridLookup[target];
                
                if (MovementLookup[owner].IsMoving != 0)
                    return;
                int range = math.max(1, AttackStatsLookup[owner].AttackRangeCells);
                int dist = BattlefieldGridUtils.CellDistanceChebyshev(
                    ownerGrid.OccupiedCell,
                    targetGrid.MovingCell);
                if (dist > range)
                    return;
                
                attackState.RemainingTime = AttackStatsLookup[owner].AttackInterval;
                var damageRequest = Ecb.CreateEntity(sortKey);
                Ecb.AddComponent(sortKey, damageRequest, new TakeDamageRequest {
                    Source = target,
                    Damage = AttackStatsLookup[owner].Damage,
                });
            }
        }
    }
}
