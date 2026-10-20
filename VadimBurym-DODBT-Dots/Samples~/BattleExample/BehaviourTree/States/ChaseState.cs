using Unity.Entities;

namespace DODBT.Example.Battle.Components
{
    public struct ChaseState : IComponentData
    {
        public Entity Owner;
        public Entity Target;
        public float UpdateInterval;
        public float UpdateTimer;
    }
}
