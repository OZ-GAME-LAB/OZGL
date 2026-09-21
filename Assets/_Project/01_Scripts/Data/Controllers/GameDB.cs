using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Newtonsoft.Json;

namespace OzGameLab01.Data
{
    public interface IIdentifiable
    {
        int Id { get; }
    }

    public interface IDataList<T> where T : IIdentifiable
    {
        List<T> GetList();
    }

    public class GameDB<T, TList> where T : IIdentifiable where TList : IDataList<T>
    {
        private readonly Dictionary<int, T> _dataDict = new();

        public async Task<bool> LoadAsync(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogWarning($"[GameDB<{typeof(T).Name}>] 어드레서블 주소가 설정되지 않았습니다.");
                return false;
            }

            var handle = Addressables.LoadAssetAsync<TextAsset>(address);
            try
            {
                TextAsset jsonAsset = await handle.Task;
                if (jsonAsset == null)
                {
                    Debug.LogError($"[GameDB<{typeof(T).Name}>] 에셋을 로드할 수 없습니다: {address}");
                    return false;
                }

                string jsonText = jsonAsset.text;
                Dictionary<int, T> next = await Task.Run(() => Deserialize(jsonText));
                lock (_dataDict)
                {
                    _dataDict.Clear();
                    foreach (KeyValuePair<int, T> pair in next)
                        _dataDict.Add(pair.Key, pair.Value);
                }

                Debug.Log($"[GameDB<{typeof(T).Name}>] {Count} rows loaded from {address}.");
                return true;
            }
            catch (Exception error)
            {
                Debug.LogError($"[GameDB<{typeof(T).Name}>] Failed to load {address}: {error.Message}");
                return false;
            }
            finally
            {
                if (handle.IsValid()) Addressables.Release(handle);
            }
        }

        private static Dictionary<int, T> Deserialize(string json)
        {
            TList wrapper = default;
            try { wrapper = JsonConvert.DeserializeObject<TList>(json); }
            catch (JsonException) { }

            List<T> rows = wrapper?.GetList();
            if (rows == null)
                rows = JsonConvert.DeserializeObject<List<T>>(json);
            if (rows == null)
                throw new InvalidOperationException($"{typeof(T).Name} data contains no rows.");

            var result = new Dictionary<int, T>();
            foreach (T item in rows)
            {
                if (item == null) throw new InvalidOperationException($"{typeof(T).Name} data contains a null row.");
                if (!result.TryAdd(item.Id, item))
                    throw new InvalidOperationException($"Duplicate {typeof(T).Name} ID {item.Id}.");
            }
            if (result.Count == 0)
                throw new InvalidOperationException($"{typeof(T).Name} data is empty.");
            return result;
        }

        public T Get(int id)
        {
            lock (_dataDict)
            {
                _dataDict.TryGetValue(id, out T value);
                return value;
            }
        }

        public bool TryGet(int id, out T value)
        {
            lock (_dataDict) return _dataDict.TryGetValue(id, out value);
        }

        public IReadOnlyDictionary<int, T> GetAll()
        {
            lock (_dataDict) return new Dictionary<int, T>(_dataDict);
        }

        public int Count
        {
            get { lock (_dataDict) return _dataDict.Count; }
        }

        public void Clear()
        {
            lock (_dataDict) _dataDict.Clear();
        }
    }
}
