using DODBT.Light.Example.BehaviourTree;
using DODBT.Light.Example.Gameplay.Settings;
using DODBT.Light.Example.Gameplay.Scene;
using UnityEngine;

namespace DODBT.Light.Example.Gameplay.Settings
{
    [CreateAssetMenu(fileName = nameof(StaticDataService), menuName="DODBT/Light Example/New StaticDataService")]
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
