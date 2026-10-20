using Unity.Entities;

namespace DODBT.Example.Battle.Animation
{
    public struct VATAnimator : IComponentData
    {
        public int MeshIndex;
        public int CurrentClipIndex;
        public float CurrentNormalizedTime;

        public int PreviousClipIndex;
        public float PreviousNormalizedTime;

        public float Blend01;
        public float BlendElapsed;
        public float BlendDuration;
        public float DefaultTransitionDuration;

        public float Speed;
        public byte Loop;
        public byte Playing;
    }
}
