using System;
using System.Collections.Generic;

namespace Infrastructure.MemoryPool.Domain
{
    public sealed class ObjectMemoryPool<T> : IDisposable
        where T : class, new()
    {
        private readonly List<T> _activeItems = new();
        private readonly Queue<T> _freeList = new();

        public T DequeueItem()
        {
            if (_freeList.TryDequeue(out var item))
            {
                _activeItems.Add(item);
                return item;
            }
            else
            {
                item = new T();
                _activeItems.Add(item);
                return item;
            }
        }

        public void EnqueueItem(T item)
        {
            if (item != null && _activeItems.Remove(item))
                _freeList.Enqueue(item);
        }

        public void Clear()
        {
            for (int i = 0, count = _activeItems.Count; i < count; i++)
                _freeList.Enqueue(_activeItems[i]);

            _activeItems.Clear();
        }

        public void Dispose()
        {
            _activeItems.Clear();
            _freeList.Clear();
        }
    }
}