using System.Collections.Generic;

namespace OzGameLab01.Effects.Models
{
    /// <summary>유물 보유 순서와 중복 허용 목록 상태</summary>
    public sealed class RelicCollectionModel<T>
    {
        private readonly List<T> _items = new List<T>();
        public IReadOnlyList<T> Items => _items;
        public int Count => _items.Count;
        public void Add(T item) => _items.Add(item);
        public void Clear() => _items.Clear();
    }
}
