using Unity.Entities;

namespace DODBT.Example.Battle.Components
{
    public struct MovementStats : IComponentData //TODO Вынести в blob
    {
        public float Speed;
        public float RotationSpeed;
    }
}
