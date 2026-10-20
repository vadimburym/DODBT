using Unity.Entities;

namespace DODBT.Example.Setup
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class BattleSystemGroup : ComponentSystemGroup { }
}
