using System;
using _Project.Code.Core.Keys;
using UnityEngine;

namespace _Project.Code.Features.Test
{
    [Serializable]
    public struct EnemyTag
    {
        public EnemyId EnemyId;
        public GameObject GameObjectRef;

        public void Setup(EnemyId enemyId, GameObject gameObjectRef)
        {
            EnemyId = enemyId;
            GameObjectRef = gameObjectRef;
        }
    }
}