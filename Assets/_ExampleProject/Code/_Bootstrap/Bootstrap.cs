using System;
using _Project.Infrastructure;
using UnityEngine;

namespace _Project.Code._Bootstrap
{
    [DefaultExecutionOrder(-100)]
    public sealed class Bootstrap : MonoBehaviour
    {
        [SerializeField] private GameInstaller _gameInstaller;
        [SerializeField] private SceneInstaller _sceneInstaller;
        
        private IContext _context;
        private bool _isGameReady;
        
        private void Awake()
        {
            _context = new GameContext();
            _gameInstaller.InstallBindings();
            _sceneInstaller.InstallBindings();
            EcsWorlds.WarmUp();
            _context.Construct();
            _context.Initialize();
            GC.Collect();
            _isGameReady = true;
#if UNITY_EDITOR
            Debug.Log("Game Ready");
#endif
        }
        
        public void Restart()
        {
            _context.Restart();
        }

        public void Load()
        {
            _context.Load();
        }
        
        private void Update()
        {
            if (!_isGameReady)
                return;
            _context.OnUpdate();
        }

        private void FixedUpdate()
        {
            if (!_isGameReady)
                return;
            _context.OnFixedUpdate();
        }

        private void LateUpdate()
        {
            if (!_isGameReady)
                return;
            _context.OnLateUpdate();
        }

        private void OnDestroy()
        {
            if (!_isGameReady)
                return;
            _context.OnDestroy();
        }
    }
}