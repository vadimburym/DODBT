using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace DODBT.Example.Battle.Grid
{
    public struct GridRuntimeMapSingleton : IComponentData
    {
        public NativeParallelHashMap<int2, Entity> OccupiedMap;
    }
}
