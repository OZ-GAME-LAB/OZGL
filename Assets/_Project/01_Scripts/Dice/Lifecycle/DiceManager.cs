using UnityEngine;
using OzGameLab01.Dice;
using OzGameLab01.Common.Messaging;
using OzGameLab01.Interfaces;

namespace OzGameLab01.Managers
{
    /// <summary>Dice의 생성·등록·종료만 담당. 주사위 규칙은 DiceModel이 소유한다.</summary>
    public class DiceManager : Singleton<DiceManager>, IGameManager
    {
        [Header("Dice Settings")]
        [SerializeField] private int _minDice = 1;
        [SerializeField] private int _maxDice = 6;
        private MessageBus _messages;
        private DiceModel _model;
        public DiceFacade Facade { get; private set; }
        public bool IsInitialized => Facade != null;

        protected override void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            base.Awake();
            // 씬 직접 실행 호환. 부팅 경로에서도 Initialize는 중복 호출에 안전하다.
            Initialize();
        }
        public void Initialize()
        {
            if (IsInitialized) return;
            try
            {
                _messages = new MessageBus(Debug.LogException);
                _model = new DiceModel(new DiceState(), _messages, SystemBus.Messages,
                    () => Random.Range(_minDice, _maxDice + 1));
                Facade = new DiceFacade(_messages, SystemBus.Messages);
                SystemBus.Register(Facade);
            }
            catch { Shutdown(); throw; }
        }
        public void Shutdown()
        {
            if (Facade != null) SystemBus.Unregister(Facade);
            Facade?.Dispose();
            _model?.Dispose();
            _messages?.Dispose();
            Facade = null;
            _model = null;
            _messages = null;
        }
        private void OnDestroy() => Shutdown();
    }
}
