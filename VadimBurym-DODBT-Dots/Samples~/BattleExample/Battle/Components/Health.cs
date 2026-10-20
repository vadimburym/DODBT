using Unity.Entities;

namespace DODBT.Example.Battle.Components
{
    public struct Health : IComponentData
    {
        public ushort Max;
        public ushort Current;
    }
}
