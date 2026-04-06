using System.Collections.Generic;
using VadimBurym.DodBehaviourTree;

namespace _ExampleProject.Code.Features.AI.Service
{
    public interface IBehaviourTreeService
    {
        IReadOnlyDictionary<string, BehaviourTree<LeoEcsContext, LeoEcsLeafState>> Trees { get; }
        void SetupBehaviourTree(string guid, BehaviourTree<LeoEcsContext, LeoEcsLeafState> tree);
        BehaviourTree<LeoEcsContext, LeoEcsLeafState> GetBehaviourTree(string guid);
    }
}