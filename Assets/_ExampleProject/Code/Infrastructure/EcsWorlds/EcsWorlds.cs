using System;
using System.Collections.Generic;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace _Project.Infrastructure
{
    public static class EcsWorlds
    {
        public static readonly string[] WORLDS = new string[3]
        {
            "DEFAULT",
            "EVENTS",
            "BT_STATES"
        };

        public static readonly string DEFAULT = WORLDS[0];
        public static readonly string EVENTS = WORLDS[1];
        public static readonly string BT_STATES = WORLDS[2];
        
        private static readonly Dictionary<string, EcsWorld> _worlds = new();

        public static void WarmUp()
        {
            for (int i = 0; i < WORLDS.Length; i++)
            {
                _worlds.Add(WORLDS[i], new EcsWorld());
            }
        }

        public static EcsWorld GetWorld(string worldName)
            => _worlds[worldName];

        public static EcsPool<T> GetPool<T>(string worldName) where T : struct
            => _worlds[worldName].GetPool<T>();

        public static EcsFilter GetFilter<T>(string worldName) where T : struct
            => _worlds[worldName].Filter<T>().End();

        public static void CleanUp()
        {
            foreach (var pair in _worlds)
            {
                pair.Value.Destroy();
            }
            _worlds.Clear();
        }

        public static void RaiseEvent<T>(string worldName, T eventData) where T : struct
        {
            var world = _worlds[worldName];
            var eventEntity = world.NewEntity();
            world.GetPool<T>().Add(eventEntity) = eventData;
            world.GetPool<OneFrameTag>().Add(eventEntity);
        }

        public static void RaiseRequest<T>(string worldName, T requestData) where T : struct
        {
            var world = _worlds[worldName];
            var request = world.NewEntity();
            world.GetPool<T>().Add(request) = requestData;
        }

        public static EcsSystems CreateSystems()
        {
            return new EcsSystems(_worlds[DEFAULT]);
        }

        public static EcsSystems CreateSystems(List<IEcsSystem> systems, string name = "", bool isMainDebug = false)
        {
            var ecsSystems = new EcsSystems(_worlds[DEFAULT]);
            for (int i = 0; i < systems.Count; i++)
                ecsSystems.Add(systems[i]);
#if UNITY_EDITOR
            AddSystemsDebug(ecsSystems, name, isMainDebug);
#endif
            InjectSystems(ecsSystems);
            return ecsSystems;
        }
#if UNITY_EDITOR
        public static void AddSystemsDebug(IEcsSystems systems, string systemsName, bool isMainDebug = false)
        {
            systems.Add(new Leopotam.EcsLite.UnityEditor.EcsSystemsDebugSystem(systemsName));
            if (isMainDebug == true)
            {
                systems.Add(new Leopotam.EcsLite.UnityEditor.EcsWorldDebugSystem());
                foreach (var key in _worlds.Keys)
                {
                    if (key == DEFAULT)
                        continue;
                    systems.Add(new Leopotam.EcsLite.UnityEditor.EcsWorldDebugSystem(key));
                }
            }
        }
#endif
        public static void InjectSystems(IEcsSystems systems)
        {
            foreach (var world in _worlds)
            {
                systems.AddWorld(world.Value, world.Key);
            }
            systems.Inject();
        }
    }
}