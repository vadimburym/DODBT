using System.Runtime.CompilerServices;
using DODBT.Example;
using Unity.Collections;
using Unity.Entities;

namespace DODBT.Example.Battle.Animation
{
    public static class AnimatorUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PlayAnimation(
            Entity renderer,
            AnimationId animationId,
            EntityCommandBuffer.ParallelWriter ecb,
            int sortKey,
            float startNormalizedTime = 0f,
            float transitionDuration = 0.25f,
            byte restartIfSame = 0,
            byte oneShot = 0)
        {
            ecb.SetComponent(sortKey, renderer, new VATAnimationCommand {
                RequestedAnimationIndex = (byte)animationId,
                StartNormalizedTime = startNormalizedTime,
                TransitionDuration = transitionDuration,
                RestartIfSame = restartIfSame,
                IsLoop = oneShot == 0 ? (byte)1 : (byte)0
            });
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PlayAnimation(
            Entity renderer,
            AnimationId animationId,
            EntityCommandBuffer ecb,
            float startNormalizedTime = 0f,
            float transitionDuration = 0.25f,
            byte restartIfSame = 0,
            byte oneShot = 0)
        {
            ecb.SetComponent(renderer, new VATAnimationCommand {
                RequestedAnimationIndex = (byte)animationId,
                StartNormalizedTime = startNormalizedTime,
                TransitionDuration = transitionDuration,
                RestartIfSame = restartIfSame,
                IsLoop = oneShot == 0 ? (byte)1 : (byte)0
            });
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PlayAnimation(
            ComponentLookup<VATAnimationCommand> animationLookup,
            Entity renderer,
            AnimationId animationId,
            float startNormalizedTime = 0f,
            float transitionDuration = 0.25f,
            byte restartIfSame = 0,
            byte oneShot = 0)
        {
            animationLookup[renderer] = new VATAnimationCommand {
                RequestedAnimationIndex = (byte)animationId,
                StartNormalizedTime = startNormalizedTime,
                TransitionDuration = transitionDuration,
                RestartIfSame = restartIfSame,
                IsLoop = oneShot == 0 ? (byte)1 : (byte)0
            };
        }
    }
}
