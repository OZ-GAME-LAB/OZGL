using OzGameLab01.Common.Operations;

namespace OzGameLab01.GameFlow.Models
{
    /// <summary>단일 씬 전환의 진행 상태와 요청 식별값</summary>
    public sealed class SceneTransitionModel
    {
        private readonly OperationRegistry _operations;
        private OperationRegistry.OperationLease _operation;
        public SceneTransitionModel(OperationRegistry operations = null)
            => _operations = operations ?? new OperationRegistry();
        public bool IsTransitioning { get; private set; }
        public string FromScene { get; private set; }
        public string ToScene { get; private set; }
        public bool TryBegin(string from, string to)
        {
            if (IsTransitioning || string.IsNullOrWhiteSpace(to)) return false;
            // 대상 씬이 달라도 동시에 진행할 수 없는 하나의 작업 자원이다.
            if (!_operations.TryBegin("GameFlow/SceneTransition", OperationPolicy.ConcurrentOnly, out _operation)) return false;
            FromScene = from;
            ToScene = to;
            IsTransitioning = true;
            return true;
        }
        public void Finish()
        {
            _operation?.Complete();
            _operation = null;
            IsTransitioning = false;
        }
        public void Cancel()
        {
            _operation?.Dispose();
            _operation = null;
            IsTransitioning = false;
        }
    }
}
