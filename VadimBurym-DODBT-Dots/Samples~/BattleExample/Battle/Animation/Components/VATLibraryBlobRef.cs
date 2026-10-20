using Unity.Entities;

namespace DODBT.Example.Battle.Animation
{
    public struct VATLibraryBlobRef : IComponentData
    {
        public BlobAssetReference<VATAnimationLibraryBlob> Value;
    }
}
