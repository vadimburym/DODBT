using Unity.Entities;
using Unity.Rendering;

namespace DODBT.Example.Battle.Animation
{
    [MaterialProperty("unity_RendererUserValuesPropertyEntry")]
    public struct VATRendererUserValue : IComponentData
    {
        public uint Value;
    }
}
