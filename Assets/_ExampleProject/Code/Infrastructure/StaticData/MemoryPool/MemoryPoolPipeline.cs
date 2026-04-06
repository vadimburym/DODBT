using System;
using _Project.Code.Core.Keys;
using UnityEngine;

namespace _Project.Code.Infrastructure.StaticData.MemoryPool
{
    [CreateAssetMenu(fileName = nameof(MemoryPoolPipeline), menuName ="_Project/StaticData/New MemoryPoolPipeline")]
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