namespace OzGameLab01.Data
{
    /// <summary>
    /// 게임에서 사용하는 씬 이름을 한곳에서 관리합니다.
    ///
    /// 각 이름은 Build Profiles의 Scene List에 등록된
    /// 씬 이름과 정확히 일치해야 합니다.
    /// 
    /// 프로토타입 제작에 사용되는 씬 이름을 정의합니다. 실제 게임에서는 씬 이름이 달라질 수 있습니다.
    /// </summary>
    public static class SceneNames
    {
        public const string BOOT = "00_Boot";
        public const string TITLE = "01_Title";
        public const string BOARD = "02_MainGame";
        public const string COMBAT = "03_Combat";
        public const string TUTORIAL = "04_Tutorial";

        // 기존 외부 호출과 상수식의 호환 별칭
        /// <summary>
        /// 전역 매니저를 초기화하는 부트 씬입니다.
        /// </summary>
        public const string Boot = BOOT;

        /// <summary>
        /// 게임 시작과 종료 후 돌아오는 타이틀 씬입니다.
        /// </summary>
        public const string Title = TITLE;

        /// <summary>
        /// 주사위와 타일 이벤트가 진행되는 보드 씬입니다.
        /// </summary>
        public const string Board = BOARD;

        /// <summary>
        /// 일반 전투와 보스 전투가 진행되는 전투 씬입니다.
        /// </summary>
        public const string Combat = COMBAT;

        /// <summary>
        /// 튜토리얼 시퀀스를 재생하는 씬입니다.
        /// </summary>
        public const string Tutorial = TUTORIAL;
    }
}
