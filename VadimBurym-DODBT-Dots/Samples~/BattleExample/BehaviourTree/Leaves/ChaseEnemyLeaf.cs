using DODBT.Example.Battle.Grid;
using System;
using DODBT.Example.BehaviourTree;
using DODBT.Example.Battle.Components;
using Unity.Entities;
using VadimBurym.DodBehaviourTree.Dots;
using VadimBurym.DodBehaviourTree.Dots.Generated;
using DODBT.Example.Battle.Animation;

namespace DODBT.Example.BehaviourTree.Leaves
{
    [Serializable]
    [LeafCodeGen(4, typeof(DODBT.Example.BehaviourTree.ExampleContext))]
    public sealed class ChaseEnemyLeaf : ILeaf
    {
        public LeafData GetCompiledData()
        {
            return new LeafData {
                LeafId = 4,
            };
        }

        public static NodeStatus OnTick(ref RunnerState_ExampleContext state)
        {
            if (state.Context.IsMovingTagLookup[state.Agent].IsMoving == 0)
            {
                AnimatorUtils.PlayAnimation(
                    renderer: state.Context.RenderEntityLookup[state.Agent].Value,
                    animationId: AnimationId.Idle,
                    ecb: state.Context.Ecb,
                    sortKey: state.SortKey);
            }
            return NodeStatus.Running;
        }

        public static void OnEnter(ref RunnerState_ExampleContext state)
        {
            var context = state.Context;
            var agent = state.Agent;
            var sortKey = state.SortKey;
            var enemy = context.EyeSensorLookup[agent].DetectedEntity;

            var ecb = context.Ecb;
            var random = state.Random;
            var entity = ecb.CreateEntity(sortKey);
            ecb.AddComponent<ChaseState>(sortKey, entity, new ChaseState {
                Owner = agent,
                Target = enemy,
                UpdateInterval = 0.5f,
                UpdateTimer = random.NextFloat(0, 0.5f)
            });
            ecb.AddComponent(sortKey, ecb.CreateEntity(sortKey), new LeafStateWriteRequest {
                Entity = agent,
                Index = state.LeafState.BufferIndex,
                Value = entity
            });
        }

        public static void OnExit(ref RunnerState_ExampleContext state)
        {
            var ecb = state.Context.Ecb;
            ecb.DestroyEntity(state.SortKey, state.LeafState.StateEntity);
        }

        public static void OnAbort(ref RunnerState_ExampleContext state)
        {
            var ecb = state.Context.Ecb;
            ecb.DestroyEntity(state.SortKey, state.LeafState.StateEntity);
        }
    }
}
