using System;
using System.Collections;
using System.Collections.Generic;

namespace KH.Framework2D.Utils
{
    /// <summary>
    /// Observable list that notifies when items are added, removed, or cleared.
    /// </summary>
    public class ObservableCollection<T> : IEnumerable<T>
    {
        private readonly List<T> _items = new();
        
        public event Action<T> OnItemAdded;
        public event Action<T> OnItemRemoved;
        public event Action OnCleared;
        public event Action OnChanged; // Any modification
        
        public int Count => _items.Count;
        public T this[int index] => _items[index];
        
        public void Add(T item)
        {
            _items.Add(item);
            OnItemAdded?.Invoke(item);
            OnChanged?.Invoke();
        }
        
        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                _items.Add(item);
                OnItemAdded?.Invoke(item);
            }
            OnChanged?.Invoke();
        }
        
        public bool Remove(T item)
        {
            if (_items.Remove(item))
            {
                OnItemRemoved?.Invoke(item);
                OnChanged?.Invoke();
                return true;
            }
            return false;
        }
        
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _items.Count) return;
            
            var item = _items[index];
            _items.RemoveAt(index);
            OnItemRemoved?.Invoke(item);
            OnChanged?.Invoke();
        }
        
        public void Clear()
        {
            _items.Clear();
            OnCleared?.Invoke();
            OnChanged?.Invoke();
        }
        
        public bool Contains(T item) => _items.Contains(item);
        public int IndexOf(T item) => _items.IndexOf(item);
        
        public void ClearSubscriptions()
        {
            OnItemAdded = null;
            OnItemRemoved = null;
            OnCleared = null;
            OnChanged = null;
        }
        
        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
