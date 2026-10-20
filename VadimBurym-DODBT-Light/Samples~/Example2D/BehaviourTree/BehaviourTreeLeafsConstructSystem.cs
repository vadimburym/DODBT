using DODBT.Light.Example.BehaviourTree;
using DODBT.Light.Example.Setup;
using DODBT.Light.Example.Gameplay.Settings;
using DODBT.Light.Example.Services;

namespace DODBT.Light.Example.BehaviourTree
{
    public sealed class BehaviourTreeLeafsConstructSystem : IConstruct, IInit
    {
        private IBehaviourTreeService _behaviourTreeService;
        
        public void Construct()
        {
            _behaviourTreeService = ServiceLocator.Resolve<IBehaviourTreeService>();
        }

        public void Init()
        {
            foreach (var bt in _behaviourTreeService.Trees.Values)
            {
                var leafs = bt.Leafs;
                for (int i = 0; i < leafs.Length; i++)
                {
                    var leaf = leafs[i];
                    if (leaf is IConstruct construct)
                    {
                        construct.Construct();
                    }
                }
            }
        }
    }
}
