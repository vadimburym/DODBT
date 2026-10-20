using DODBT.Light.Example.BehaviourTree;
using DODBT.Light.Example.Setup;
using DODBT.Light.Example.Gameplay.Settings;
using DODBT.Light.Example.Services;
using VadimBurym.DodBehaviourTree.Light;

namespace DODBT.Light.Example.BehaviourTree
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
