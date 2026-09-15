using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OzGameLab01.Data
{
    /// <summary>
    /// 완성된 ID 테이블을 교체하며 정의 데이터의 조회를 제공합니다.
    /// </summary>
    public sealed class IdDataCache<T>
    {
        private readonly Func<T, int> _getId;
        private readonly object _gate = new object();
        private IReadOnlyDictionary<int, T> _items = new ReadOnlyDictionary<int, T>(new Dictionary<int, T>());

        public IdDataCache(Func<T, int> getId)
        {
            _getId = getId ?? throw new ArgumentNullException(nameof(getId));
        }

        public IReadOnlyDictionary<int, T> Items
        {
            get
            {
                lock (_gate)
                {
                    return _items;
                }
            }
        }

        public void Replace(IEnumerable<T> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }
            Dictionary<int, T> next = new Dictionary<int, T>();
            foreach (T item in items)
            {
                if (ReferenceEquals(item, null))
                {
                    continue;
                }
                // 기존 테이블과 동일한 중복 ID의 마지막 항목 우선 정책
                next[_getId(item)] = item;
            }
            lock (_gate)
            {
                _items = new ReadOnlyDictionary<int, T>(next);
            }
        }

        public T Get(int id)
        {
            IReadOnlyDictionary<int, T> snapshot = Items;
            snapshot.TryGetValue(id, out T value);
            return value;
        }

        public List<T> GetAll()
        {
            return new List<T>(Items.Values);
        }
    }
}
