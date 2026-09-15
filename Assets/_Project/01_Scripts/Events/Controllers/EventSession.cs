using System;
using UnityEngine;
using OzGameLab01.UI;

namespace OzGameLab01.Events
{
    /// <summary>
    /// "이번에 열린 이벤트" 하나에 대한 씬 소속 데이터를 보관합니다. 부팅 시부터 계속
    /// 살아있는 <see cref="OzGameLab01.Managers.EventManager"/>(Facade 보유)와 달리
    /// 이 오브젝트는 해당 이벤트 UI가 배치된 씬에서만 존재합니다. EventFacade가 필요할
    /// 때 이 컴포넌트를 찾아 Inspector 참조(eventUIView/eventPool)를 읽고
    /// <see cref="ShowEvent"/>/<see cref="CloseEvent"/>로 실제 표시를 위임합니다 —
    /// Facade는 View 타입을 직접 참조하지 않습니다.
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

        /// <summary>
        /// 이벤트 패널을 열고 제목/설명/선택지를 표시합니다. UI 참조가 없으면 false를
        /// 반환합니다.
        /// </summary>
        public bool ShowEvent(EventSO choiceEvent, Action<int> onChoiceSelected)
        {
            if (eventUIView == null)
            {
                Debug.LogError("[EventSession] 이벤트 UI 참조가 완전히 연결되지 않았습니다.", gameObject);
                return false;
            }

            gameObject.SetActive(true);

            eventUIView.SetTitle(choiceEvent.eventTitle);
            eventUIView.SetDescription(choiceEvent.eventDialog);

            if (choiceEvent.eventCategory == EventCategory.Choice)
            {
                eventUIView.ShowChoices(choiceEvent.choices, onChoiceSelected);
            }
            else if (choiceEvent.eventCategory == EventCategory.Action)
            {
                eventUIView.ShowAction(choiceEvent.choices, onChoiceSelected);
            }

            return true;
        }

        public void CloseEvent()
        {
            gameObject.SetActive(false);
        }
    }
}
