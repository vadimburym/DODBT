using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Infrastructure.MemoryPool.Domain
{
    public sealed class GameObjectMemoryPool
    {
        private readonly Transform _container;
        private readonly int _initValue;
        private readonly GameObject _itemPrefab;

        public IReadOnlyList<GameObject> ActiveItems => _activeItems;
        private readonly List<GameObject> _activeItems = new();
        private readonly Queue<GameObject> _freeList = new();
        
        public GameObjectMemoryPool(
            GameObject assetReference,
            Transform container,
            int initValue = 0)
        {
            _itemPrefab = assetReference;
            _container = container;
            _initValue = initValue;
        }

        public void Init()
        {
            for (int i = 0; i < _initValue; i++)
                SpawnItem();
            Clear();
        }

        public GameObject SpawnItem()
        {
            if (_freeList.TryDequeue(out var item))
                item.SetActive(true);
            else
                item = Object.Instantiate(_itemPrefab, _container);

            _activeItems.Add(item);
            return item;
        }

        public T SpawnItem<T>() where T : Component
        {
            return SpawnItem().GetComponent<T>();
        }

        public void UnspawnItem(GameObject item)
        {
            if (item != null && _activeItems.Remove(item))
            {
                item.SetActive(false);
                _freeList.Enqueue(item);
            }
        }

        public void UnspawnItem<T>(T item) where T : Component
        {
            UnspawnItem(item.gameObject);
        }

        public void Clear()
        {
            for (int i = 0, count = _activeItems.Count; i < count; i++)
            {
                GameObject item = _activeItems[i];
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