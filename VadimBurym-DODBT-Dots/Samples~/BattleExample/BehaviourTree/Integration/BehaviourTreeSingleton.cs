using Unity.Collections;
using Unity.Entities;
using VadimBurym.DodBehaviourTree.Dots;

namespace DODBT.Example.BehaviourTree
{
    public struct BehaviourTreeSingleton : IComponentData
    {
        public NativeArray<BlobAssetReference<BehaviourTreeBlob>> Blobs;
    }
}
