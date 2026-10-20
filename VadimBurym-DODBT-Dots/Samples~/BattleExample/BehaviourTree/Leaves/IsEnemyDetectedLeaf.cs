using DODBT.Example.Battle.Grid;
using System;
using DODBT.Example.BehaviourTree;
using Unity.Burst;
using Unity.Entities;
using VadimBurym.DodBehaviourTree.Dots;
using VadimBurym.DodBehaviourTree.Dots.Generated;

namespace DODBT.Example.BehaviourTree.Leaves
{
    [Serializable]
    [LeafCodeGen(1, typeof(DODBT.Example.BehaviourTree.ExampleContext))]
    public sealed class IsEnemyDetectedLeaf : ILeaf
    {
        public LeafData GetCompiledData()
        {
            return new LeafData {
                LeafId = 1,
            };
        }

        public static NodeStatus OnTick(ref RunnerState_ExampleContext state)
        {
            var sensor = state.Context.EyeSensorLookup[state.Agent];
            bool isEntityExist = state.Context.EntityInfoLookup.Exists(sensor.DetectedEntity);
            return sensor.IsDetected == 1 && isEntityExist ? NodeStatus.Success : NodeStatus.Failure;
        }

        public static void OnEnter(ref RunnerState_ExampleContext state) { }
        public static void OnExit(ref RunnerState_ExampleContext state) { }
        public static void OnAbort(ref RunnerState_ExampleContext state) { }
    }
}
