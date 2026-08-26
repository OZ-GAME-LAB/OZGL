namespace OzGameLab01.Data
{
    /// <summary>
    /// 게임 전체에서 공통으로 사용하는 진행 상태입니다.
    /// </summary>
    public enum GameState
    {
        /// <summary>
        /// 게임 초기화 상태입니다.
        /// </summary>
        Boot,

        /// <summary>
        /// 타이틀 화면 상태입니다.
        /// </summary>
        Title,

        /// <summary>
        /// 게임 진행 상태입니다.
        /// </summary>
        Playing,

        /// <summary>
        /// 게임 일시정지 상태입니다.
        /// </summary>
        Paused
    }
}