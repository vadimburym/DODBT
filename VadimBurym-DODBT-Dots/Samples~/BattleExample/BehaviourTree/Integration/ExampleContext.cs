using DODBT.Example.Battle.Grid;
using DODBT.Example.Battle.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using DODBT.Example.Battle.Animation;

namespace DODBT.Example.BehaviourTree
{
    [VadimBurym.DodBehaviourTree.Dots.GenerateBtRunner]
    public struct ExampleContext
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        public EntityStorageInfoLookup EntityInfoLookup;

        [ReadOnly] public ComponentLookup<EyeSensor> EyeSensorLookup;
        [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
        [ReadOnly] public ComponentLookup<AttackStats> AttackStatsLookup;
        [ReadOnly] public ComponentLookup<GridNavigationState> GridNavigationStateLookup;
        [ReadOnly] public ComponentLookup<IsMovingTag> IsMovingTagLookup;
        [ReadOnly] public ComponentLookup<RendererEntityRef> RenderEntityLookup;

        [ReadOnly] public ComponentLookup<AttackState> AttackStateLookup;
    }
}
