using Unity.Entities;
using Unity.Mathematics;

namespace DODBT.Example.Battle.Components
{
    public struct TargetPosition : IComponentData
    {
        public float3 Position;
        public float StoppingRadius;
    }
}
