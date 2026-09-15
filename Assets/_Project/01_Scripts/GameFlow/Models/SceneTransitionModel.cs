namespace OzGameLab01.GameFlow.Models
{
    /// <summary>단일 씬 전환의 진행 상태와 요청 식별값</summary>
    public sealed class SceneTransitionModel
    {
        public bool IsTransitioning { get; private set; }
        public string FromScene { get; private set; }
        public string ToScene { get; private set; }
        public bool TryBegin(string from, string to)
        {
            if (IsTransitioning || string.IsNullOrWhiteSpace(to)) return false;
            FromScene = from;
            ToScene = to;
            IsTransitioning = true;
            return true;
        }
        public void Finish() => IsTransitioning = false;
    }
}