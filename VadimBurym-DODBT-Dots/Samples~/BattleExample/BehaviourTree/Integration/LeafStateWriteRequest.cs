using Unity.Entities;

namespace DODBT.Example.BehaviourTree
{
    [InternalBufferCapacity(0)]
    public struct LeafStateWriteRequest : IComponentData
    {
        public Entity Entity;
        public ushort Index;
        public Entity Value;
    }
}
