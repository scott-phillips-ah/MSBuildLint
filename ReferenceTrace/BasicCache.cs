using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ReferenceTrace
{
    public class BasicCache<K, V> : IDictionary<K, V> where K : class where V : class
    {
        private readonly Dictionary<K, V> _cache = new Dictionary<K, V>();
        private Func<K, V> _cacheMissLoader;

        public BasicCache(Func<K, V> cacheMissLoader)
        {
            _cacheMissLoader = cacheMissLoader;
        }

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => _cache.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(KeyValuePair<K, V> item) => _cache.Add(item.Key, item.Value);

        public void Clear() => _cache.Clear();

        public bool Contains(KeyValuePair<K, V> item) => _cache.Contains(item);

        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        public bool Remove(KeyValuePair<K, V> item) => _cache.Remove(item.Key);

        public int Count => _cache.Count;
        public bool IsReadOnly => false;
        public void Add(K key, V value) => _cache.Add(key, value);

        public bool ContainsKey(K key) => _cache.ContainsKey(key);

        public bool Remove(K key) => _cache.Remove(key);

        public bool TryGetValue(K key, out V value)
        {
            if (_cache.TryGetValue(key, out value)) return true;
            value = _cacheMissLoader(key);
            _cache.Add(key, value);

            return true;
        }

        public V this[K key]
        {
            get
            {
                TryGetValue(key, out var value);
                return value;
            }
            set => _cache[key] = value;
        }

        public ICollection<K> Keys { get; }
        public ICollection<V> Values { get; }
    }
}