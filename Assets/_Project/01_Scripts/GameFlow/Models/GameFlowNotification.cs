using OzGameLab01.Data;

namespace OzGameLab01.GameFlow.Models
{
    public enum GameFlowNotificationKind
    {
        ManagersReady,
        ManagersFailed,
        GameStateChanged,
        TransitionStarted,
        SceneActivated,
        TransitionCompleted,
        TransitionFailed,
        TransitionInterrupted
    }

    /// <summary>초기화·상태 변경·씬 전환 결과의 값 스냅샷</summary>
    public readonly struct GameFlowNotification
    {
        public GameFlowNotificationKind Kind { get; }
        public string FromScene { get; }
        public string ToScene { get; }
        public GameState PreviousState { get; }
        public GameState CurrentState { get; }
        public long Revision { get; }
        public GameFlowNotification(GameFlowNotificationKind kind, string from, string to,
            GameState previous, GameState current, long revision)
        {
            Kind = kind; FromScene = from; ToScene = to;
            PreviousState = previous; CurrentState = current; Revision = revision;
        }
    }
}