using _ExampleProject.Code.Infrastructure.StaticData.BehaviourTree;
using _ExampleProject.Code.Infrastructure.StaticData.Enemy;
using _ExampleProject.Code.Infrastructure.StaticData.Player;
using _ExampleProject.Code.Infrastructure.StaticData.Projectile;
using _ExampleProject.Code.Infrastructure.StaticData.Weapons;
using _Project.Code.Infrastructure.StaticData.MemoryPool;
using UnityEngine;

namespace _Project.Code.Infrastructure
{
    [CreateAssetMenu(fileName = nameof(StaticDataService), menuName="_Project/New StaticDataService")]
    public sealed class StaticDataService : ScriptableObject
    {
        public MemoryPoolPipeline MemoryPoolPipeline;
        public EnemyStaticData EnemyStaticData;
        public BehaviourTreeStaticData BehaviourTreeStaticData;
        public PlayerStaticData PlayerStaticData;
        public WeaponsStaticData WeaponsStaticData;
        public ProjectilesStaticData ProjectilesStaticData;
    }
}