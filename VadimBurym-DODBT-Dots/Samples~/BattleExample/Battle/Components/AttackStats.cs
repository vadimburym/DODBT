using Unity.Entities;

namespace DODBT.Example.Battle.Components
{
    public struct AttackStats : IComponentData //TODO Вынести в blob
    {
        public byte AttackRangeCells;
        public float AttackInterval;
        public ushort Damage;
    }
}
