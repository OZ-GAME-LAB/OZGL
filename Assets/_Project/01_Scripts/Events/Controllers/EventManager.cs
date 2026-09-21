using OzGameLab01.Events;
using OzGameLab01.Interfaces;
using OzGameLab01.Common;

namespace OzGameLab01.Managers
{
    /// <summary>
    /// Events 폴더 시스템 전반을 관리하는, 게임 부팅 후 계속 살아있는 매니저입니다.
    /// "이번에 열린 이벤트" 하나에 대한 씬 소속 데이터는 <see cref="EventSession"/>이
    /// 따로 들고 있고, 이 클래스는 항상 존재하는 <see cref="EventFacade"/>를 노출하는
    /// 것 외의 일을 하지 않습니다.
    /// </summary>
    public class EventManager : Singleton<EventManager>, IGameManager
    {
        public EventFacade Facade { get; private set; }
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
                Facade = new EventFacade();
                SystemBus.Register(Facade);
            }
            catch { Shutdown(); throw; }
        }

        public void Shutdown()
        {
            if (Facade != null) SystemBus.Unregister(Facade);
            Facade = null;
        }

        private void OnDestroy() => Shutdown();
    }
}
