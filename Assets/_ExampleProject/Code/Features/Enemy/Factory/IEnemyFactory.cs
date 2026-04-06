using _Project.Code.Core.Keys;
using UnityEngine;

namespace _ExampleProject.Code.Features.Enemy.Factory
{
    public interface IEnemyFactory
    {
        int Create(EnemyId enemyId, Vector2 position);
    }
}