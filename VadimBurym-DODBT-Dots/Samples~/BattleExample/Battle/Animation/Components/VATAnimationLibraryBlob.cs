using Unity.Entities;

namespace DODBT.Example.Battle.Animation
{
    public struct VATAnimationLibraryBlob
    {
        public BlobArray<VATClipRuntimeData> Clips;
        public byte ClipsLength;
    }
}
