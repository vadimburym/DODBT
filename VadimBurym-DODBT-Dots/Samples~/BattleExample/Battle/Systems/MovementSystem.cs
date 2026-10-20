using DODBT.Example.Battle.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DODBT.Example.Battle.Systems
{
    [DisableAutoCreation]
    public partial struct MovementSystem : ISystem
    {
        private const float EPSILON = 0.01f;
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new MovementJob {
                DeltaTime = SystemAPI.Time.DeltaTime
            };
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
        
        [BurstCompile]
        public partial struct MovementJob : IJobEntity
        {
            public float DeltaTime;

            [BurstCompile]
            public void Execute(
                ref LocalTransform localTransform,
                in MovementStats movement,
                in TargetPosition targetPosition,
                ref IsMovingTag isMoving)
            {
                var toTarget = targetPosition.Position - localTransform.Position;
                toTarget.y = 0f;
                float stoppingRadius = math.max(0.05f, targetPosition.StoppingRadius);
                float stoppingRadiusSq = stoppingRadius * stoppingRadius;
                float distSq = math.lengthsq(toTarget);
                if (distSq <= stoppingRadiusSq + EPSILON)
                {
                    isMoving.IsMoving = 0;
                    return;
                }
                isMoving.IsMoving = 1;
                var dir = math.normalizesafe(toTarget, new float3(0f, 0f, 1f));
                var desiredRotation = quaternion.LookRotationSafe(dir, math.up());

                localTransform.Rotation = math.slerp(
                    localTransform.Rotation,
                    desiredRotation,
                    math.saturate(DeltaTime * movement.RotationSpeed));

                float distance = math.sqrt(distSq);
                float moveStep = movement.Speed * DeltaTime;
                float travel = math.min(moveStep, math.max(0f, distance - stoppingRadius));
                localTransform.Position += dir * travel;
            }
        }
    }
}
