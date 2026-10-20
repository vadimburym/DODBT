using Unity.Entities;

namespace DODBT.Example.Battle.Grid
{
    public struct EyeSensorStats : IComponentData //TODO в Blob
    {
        public float DetectRadius;
        public float ChaseRadius;
        public float UpdateNearestInterval;
        public float ScanInterval;
    }
}
