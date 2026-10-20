using DODBT.Example.Battle.Grid;
using System;
using DODBT.Example.BehaviourTree;
using DODBT.Example.Battle.Components;
using VadimBurym.DodBehaviourTree.Dots;
using VadimBurym.DodBehaviourTree.Dots.Generated;
using DODBT.Example.Battle.Animation;

namespace DODBT.Example.BehaviourTree.Leaves
{
    [Serializable]
    [LeafCodeGen(6, typeof(DODBT.Example.BehaviourTree.ExampleContext))]
    public sealed class AttackEnemyLeaf : ILeaf
    {
        public LeafData GetCompiledData()
        {
            return new LeafData {
                LeafId = 6,
            };
        }

        public static NodeStatus OnTick(ref RunnerState_ExampleContext state)
        {
            var lookUp = state.Context.AttackStateLookup;
            if (!lookUp.HasComponent(state.LeafState.StateEntity))
                return NodeStatus.Running;
            if (!state.Context.EntityInfoLookup.Exists(lookUp[state.LeafState.StateEntity].Target))
                return NodeStatus.Failure;
            if (state.Context.EyeSensorLookup[state.Agent].DetectedEntity != lookUp[state.LeafState.StateEntity].Target)
                return NodeStatus.Failure;

            return NodeStatus.Running;
        }

        public static void OnEnter(ref RunnerState_ExampleContext state)
        {
            var context = state.Context;
            var agent = state.Agent;
            var sortKey = state.SortKey;
            var enemy = context.EyeSensorLookup[agent].DetectedEntity;

            var ecb = context.Ecb;
            var entity = ecb.CreateEntity(sortKey);
            ecb.AddComponent<AttackState>(sortKey, entity, new AttackState {
                Owner = agent,
                Target = enemy
            });
            ecb.AddComponent(sortKey, ecb.CreateEntity(sortKey), new LeafStateWriteRequest {
                Entity = agent,
                Index = state.LeafState.BufferIndex,
                Value = entity
            });
            ecb.SetComponentEnabled<SeeToDetectedTag>(sortKey, agent, true);

            AnimatorUtils.PlayAnimation(
                renderer: context.RenderEntityLookup[agent].Value,
                animationId: AnimationId.Attack,
                ecb: ecb,
                sortKey: sortKey);
        }

        public static void OnExit(ref RunnerState_ExampleContext state)
        {
            var ecb = state.Context.Ecb;
            ecb.DestroyEntity(state.SortKey, state.LeafState.StateEntity);
            ecb.SetComponentEnabled<SeeToDetectedTag>(state.SortKey, state.Agent, false);
        }

        public static void OnAbort(ref RunnerState_ExampleContext state)
        {
            var ecb = state.Context.Ecb;
            ecb.DestroyEntity(state.SortKey, state.LeafState.StateEntity);
            ecb.SetComponentEnabled<SeeToDetectedTag>(state.SortKey, state.Agent, false);
        }
    }
}
