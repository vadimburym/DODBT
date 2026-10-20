using Unity.Entities;

namespace DODBT.Example.Battle.Animation
{
    public struct VATAnimationCommand : IComponentData
    {
        public int RequestedAnimationIndex;
        public float TransitionDuration;
        public float StartNormalizedTime;
        public byte RestartIfSame;
        public byte IsLoop;
    }
}
