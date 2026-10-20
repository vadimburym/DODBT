using System;
using DODBT.Light.Example.Gameplay.Settings;
using UnityEngine;

namespace DODBT.Light.Example.Gameplay.Settings
{
    [CreateAssetMenu(fileName = nameof(MemoryPoolPipeline), menuName ="DODBT/Light Example/StaticData/New MemoryPoolPipeline")]
    public sealed class MemoryPoolPipeline : ScriptableObject
    {
        public GameObjectMemoryPoolInfo[] GameObjectMemoryPools;
        
        [Serializable]
        public struct GameObjectMemoryPoolInfo
        {
            public MemoryPoolId PoolId;
            public TransformId TransformId;
            public GameObject Asset;
            public int InitialCount;
        }
    }
}
