namespace OzGameLab01.Managers
{
    /// <summary>
    /// 게임 로직과 UI가 발행하는 사운드 재생 요청 데이터입니다.
    /// </summary>
    public readonly struct SoundRequest
    {
        public SoundId Id { get; }
        public SoundChannel Channel { get; }
        public bool Restart { get; }

        public SoundRequest(SoundId id, SoundChannel channel, bool restart = false)
        {
            Id = id;
            Channel = channel;
            Restart = restart;
        }
    }
}
