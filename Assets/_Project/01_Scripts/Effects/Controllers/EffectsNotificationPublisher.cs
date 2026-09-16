using System;
using System.Collections.Generic;
using OzGameLab01.Effects.Models;
using UnityEngine;

namespace OzGameLab01.Effects.Controllers
{
    /// <summary>메인 스레드의 완료 알림 전달과 재진입 순서 보존</summary>
    internal sealed class EffectsNotificationPublisher
    {
        private readonly Queue<EffectsNotification> _pending = new Queue<EffectsNotification>();
        private long _revision;
        private bool _dispatching;
        public event Action<EffectsNotification> Notification;

        public void Publish(EffectsNotificationKind kind, int sourceId, int count)
        {
            _pending.Enqueue(new EffectsNotification(kind, sourceId, count, ++_revision));
            if (_dispatching) return;
            _dispatching = true;
            try
            {
                while (_pending.Count > 0)
                {
                    EffectsNotification notification = _pending.Dequeue();
                    Action<EffectsNotification> handlers = Notification;
                    if (handlers == null) continue;
                    foreach (Action<EffectsNotification> handler in handlers.GetInvocationList())
                    {
                        try { handler(notification); }
                        catch (Exception exception) { Debug.LogException(exception); }
                    }
                }
            }
            finally { _dispatching = false; }
        }

        public void ClearSubscribers()
        {
            Notification = null;
            _pending.Clear();
        }
    }
}