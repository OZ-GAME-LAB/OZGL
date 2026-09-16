using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace OzGameLab01.Data
{
    /// <summary>
    /// JSON 로드·역직렬화·캐시 교체의 실행 흐름과 완료 알림
    /// </summary>
    public class GameDB<T, TList> : IDataNotificationSource where TList : IDataList<T>
    {
        private readonly DataNotificationPublisher _notifications = new DataNotificationPublisher();
        private readonly IJsonAssetLoader _loader;
        private readonly SemaphoreSlim _loadGate = new SemaphoreSlim(1, 1);
        private readonly IdDataCache<T> _cache;
        private static readonly FieldInfo _idField = typeof(T).GetField("id");

        public string Dataset => typeof(T).FullName;

        public event Action<DataNotification> Notification
        {
            add => _notifications.Notification += value;
            remove => _notifications.Notification -= value;
        }

        public bool TryGetLatestNotification(string dataset, out DataNotification notification)
            => _notifications.TryGetLatestNotification(dataset, out notification);

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
            return (int)_idField.GetValue(item);
        }

        public async Task LoadAsync(string address)
        {
            await _loadGate.WaitAsync();
            try
            {
                if (_idField == null || _idField.FieldType != typeof(int))
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
                _notifications.Record(Dataset, DataNotificationKind.CacheReplaced, _cache.Items.Count);
            }
            finally
            {
                _loadGate.Release();
            }
            // 캐시 교체 및 로드 잠금 해제 이후 완료 알림
            _notifications.DispatchPending();
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
}
