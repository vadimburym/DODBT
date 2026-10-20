using Unity.Entities;

namespace DODBT.Example.BehaviourTree
{
    public struct AiBrain : IComponentData
    {
        public byte BlobId;
        public float UpdateTime;
        public uint RandomState;
    }
}
