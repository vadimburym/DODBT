using Unity.Entities;

namespace DODBT.Example.Battle.Grid
{
    public struct BattlefieldGridSingleton : IComponentData
    {
        public BlobAssetReference<BattlefieldGridBlob> Value;
    }
}
