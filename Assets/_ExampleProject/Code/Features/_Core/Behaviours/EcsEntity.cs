using Leopotam.EcsLite;
using UnityEngine;

namespace _ExampleProject.Code.Features._Core.Behaviours
{
    public sealed class EcsEntity : MonoBehaviour
    {
        public EcsWorld World { get; private set; } 
        public int Index { get; private set; }

        public void Construct(EcsWorld world, int index)
        {
            World = world;
            Index = index;
        }
    }
}