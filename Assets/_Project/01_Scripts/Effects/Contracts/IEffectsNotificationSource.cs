using System;
using OzGameLab01.Effects.Models;

namespace OzGameLab01.Effects.Contracts
{
    /// <summary>공용 버스 어댑터의 인스턴스별 알림 구독 계약</summary>
    public interface IEffectsNotificationSource
    {
        event Action<EffectsNotification> Notification;
    }
}