using DODBT.Example.Battle.Grid;
using DODBT.Example.Battle.Components;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DODBT.Example.Battle.Sensing
{
    [BurstCompile]
    [DisableAutoCreation]
    [UpdateAfter(typeof(EyeSensorSystem))]
    public partial struct SeeToDetectedSystem : ISystem
    {
        private ComponentLookup<LocalToWorld> _localTransformLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _localTransformLookup = SystemAPI.GetComponentLookup<LocalToWorld>();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _localTransformLookup.Update(ref state);
            
            var job = new SeeToDetectedJob {
                DeltaTime = SystemAPI.Time.DeltaTime,
                LocalTransformLookup = _localTransformLookup
            };
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
        
        [BurstCompile]
        public partial struct SeeToDetectedJob : IJobEntity
        {
            public float DeltaTime;
            [ReadOnly] public ComponentLookup<LocalToWorld> LocalTransformLookup;
            
            [BurstCompile]
            public void Execute(
                ref LocalTransform localTransform,
                in EyeSensor eyeSensor,
                in MovementStats movement,
                in SeeToDetectedTag seeToDetectedTag)
            {
                var targetEntity = eyeSensor.DetectedEntity;
                if (!LocalTransformLookup.HasComponent(targetEntity))
                    return;
                var targetTransform = LocalTransformLookup[targetEntity];
                var toTarget = targetTransform.Position - localTransform.Position;
                toTarget.y = 0f;
                var dir = math.normalizesafe(toTarget, new float3(0f, 0f, 1f));
                var desiredRotation = quaternion.LookRotationSafe(dir, math.up());
                localTransform.Rotation = math.slerp(
                    localTransform.Rotation,
                    desiredRotation,
                    math.saturate(DeltaTime * movement.RotationSpeed) 
                );
            }
        }
    }
}
