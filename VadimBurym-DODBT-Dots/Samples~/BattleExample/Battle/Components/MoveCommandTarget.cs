using Unity.Entities;

namespace DODBT.Example.Battle.Components
{
    [InternalBufferCapacity(32)]
    public struct MoveCommandTarget : IBufferElementData
    {
        public Entity Value;
    }
}
