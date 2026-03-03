using System;
using System.Collections.Generic;
using _Project.Code.Core.Keys;
using Infrastructure.MemoryPool.Domain;
using UnityEngine;

namespace Infrastructure.MemoryPool.Service
{
    public sealed class MemoryPoolService : IMemoryPoolService
    {
        private readonly Dictionary<MemoryPoolId, GameObjectMemoryPool> _gameObjectPools = new();
        private readonly Dictionary<Type, IDisposable> _objectMemoryPools = new();
        
        public void AddGameObjectMemoryPool(MemoryPoolId id, GameObjectMemoryPool memoryPool) => _gameObjectPools.Add(id, memoryPool);
        public GameObject SpawnGameObject(MemoryPoolId name) => _gameObjectPools[name].SpawnItem();
        public void UnspawnGameObject(MemoryPoolId name, GameObject item) => _gameObjectPools[name].UnspawnItem(item);
        public T SpawnGameObject<T>(MemoryPoolId name) where T : Component => _gameObjectPools[name].SpawnItem<T>();
        public void UnspawnGameObject<T>(MemoryPoolId name, T item) where T : Component => _gameObjectPools[name].UnspawnItem(item);
        public void ClearGameObjectPool(MemoryPoolId name) => _gameObjectPools[name].Clear();

        public ObjectMemoryPool<T> GetObjectPool<T>() where T : class, new()
        {
            var type = typeof(T);
            if (_objectMemoryPools.TryGetValue(type, out IDisposable pool))
            {
                return (ObjectMemoryPool<T>)pool;
            }
            else
            {
                var newPool = new ObjectMemoryPool<T>();
                _objectMemoryPools.Add(type, newPool);
                return newPool;
            }
        }

        public void EnqueueObject<T>(T item) where T : class, new() => GetObjectPool<T>().EnqueueItem(item);
        public T DequeueObject<T>() where T : class, new() => GetObjectPool<T>().DequeueItem();
        public void ClearObjectPool<T>() where T : class, new() => GetObjectPool<T>().Clear();
        public void DisposeObjectPool<T>() where T : class, new() => _objectMemoryPools[typeof(T)].Dispose();
    }
}