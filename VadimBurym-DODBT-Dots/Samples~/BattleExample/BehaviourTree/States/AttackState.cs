using Unity.Entities;
using Unity.Mathematics;

namespace DODBT.Example.Battle.Components
{
    public struct AttackState : IComponentData
    {
        public Entity Owner;
        public Entity Target;
        public float RemainingTime;
    }
}
