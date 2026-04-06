using System;
using System.Collections.Generic;
using _Project.Code.Core.Abstractions.Contracts;
using _Project.Infrastructure;
using Leopotam.EcsLite;

namespace _Project.Code._Bootstrap
{
    public sealed class GameContext : IContext
    {
        private readonly List<IConstruct> _construct = new();
        private readonly List<IWarmUp> _warmUp = new();
        private readonly List<ICleanUp> _cleanUp = new();
        private readonly List<IInit> _init = new();
        private readonly List<ITick> _tick = new();
        private readonly List<IFixedTick> _fixedTick = new();
        private readonly List<ILateTick> _lateTick = new();
        private readonly List<ISave> _save = new();
        private readonly List<ILoad> _load = new();
        private readonly List<IDisposable> _disposables = new();
        
        private readonly List<IEcsSystem> _systems = new();
        private IEcsSystems _tickSystems;
        
        public void Construct()
        {
            ServiceLocator.ResolveList(_construct, optional:true);
            ServiceLocator.ResolveList(_warmUp, optional:true);
            ServiceLocator.ResolveList(_cleanUp, optional:true);
            ServiceLocator.ResolveList(_init, optional:true);
            ServiceLocator.ResolveList(_tick, optional:true);
            ServiceLocator.ResolveList(_fixedTick, optional:true);
            ServiceLocator.ResolveList(_save, optional:true);
            ServiceLocator.ResolveList(_load, optional:true);
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

        public void Restart()
        {
            for (int i = 0; i < _cleanUp.Count; i++)
                _cleanUp[i].CleanUp();
            for (int i = 0; i < _init.Count; i++)
                _init[i].Init();
        }

        public void Load()
        {
            for (int i = 0; i < _cleanUp.Count; i++)
                _cleanUp[i].CleanUp();
            for (int i = 0; i < _load.Count; i++)
                _load[i].Load();
        }

        public void OnUpdate()
        {
            for (int i = 0; i < _tick.Count; i++)
                _tick[i].Tick();
            _tickSystems?.Run();
        }

        public void OnFixedUpdate()
        {
            for (int i = 0; i < _fixedTick.Count; i++)
                _fixedTick[i].FixedTick();
        }

        public void OnLateUpdate()
        {
            for (int i = 0; i < _lateTick.Count; i++)
                _lateTick[i].LateTick();
        }

        public void OnSave()
        {
            for (int i = 0; i < _save.Count; i++)
                _save[i].Save();
        }

        public void OnDestroy()
        {
            for (int i = 0; i < _disposables.Count; i++)
                _disposables[i].Dispose();
            _tickSystems?.Destroy();
        }
    }
}