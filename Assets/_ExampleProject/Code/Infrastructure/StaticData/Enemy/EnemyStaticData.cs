using _Project.Code.Core.Keys;
using UnityEngine;

namespace _ExampleProject.Code.Infrastructure.StaticData.Enemy
{
    [CreateAssetMenu(fileName = nameof(EnemyStaticData), menuName ="_Project/StaticData/New EnemyStaticData")]
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