using System;
using OzGameLab01.GameFlow.Models;

namespace OzGameLab01.GameFlow.Contracts
{
    /// <summary>공용 버스 어댑터의 인스턴스별 구독 계약</summary>
    public interface IGameFlowNotificationSource
    {
        event Action<GameFlowNotification> Notification;
    }
}