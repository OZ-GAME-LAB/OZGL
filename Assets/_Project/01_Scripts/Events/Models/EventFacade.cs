using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.Events.Contracts;

namespace OzGameLab01.Events
{
    /// <summary>
    /// Events 시스템 외부(BoardSceneController 등)가 호출하는 유일한 진입점입니다.
    /// 이 클래스는 게임 부팅 후 계속 살아있는 EventManager가 들고 있는 반면,
    /// 실제로 조작할 UI/데이터는 씬에 배치된 EventSession이 갖고 있으므로
    /// 필요할 때마다 EventSession을 찾아 사용합니다(EventSession의 GameObject가
    /// 비활성 상태로 시작해도 Awake 시점에 의존하지 않도록). 유효성 검사와 선택지
    /// 상태(EventState) 조율만 담당하고, 실제 표시는 EventSession.ShowEvent/CloseEvent에
    /// 위임합니다 — Facade는 View 타입을 직접 참조하지 않습니다. 완료 알림은 전역
    /// 버스로 EventChoiceCompleted를 발행합니다.
    /// </summary>
    public class EventFacade
    {
        private readonly EventState _state = new EventState();
        private EventSession _session;

        private EventSession GetSession()
        {
            if (_session == null)
            {
                _session = UnityEngine.Object.FindFirstObjectByType<EventSession>(FindObjectsInactive.Include);
            }

            return _session;
        }

        public bool OpenRandomEvent()
        {
            EventSession session = GetSession();
            if (session == null)
            {
                Debug.LogWarning("[EventFacade] 씬에서 EventSession을 찾을 수 없습니다.");
                return false;
            }

            EventSO[] eventPool = session.EventPool;
            if (eventPool == null || eventPool.Length == 0)
            {
                Debug.LogWarning("[EventFacade] 등록된 런타임 이벤트 데이터가 없습니다.", session.PanelObject);
                return false;
            }

            List<EventSO> validEvents = new();

            foreach (EventSO choiceEvent in eventPool)
            {
                if (choiceEvent != null && choiceEvent.choices != null && choiceEvent.choices.Count > 0)
                {
                    validEvents.Add(choiceEvent);
                }
            }

            if (validEvents.Count == 0)
            {
                Debug.LogWarning("[EventFacade] 유효한 런타임 이벤트 데이터가 없습니다.", session.PanelObject);
                return false;
            }

            return OpenChoiceEvent(validEvents[UnityEngine.Random.Range(0, validEvents.Count)]);
        }

        public bool OpenChoiceEvent(EventSO choiceEvent)
        {
            EventSession session = GetSession();
            if (session == null)
            {
                Debug.LogWarning("[EventFacade] 씬에서 EventSession을 찾을 수 없습니다.");
                return false;
            }

            if (choiceEvent == null || choiceEvent.choices == null || choiceEvent.choices.Count == 0)
            {
                Debug.LogWarning("[EventFacade] 유효한 이벤트 데이터가 없어 이벤트를 열 수 없습니다.", session.PanelObject);
                return false;
            }

            if (!session.ShowEvent(choiceEvent, ChoiceResult))
            {
                return false;
            }

            _state.SetChoiceList(choiceEvent.choices);
            return true;
        }

        public void ChoiceResult(int choiceIndex)
        {
            EventChoice selected = _state.GetChoice(choiceIndex);
            if (selected == null)
            {
                Debug.LogWarning($"[EventFacade] 잘못된 선택 인덱스입니다: {choiceIndex} ChoiceCount : {_state.ChoiceCount}");
                return;
            }

            _state.ExecuteChoice(selected);

            CloseCanvas();
            SystemBus.Messages.Publish(new EventChoiceCompleted());
        }

        private void CloseCanvas()
        {
            GetSession()?.CloseEvent();
        }
    }
}
