using UnityEngine;
using OzGameLab01.UI;

namespace OzGameLab01.Events
{
    /// <summary>
    /// "이번에 열린 이벤트" 하나에 대한 씬 소속 데이터를 보관합니다. 부팅 시부터 계속
    /// 살아있는 <see cref="OzGameLab01.Managers.EventManager"/>(Facade 보유)와 달리
    /// 이 오브젝트는 해당 이벤트 UI가 배치된 씬에서만 존재합니다. EventFacade가 필요할
    /// 때 이 컴포넌트를 찾아 Inspector 참조(eventUIView/eventPool)를 읽어갑니다.
    /// </summary>
    public class EventSession : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private EventUiView eventUIView;

        [Header("Runtime Event Data")]
        [SerializeField] private EventSO[] eventPool;

        public EventUiView EventUIView => eventUIView;
        public EventSO[] EventPool => eventPool;
        public GameObject PanelObject => gameObject;
    }
}
