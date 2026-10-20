using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace DODBT.Example.Battle.Animation
{
    [BurstCompile]
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct VATAnimationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VATAnimator>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new VATAnimationUpdateJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
            };
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct VATAnimationUpdateJob : IJobEntity
        {
            public float DeltaTime;

            private void Execute(
                ref VATAnimator animator,
                ref VATAnimationCommand command,
                in VATLibraryBlobRef libraryRef)
            {
                ref var clips = ref libraryRef.Value.Value.Clips;
                var clipsLength = libraryRef.Value.Value.ClipsLength;
                var validClip = command.RequestedAnimationIndex >= 0 && command.RequestedAnimationIndex < clipsLength;
                
                if (validClip)
                {
                    int requestedClip = command.RequestedAnimationIndex + clipsLength * animator.MeshIndex;
                    bool sameClip = animator.CurrentClipIndex == requestedClip;
                    if (!sameClip || command.RestartIfSame != 0)
                    {
                        animator.PreviousClipIndex = animator.CurrentClipIndex;
                        animator.PreviousNormalizedTime = animator.CurrentNormalizedTime;
                        animator.CurrentClipIndex = requestedClip;
                        animator.CurrentNormalizedTime = math.saturate(command.StartNormalizedTime);
                        animator.BlendElapsed = 0f;
                        animator.BlendDuration = command.TransitionDuration >= 0f ? command.TransitionDuration : animator.DefaultTransitionDuration;
                        animator.BlendDuration = math.max(0f, animator.BlendDuration);
                        animator.Blend01 = animator.BlendDuration <= 0f ? 1f : 0f;
                        animator.Loop = command.IsLoop;
                    }
                    command.RequestedAnimationIndex = -1;
                    command.TransitionDuration = -1f;
                    command.StartNormalizedTime = 0f;
                    command.RestartIfSame = 0;
                }

                if (animator.Playing != 0)
                {
                    int totalClipsCount = clips.Length;
                    if ((uint)animator.CurrentClipIndex < (uint)totalClipsCount)
                    {
                        float currentLength = math.max(clips[animator.CurrentClipIndex].Length, 1e-5f);
                        animator.CurrentNormalizedTime = AdvanceTime(animator.CurrentNormalizedTime, currentLength, DeltaTime, animator.Speed, animator.Loop != 0);
                    }

                    if (animator.PreviousClipIndex >= 0 && (uint)animator.PreviousClipIndex < (uint)totalClipsCount)
                    {
                        float previousLength = math.max(clips[animator.PreviousClipIndex].Length, 1e-5f);
                        animator.PreviousNormalizedTime = AdvanceTime(animator.PreviousNormalizedTime, previousLength, DeltaTime, animator.Speed, animator.Loop != 0);
                    }
                }

                if (animator.PreviousClipIndex >= 0)
                {
                    if (animator.BlendDuration <= 0f)
                    {
                        animator.Blend01 = 1f;
                        animator.PreviousClipIndex = -1;
                        animator.PreviousNormalizedTime = 0f;
                    }
                    else
                    {
                        animator.BlendElapsed += math.max(0f, DeltaTime);
                        animator.Blend01 = math.saturate(animator.BlendElapsed / animator.BlendDuration);
                        if (animator.Blend01 >= 0.9999f)
                        {
                            animator.Blend01 = 1f;
                            animator.PreviousClipIndex = -1;
                            animator.PreviousNormalizedTime = 0f;
                        }
                    }
                }
                else
                {
                    animator.Blend01 = 1f;
                }
            }

            private static float AdvanceTime(float current01, float clipLength, float deltaTime, float speed, bool loop)
            {
                if (clipLength <= 0f)
                    return current01;

                float next = current01 + (deltaTime * speed) / clipLength;
                return loop ? math.frac(next) : math.clamp(next, 0f, 0.9f);;
                //return loop ? math.frac(next) : math.saturate(next);
            }
        }
    }
}
