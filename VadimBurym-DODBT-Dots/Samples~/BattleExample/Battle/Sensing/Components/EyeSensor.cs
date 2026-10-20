using Unity.Entities;

namespace DODBT.Example.Battle.Grid
{
    public struct EyeSensor : IComponentData
    {
        public byte IsDetected;
        public Entity DetectedEntity;
        public float UpdateNearestTimer;
        public float ScanTimer;
    }
}
