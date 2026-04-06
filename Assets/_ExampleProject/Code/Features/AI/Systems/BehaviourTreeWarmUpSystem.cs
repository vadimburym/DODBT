using _ExampleProject.Code.Features.AI.Service;
using _Project.Code.Core.Abstractions.Contracts;
using _Project.Code.Infrastructure;
using _Project.Infrastructure;
using VadimBurym.DodBehaviourTree;

namespace _ExampleProject.Code.Infrastructure.StaticData.BehaviourTree
{
    public sealed class BehaviourTreeWarmUpSystem : IConstruct, IWarmUp
    {
        private BehaviourTreeStaticData _behaviourTreeStaticData;
        private IBehaviourTreeService _behaviourTreeService;
        
        public void Construct()
        {
            _behaviourTreeStaticData = ServiceLocator.Resolve<StaticDataService>().BehaviourTreeStaticData;
            _behaviourTreeService = ServiceLocator.Resolve<IBehaviourTreeService>();
        }

        public void WarmUp()
        {
            for (int i = 0; i < _behaviourTreeStaticData.Assets.Length; i++)
            {
                var asset = _behaviourTreeStaticData.Assets[i];
                var bt = new BehaviourTree<LeoEcsContext, LeoEcsLeafState>();
                bt.Construct(asset);
                _behaviourTreeService.SetupBehaviourTree(asset.GUID, bt);
            }
        }
    }
}