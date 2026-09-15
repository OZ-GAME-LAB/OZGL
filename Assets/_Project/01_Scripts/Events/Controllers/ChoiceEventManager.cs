using OzGameLab01.Events;

namespace OzGameLab01.Managers
{
    /// <summary>
    /// Events 폴더 시스템 전반을 관리하는, 게임 부팅 후 계속 살아있는 매니저입니다.
    /// "이번에 열린 이벤트" 하나에 대한 씬 소속 데이터는 <see cref="EventSession"/>이
    /// 따로 들고 있고, 이 클래스는 항상 존재하는 <see cref="EventFacade"/>를 노출하는
    /// 것 외의 일을 하지 않습니다.
    /// </summary>
    public class ChoiceEventManager : Singleton<ChoiceEventManager>
    {
        private EventFacade _facade;
        public EventFacade Facade => _facade ??= new EventFacade();
    }
}
