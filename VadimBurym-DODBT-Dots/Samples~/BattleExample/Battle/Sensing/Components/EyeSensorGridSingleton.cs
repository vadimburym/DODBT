using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace DODBT.Example.Battle.Grid
{
    public struct EyeSensorGridSingleton : IComponentData
    {
        public NativeParallelMultiHashMap<int2, Entity> Command0Grid;
        public NativeParallelMultiHashMap<int2, Entity> Command1Grid;
        public float CellSize;
    }
}
