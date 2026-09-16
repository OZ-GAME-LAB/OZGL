using OzGameLab01.Effects.Models;
using OzGameLab01.Interfaces;

namespace OzGameLab01.Managers
{
    /// <summary>
    /// 보유 유물 관리(획득/복원/초기화)를 담당하는 RelicFacade를 노출하는, 게임 부팅 후
    /// 계속 살아있는 매니저입니다. 실제 로직은 RelicFacade가 전담합니다.
    /// </summary>
    public sealed class RelicManager : Singleton<RelicManager>, IGameManager
    {
        public RelicFacade Facade { get; private set; }
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
                Facade = new RelicFacade();
                SystemBus.Register(Facade);
            }
            catch { Shutdown(); throw; }
        }

        public void Shutdown()
        {
            if (Facade != null)
            {
                SystemBus.Unregister(Facade);
                Facade.ClearSubscriptions();
            }
            Facade = null;
        }

        private void OnDestroy() => Shutdown();
    }
}
