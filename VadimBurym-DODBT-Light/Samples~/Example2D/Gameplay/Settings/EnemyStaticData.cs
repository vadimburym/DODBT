using DODBT.Light.Example.Gameplay.Settings;
using UnityEngine;

namespace DODBT.Light.Example.Gameplay.Settings
{
    [CreateAssetMenu(fileName = nameof(EnemyStaticData), menuName ="DODBT/Light Example/StaticData/New EnemyStaticData")]
    public sealed class EnemyStaticData : ScriptableObject
    {
        public EnemyConfig[] Enemies;
        
        public EnemyConfig GetEnemyConfig(EnemyId enemyId)
        {
            for (int i = 0; i < Enemies.Length; i++)
                if (Enemies[i].Id == enemyId) return Enemies[i];
            return null;
        }
    }
}
