using Unity.Entities;

namespace DODBT.Example.Battle.Components
{
    public struct TakeDamageRequest : IComponentData
    {
        public ushort Damage;
        public Entity Source;
    }
}
