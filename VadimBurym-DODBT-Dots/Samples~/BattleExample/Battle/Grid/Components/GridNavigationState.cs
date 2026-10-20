using Unity.Entities;
using Unity.Mathematics;

namespace DODBT.Example.Battle.Grid
{
    public struct GridNavigationState : IComponentData
    {
        public int2 MovingCell;
        public int2 OccupiedCell;
        public byte HasOccupiedCell;
    }
}
