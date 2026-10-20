using DODBT.Light.Example.Gameplay.Settings;
using UnityEngine;
using VadimBurym.DodBehaviourTree.Light;

namespace DODBT.Light.Example.Gameplay.Settings
{
    [CreateAssetMenu(fileName = nameof(EnemyStaticData), menuName ="DODBT/Light Example/New Enemy")]
    public sealed class EnemyConfig : ScriptableObject
    {
        [Header("Main")]
        public EnemyId Id;
        public float DifficultyWeight;
        public BehaviourTreeAsset Brain;
        public float BrainTickInterval;
        
        [Header("Stats")]
        public float MoveSpeed;
        public int MaxHealth;
        public int Armour;
        public WeaponId Weapon;
        public int InitAmmo;

        [Header("Player Visibility Sensor")] 
        public float DetectDistance;
        public float HuntingDistance;
        public float PlayerVisibilitySensorTickInterval;
    }
}
