using System;
using DODBT.Example.BehaviourTree;
using Unity.Entities;
using VadimBurym.DodBehaviourTree.Dots;
using VadimBurym.DodBehaviourTree.Dots.Generated;
using DODBT.Example.Battle.Animation;

namespace DODBT.Example.BehaviourTree.Leaves
{
    [Serializable]
    [LeafCodeGen(7, typeof(DODBT.Example.BehaviourTree.ExampleContext))]
    public sealed class IdleLeaf : ILeaf
    {
        public LeafData GetCompiledData()
        {
            return new LeafData {
                LeafId = 7,
            };
        }

        public static NodeStatus OnTick(ref RunnerState_ExampleContext state)
        {
            return NodeStatus.Running;
        }

        public static void OnEnter(ref RunnerState_ExampleContext state)
        {
            AnimatorUtils.PlayAnimation(
                renderer: state.Context.RenderEntityLookup[state.Agent].Value,
                animationId: AnimationId.Idle,
                ecb: state.Context.Ecb,
                sortKey: state.SortKey);
        }

        public static void OnExit(ref RunnerState_ExampleContext state) { }
        public static void OnAbort(ref RunnerState_ExampleContext state) { }
    }
}
