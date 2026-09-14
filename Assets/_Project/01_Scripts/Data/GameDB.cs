using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using OzGameLab01.Data;

/// <summary>
/// JSON 로딩과 역직렬화를 조율하고 완성된 ID 캐시를 공개합니다.
/// </summary>
public class GameDB<T, TList> where TList : IDataList<T>
{
    private readonly IJsonAssetLoader _loader;
    private readonly SemaphoreSlim _loadGate = new SemaphoreSlim(1, 1);
    private readonly IdDataCache<T> _cache;
    private static readonly FieldInfo IdField = typeof(T).GetField("id");

    public GameDB() : this(new AddressableJsonAssetLoader())
    {
    }

    public GameDB(IJsonAssetLoader loader)
    {
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _cache = new IdDataCache<T>(GetId);
    }

    private static int GetId(T item)
    {
        return (int)IdField.GetValue(item);
    }

    public async Task LoadAsync(string address)
    {
        await _loadGate.WaitAsync();
        try
        {
            if (IdField == null || IdField.FieldType != typeof(int))
            {
                throw new InvalidOperationException(typeof(T).Name + "에 public int id 필드가 필요합니다.");
            }
            string json = await _loader.LoadAsync(address);
            List<T> items = await Task.Run(() => JsonDataParser.Parse<T, TList>(json));
            if (items == null)
            {
                throw new InvalidOperationException("JSON 데이터 목록이 없습니다: " + address);
            }
            // 역직렬화 또는 ID 검증 실패 시 이전 캐시 유지
            _cache.Replace(items);
        }
        finally
        {
            _loadGate.Release();
        }
    }

    public T Get(int id)
    {
        return _cache.Get(id);
    }

    public List<T> GetAll()
    {
        return _cache.GetAll();
    }
}
