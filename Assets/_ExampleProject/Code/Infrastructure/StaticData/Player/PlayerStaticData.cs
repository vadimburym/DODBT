using UnityEngine;

namespace _ExampleProject.Code.Infrastructure.StaticData.Player
{
    [CreateAssetMenu(fileName = nameof(PlayerStaticData), menuName ="_Project/StaticData/New PlayerStaticData")]
    public sealed class PlayerStaticData : ScriptableObject
    {
        public float MoveSpeed;
    }
}