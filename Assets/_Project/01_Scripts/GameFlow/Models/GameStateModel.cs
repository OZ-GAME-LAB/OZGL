using OzGameLab01.Data;

namespace OzGameLab01.GameFlow.Models
{
    /// <summary>초기화 여부와 중복 변경 방지를 포함한 게임 상태</summary>
    public sealed class GameStateModel
    {
        public bool IsInitialized { get; private set; }
        public GameState CurrentState { get; private set; } = GameState.Boot;
        public void Initialize() { CurrentState = GameState.Boot; IsInitialized = true; }
        public bool TryChange(GameState next, out GameState previous)
        {
            previous = CurrentState;
            if (!IsInitialized || next == CurrentState) return false;
            CurrentState = next;
            return true;
        }
        public void Reset() { CurrentState = GameState.Boot; IsInitialized = false; }
    }
}