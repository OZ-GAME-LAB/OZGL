using System;
using System.Collections.Generic;
using UnityEngine;

namespace OzGameLab01.Data
{
    /// <summary>발행처 인스턴스별 알림 전달과 최근 적용 결과 보관</summary>
    internal sealed class DataNotificationPublisher : IDataNotificationSource
    {
        private readonly Dictionary<string, DataNotification> _latest = new Dictionary<string, DataNotification>();
        private readonly Queue<DataNotification> _pending = new Queue<DataNotification>();
        private readonly object _gate = new object();
        private bool _dispatching;
        private long _revision;

        public event Action<DataNotification> Notification;

        public bool TryGetLatestNotification(string dataset, out DataNotification notification)
        {
            lock (_gate)
            {
                return _latest.TryGetValue(dataset, out notification);
            }
        }

        /// <summary>호출 스레드의 적용 결과 전달 및 구독자 예외 격리</summary>
        public void Publish(string dataset, DataNotificationKind kind, int count)
        {
            Record(dataset, kind, count);
            DispatchPending();
        }

        /// <summary>캐시 적용 순서에 따른 스냅샷 기록</summary>
        public void Record(string dataset, DataNotificationKind kind, int count)
        {
            lock (_gate)
            {
                DataNotification notification = new DataNotification(dataset, kind, count, ++_revision);
                _latest[dataset] = notification;
                _pending.Enqueue(notification);
            }
        }

        /// <summary>로드 잠금 외부의 순차 전달과 재진입 알림 대기</summary>
        public void DispatchPending()
        {
            lock (_gate)
            {
                if (_dispatching)
                {
                    return;
                }
                _dispatching = true;
            }
            while (true)
            {
                DataNotification notification;
                lock (_gate)
                {
                    if (_pending.Count == 0)
                    {
                        _dispatching = false;
                        return;
                    }
                    notification = _pending.Dequeue();
                }
                Action<DataNotification> handlers = Notification;
                if (handlers == null)
                {
                    continue;
                }
                foreach (Action<DataNotification> handler in handlers.GetInvocationList())
                {
                    try
                    {
                        handler(notification);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }
            }
        }

        public void ClearSubscribers() => Notification = null;
    }
}
