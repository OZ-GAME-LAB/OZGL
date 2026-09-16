using System;
using OzGameLab01.Board.Models;
namespace OzGameLab01.Board.Controllers
{
    // 공용 버스 어댑터의 보드 알림 구독 계약
    public interface IBoardNotificationSource
    {
        event Action<BoardNotification> Notification;
    }
}
