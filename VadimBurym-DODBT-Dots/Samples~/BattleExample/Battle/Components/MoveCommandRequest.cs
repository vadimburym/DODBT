using Unity.Entities;
using Unity.Mathematics;

namespace DODBT.Example.Battle.Components
{
    public struct MoveCommandRequest : IComponentData
    {
        public float3 Destination;
    }
}
