using System.Collections.Generic;
using VadimBurym.DodBehaviourTree;

namespace _ExampleProject.Code.Features.AI.Service
{
    public sealed class BehaviourTreeService : IBehaviourTreeService
    {
        public IReadOnlyDictionary<string, BehaviourTree<LeoEcsContext, LeoEcsLeafState>> Trees => _trees;
        private readonly Dictionary<string, BehaviourTree<LeoEcsContext, LeoEcsLeafState>> _trees = new();

        public void SetupBehaviourTree(string guid, BehaviourTree<LeoEcsContext, LeoEcsLeafState> tree)
            => _trees.Add(guid, tree);
        
        public BehaviourTree<LeoEcsContext, LeoEcsLeafState> GetBehaviourTree(string guid)
            => _trees[guid];
    }
}