using System;
using System.Collections.Generic;
using DODBT.Light.Example.Setup;
using DODBT.Light.Example.Gameplay.Settings;
using DODBT.Light.Example.Services;
using Leopotam.EcsLite;

namespace DODBT.Light.Example.Setup
{
    public sealed class GameSession
    {
        private readonly List<IConstruct> _construct = new();
        private readonly List<IWarmUp> _warmUp = new();
        private readonly List<IInit> _init = new();
        private readonly List<ITick> _tick = new();
        private readonly List<ILateTick> _lateTick = new();
        private readonly List<IDisposable> _disposables = new();
        
        private readonly List<IEcsSystem> _systems = new();
        private IEcsSystems _tickSystems;
        
        public void Construct()
        {
            ServiceLocator.ResolveList(_construct, optional:true);
            ServiceLocator.ResolveList(_warmUp, optional:true);
            ServiceLocator.ResolveList(_init, optional:true);
            ServiceLocator.ResolveList(_tick, optional:true);
            ServiceLocator.ResolveList(_lateTick, optional:true);
            ServiceLocator.ResolveList(_disposables, optional:true);
            
            ServiceLocator.ResolveList(_systems, optional:true);
            _tickSystems = EcsWorlds.CreateSystems(_systems, name:"TICK", isMainDebug:true);
        }

        public void Initialize()
        {
            for (int i = 0; i < _construct.Count; i++)
                _construct[i].Construct();
            for (int i = 0; i < _warmUp.Count; i++)
                _warmUp[i].WarmUp();
            _tickSystems?.Init();
            for (int i = 0; i < _init.Count; i++)
                _init[i].Init();
        }

        public void OnUpdate()
        {
            for (int i = 0; i < _tick.Count; i++)
                _tick[i].Tick();
            _tickSystems?.Run();
        }

        public void OnLateUpdate()
        {
            for (int i = 0; i < _lateTick.Count; i++)
                _lateTick[i].LateTick();
        }

        public void OnDestroy()
        {
            try { _tickSystems?.Destroy(); }
            finally
            {
                _tickSystems = null;
                try
                {
                    for (int i = _disposables.Count - 1; i >= 0; i--) _disposables[i].Dispose();
                }
                finally
                {
                    EcsWorlds.CleanUp();
                    ServiceLocator.Clear();
                }
            }
        }
    }
}
