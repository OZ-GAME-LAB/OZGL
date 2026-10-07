using System;
using System.Collections.Generic;
using OzGameLab01.GameFlow.Models;
using UnityEngine;

namespace OzGameLab01.GameFlow.Controllers
{
    /// <summary>메인 스레드의 완료 알림 전달과 재진입 순서 보존</summary>
    internal sealed class GameFlowNotificationPublisher
    {
        private readonly Queue<GameFlowNotification> _pending = new Queue<GameFlowNotification>();
        private long _revision;
        private bool _dispatching;
        public event Action<GameFlowNotification> Notification;

        public void Publish(GameFlowNotificationKind kind, string from = null, string to = null, OzGameLab01.Data.GameState previous = default, OzGameLab01.Data.GameState current = default)
        {
            _pending.Enqueue(new GameFlowNotification(kind, from, to, previous, current, ++_revision));
            if (_dispatching) return;
            _dispatching = true;
            try
            {
                while (_pending.Count > 0)
                {
                    GameFlowNotification notification = _pending.Dequeue();
                    Action<GameFlowNotification> handlers = Notification;
                    if (handlers == null) continue;
                    foreach (Action<GameFlowNotification> handler in handlers.GetInvocationList())
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