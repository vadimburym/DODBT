using _ExampleProject.Code.Features.Enemy.Factory;
using _Project.Code.Core.Abstractions.Contracts;
using _Project.Code.Core.Keys;
using _Project.Infrastructure;
using UnityEngine;

namespace _ExampleProject.Code.Features.Enemy.Systems
{
    public sealed class EnemyInitSystem : IInit, IConstruct
    {
        private IEnemyFactory _enemyFactory;
        
        
        public void Construct()
        {
            _enemyFactory = ServiceLocator.Resolve<IEnemyFactory>();
        }
        
        public void Init()
        {
            for (int i = 0; i < 1; i++)
                _enemyFactory.Create(EnemyId.Default, new Vector2(0, 0));
        }
    }
}