using DODBT.Light.Example.Gameplay.Enemies;
using DODBT.Light.Example.Setup;
using DODBT.Light.Example.Gameplay.Settings;
using DODBT.Light.Example.Services;
using UnityEngine;

namespace DODBT.Light.Example.Gameplay.Systems
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
