using UnityEngine;

namespace DODBT.Light.Example.Gameplay.Settings
{
    [CreateAssetMenu(fileName = nameof(PlayerStaticData), menuName ="DODBT/Light Example/StaticData/New PlayerStaticData")]
    public sealed class PlayerStaticData : ScriptableObject
    {
        public float MoveSpeed;
    }
}
