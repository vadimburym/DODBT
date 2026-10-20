using Unity.Entities;

namespace DODBT.Example.Battle.Components
{
    public struct CorpseTag : IComponentData, IEnableableComponent
    {
        public float Time;
    }
}
