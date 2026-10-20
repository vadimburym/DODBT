using System;
using DODBT.Light.Example.Gameplay.Settings;
using UnityEngine;

namespace DODBT.Light.Example.Gameplay.Components
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
