using DODBT.Example;
using DODBT.Example.BehaviourTree.Systems;
using DODBT.Example.Battle.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using DODBT.Example.Battle.Animation;

namespace DODBT.Example.Battle.Systems
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(BehaviourTreeTickSystem))]
    [UpdateAfter(typeof(MovementSystem))]
    public partial struct MovementAnimationSystem : ISystem
    {
        private ComponentLookup<VATAnimationCommand> _vatAnimationCommandLookup;
        
        public void OnCreate(ref SystemState state)
        {
            _vatAnimationCommandLookup = SystemAPI.GetComponentLookup<VATAnimationCommand>();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _vatAnimationCommandLookup.Update(ref state);
            var job = new MovementAnimationJob {
               VATAnimationCommandLookup = _vatAnimationCommandLookup
            };
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
        
        [BurstCompile]
        public partial struct MovementAnimationJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public ComponentLookup<VATAnimationCommand> VATAnimationCommandLookup;
            
            public void Execute(
                in IsMovingTag isMoving,
                in RendererEntityRef renderer)
            {
                if (isMoving.IsMoving == 0)
                    return;
                AnimatorUtils.PlayAnimation(
                    VATAnimationCommandLookup,
                    renderer.Value,
                    AnimationId.Run);
            }
        }
    }
}
