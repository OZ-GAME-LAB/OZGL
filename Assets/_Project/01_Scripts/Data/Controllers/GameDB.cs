using System.Collections.Generic;
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
            TextAsset jsonAsset = await handle.Task;

            if (jsonAsset == null)
            {
                Debug.LogError($"[GameDB<{typeof(T).Name}>] 에셋을 로드할 수 없습니다: {address}");
                return false;
            }

            string jsonText = jsonAsset.text;
            Addressables.Release(handle);

            await Task.Run(() =>
            {
                TList parsedList = JsonConvert.DeserializeObject<TList>(jsonText);
                if (parsedList == null) return;

                lock (_dataDict)
                {
                    _dataDict.Clear();
                    foreach (T item in parsedList.GetList())
                    {
                        if (item != null)
                            _dataDict[item.Id] = item;
                    }
                }
            });

            return true;
        }

        public T Get(int id)
        {
            _dataDict.TryGetValue(id, out T value);
            return value;
        }

        public bool TryGet(int id, out T value) => _dataDict.TryGetValue(id, out value);
        public IReadOnlyDictionary<int, T> GetAll() => _dataDict;
        public int Count => _dataDict.Count;
    }
}