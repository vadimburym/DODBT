using _Project.Code.Core.Keys;
using UnityEngine;
using VadimBurym.DodBehaviourTree;

namespace _ExampleProject.Code.Infrastructure.StaticData.Enemy
{
    [CreateAssetMenu(fileName = nameof(EnemyStaticData), menuName ="_Project/New Enemy")]
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