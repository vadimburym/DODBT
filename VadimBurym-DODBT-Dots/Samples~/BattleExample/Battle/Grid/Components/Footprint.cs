using Unity.Entities;

namespace DODBT.Example.Battle.Grid
{
    public struct Footprint : IComponentData
    {
        public byte FootprintX;
        public byte FootprintY;
    }
}
