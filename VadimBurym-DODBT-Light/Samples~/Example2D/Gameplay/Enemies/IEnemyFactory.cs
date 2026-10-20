using DODBT.Light.Example.Gameplay.Settings;
using UnityEngine;

namespace DODBT.Light.Example.Gameplay.Enemies
{
    public interface IEnemyFactory
    {
        int Create(EnemyId enemyId, Vector2 position);
    }
}
