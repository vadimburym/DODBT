using System.Collections.Generic;
using UnityEngine;

namespace Infrastructure.MemoryPool.Domain
{
    public sealed class UIGameObjectMemoryPool<T> where T : Component
    {
        private readonly Transform _container;
        private readonly T _itemPrefab;

        public IReadOnlyList<T> ActiveItems => _activeItems;
        private readonly List<T> _activeItems;
        private readonly Queue<T> _freeList = new();
        
        public UIGameObjectMemoryPool(
            T asset,
            Transform container,
            List<T> instances)
        {
            _itemPrefab = asset;
            _container = container;
            _activeItems = instances;
            Clear();
        }
        
        public T SpawnItem()
        {
            if (_freeList.TryDequeue(out var item))
                item.gameObject.SetActive(true);
            else
                item = Object.Instantiate(_itemPrefab, _container);

            _activeItems.Add(item);
            return item;
        }

        public void UnspawnItem(T item)
        {
            if (item != null && _activeItems.Remove(item))
            {
                item.gameObject.SetActive(false);
                _freeList.Enqueue(item);
            }
        }

        public void Clear()
        {
            for (int i = 0, count = _activeItems.Count; i < count; i++)
            {
                T item = _activeItems[i];
                item.gameObject.SetActive(false);
                _freeList.Enqueue(item);
            }
            _activeItems.Clear();
        }

        public void Dispose()
        {
            //destroy all
        }
    }
}