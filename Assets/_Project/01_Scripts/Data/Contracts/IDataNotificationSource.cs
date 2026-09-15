using System;

namespace OzGameLab01.Data
{
    /// <summary>공용 버스 어댑터의 구독 및 초기 상태 조회 계약</summary>
    public interface IDataNotificationSource
    {
        event Action<DataNotification> Notification;
        /// <summary>초기 로드 이후 구독자를 위한 최근 적용 결과 조회</summary>
        bool TryGetLatestNotification(string dataset, out DataNotification notification);
    }
}
