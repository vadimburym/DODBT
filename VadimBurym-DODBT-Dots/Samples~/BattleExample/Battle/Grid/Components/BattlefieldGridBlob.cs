using Unity.Entities;
using Unity.Mathematics;

namespace DODBT.Example.Battle.Grid
{
    public struct BattlefieldGridBlob
    {
        public float3 Origin;
        public float CellSize;
        public int Width;
        public int Height;
        public BlobArray<byte> Walkable;
    }
}
