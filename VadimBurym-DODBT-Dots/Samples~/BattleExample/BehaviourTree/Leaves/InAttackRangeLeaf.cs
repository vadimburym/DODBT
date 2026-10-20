using DODBT.Example.Battle.Grid;
using System;
using VadimBurym.DodBehaviourTree.Dots;
using VadimBurym.DodBehaviourTree.Dots.Generated;

namespace DODBT.Example.BehaviourTree.Leaves
{
    [Serializable]
    [LeafCodeGen(3, typeof(DODBT.Example.BehaviourTree.ExampleContext))]
    public sealed class InAttackRangeLeaf : ILeaf
    {
        public LeafData GetCompiledData()
        {
            return new LeafData {
                LeafId = 3,
            };
        }

        public static NodeStatus OnTick(ref RunnerState_ExampleContext state)
        {
            //TODO: now - GREEN, refactoring: вынести логику в сенсор
            var enemy = state.Context.EyeSensorLookup[state.Agent].DetectedEntity;
            if (!state.Context.GridNavigationStateLookup.HasComponent(enemy))
                return NodeStatus.Failure;
            var agentCell = state.Context.GridNavigationStateLookup[state.Agent].MovingCell;
            var enemyCell = state.Context.GridNavigationStateLookup[enemy].MovingCell;
            var threshold = state.Context.AttackStatsLookup[state.Agent].AttackRangeCells;
            return BattlefieldGridUtils.CellDistanceChebyshev(agentCell, enemyCell) <= threshold ? NodeStatus.Success : NodeStatus.Failure;
        }

        public static void OnEnter(ref RunnerState_ExampleContext state) { }
        public static void OnExit(ref RunnerState_ExampleContext state) { }
        public static void OnAbort(ref RunnerState_ExampleContext state) { }
    }
}
