using System.Collections.Generic;
using UnityEngine;

namespace OzGameLab01.Events
{
    [CreateAssetMenu(fileName = "EventDB", menuName = "OzGameLab01/Data/EventDB")]
    public class EventDB : ScriptableObject
    {
        private Dictionary<string, EventSO> _eventList = new();

        [SerializeField] private List<EventSO> _eventList_Battle = new List<EventSO>();
        [SerializeField] private List<EventSO> _eventList_Quiz = new List<EventSO>();
        [SerializeField] private List<EventSO> _eventList_Relic = new List<EventSO>();
        [SerializeField] private List<EventSO> _eventList_Finish = new List<EventSO>();
        [SerializeField] private List<EventSO> _eventList_Event = new List<EventSO>();

        public List<EventSO> EventList_Event => _eventList_Event;
        public List<EventSO> EventList_Finish => _eventList_Finish;
        public List<EventSO> EventList_Quiz => _eventList_Quiz;
        public List<EventSO> EventList_Relic => _eventList_Relic;
        public List<EventSO> EventList_Battle => _eventList_Battle;

        public bool SetEvent(Dictionary<string, EventSO> eventList)
        {
            if (eventList == null)
            {
                Debug.LogError("[EventDB] 선택지 이벤트 DB를 찾을 수 없습니다.");
                return false;
            }
            _eventList = eventList;

            return true;
        }

        public bool SetDictionary()
        {
            for (int i = 0; i < _eventList_Battle.Count; i++)
            {
                _eventList.Add(_eventList_Battle[i].id, _eventList_Battle[i]);
            }
            for (int i = 0; i < _eventList_Quiz.Count; i++)
            {
                _eventList.Add(_eventList_Quiz[i].id, _eventList_Quiz[i]);
            }
            for (int i = 0; i < _eventList_Relic.Count; i++)
            {
                _eventList.Add(_eventList_Relic[i].id, _eventList_Relic[i]);
            }
            for (int i = 0; i < _eventList_Finish.Count; i++)
            {
                _eventList.Add(_eventList_Finish[i].id, _eventList_Finish[i]);
            }
            for (int i = 0; i < _eventList_Event.Count; i++)
            {
                _eventList.Add(_eventList_Event[i].id, _eventList_Event[i]);
            }

            return true;
        }

        public EventSO GetRandomEvent()
        {
            return _eventList_Event[Random.Range(0, _eventList_Event.Count)];
        }

        public EventSO GetRandomTypeEvent(EventChoiceCategory eventChoiceCategory)
        {
            EventSO tempSO;
            switch (eventChoiceCategory)
            {
                case EventChoiceCategory.Relic:
                    tempSO = _eventList_Relic[Random.Range(0, _eventList_Relic.Count)];
                    break;
                case EventChoiceCategory.Quiz:
                    int randResult = Random.Range(0, _eventList_Quiz.Count);
                    Debug.Log($"[EventDB] 랜덤 퀴즈 인덱스 : {randResult}");
                    tempSO = _eventList_Quiz[randResult];
                    break;
                case EventChoiceCategory.Battle:
                    tempSO = _eventList_Battle[Random.Range(0, _eventList_Battle.Count)];
                    break;
                default:
                    Debug.LogError("[EventDB] 타겟의 카테고리가 잘못되었습니다.");
                    tempSO = null;
                    break;
            }
            return tempSO;
        }

        public EventSO GetEventById(string id)
        {
            return _eventList[id];
        }

        public bool DeleteDB()
        {
            _eventList.Clear();
            _eventList_Event.Clear();
            _eventList_Battle.Clear();
            _eventList_Quiz.Clear();
            _eventList_Relic.Clear();
            _eventList_Finish.Clear();

            return true;
        }
    }
}
