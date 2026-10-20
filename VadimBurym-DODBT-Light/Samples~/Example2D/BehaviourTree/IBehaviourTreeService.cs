using DODBT.Light.Example.BehaviourTree;
using System.Collections.Generic;
using VadimBurym.DodBehaviourTree.Light;

namespace DODBT.Light.Example.BehaviourTree
{
    public interface IBehaviourTreeService
    {
        IReadOnlyDictionary<string, BehaviourTree<LeoEcsContext, LeoEcsLeafState>> Trees { get; }
        void SetupBehaviourTree(string guid, BehaviourTree<LeoEcsContext, LeoEcsLeafState> tree);
        BehaviourTree<LeoEcsContext, LeoEcsLeafState> GetBehaviourTree(string guid);
    }
}
