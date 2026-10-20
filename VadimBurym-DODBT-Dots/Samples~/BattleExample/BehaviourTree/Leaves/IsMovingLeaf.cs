using System;
using UnityEngine;
using VadimBurym.DodBehaviourTree.Dots;
using VadimBurym.DodBehaviourTree.Dots.Generated;

namespace DODBT.Example.BehaviourTree.Leaves
{
    [Serializable]
    [LeafCodeGen(5, typeof(DODBT.Example.BehaviourTree.ExampleContext))]
    public sealed class IsMovingLeaf : ILeaf
    {
        [SerializeField] private byte _isMoving;

        public LeafData GetCompiledData()
        {
            return new LeafData {
                LeafId = 5,
                Byte0 = _isMoving,
            };
        }

        public static NodeStatus OnTick(ref RunnerState_ExampleContext state)
        {
            return state.Context.IsMovingTagLookup[state.Agent].IsMoving == state.LeafData.Byte0 ? NodeStatus.Success : NodeStatus.Failure;
        }

        public static void OnEnter(ref RunnerState_ExampleContext state) { }
        public static void OnExit(ref RunnerState_ExampleContext state) { }
        public static void OnAbort(ref RunnerState_ExampleContext state) { }
    }
}
