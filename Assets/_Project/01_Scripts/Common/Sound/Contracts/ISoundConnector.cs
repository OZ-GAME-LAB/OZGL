using System;
using OzGameLab01.Managers;

namespace OzGameLab01.Interfaces
{
    /// <summary>
    /// 사운드 요청 발행과 구독에 사용할 인터페이스입니다.
    /// </summary>
    public interface ISoundConnector
    {
        event Action<SoundRequest> SoundRequested; // SoundId와 채널을 담은 재생 요청 이벤트

        void Publish(SoundRequest request); // 사운드 재생 요청 발행
    }
}
