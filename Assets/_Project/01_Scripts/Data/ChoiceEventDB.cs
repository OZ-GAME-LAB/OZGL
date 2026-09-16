using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class ChoiceEventDB : ScriptableObject
{
    private Dictionary<string, ChoiceEventSO> _eventList = new();

    [SerializeField] private List<ChoiceEventSO> _eventList_Battle = new List<ChoiceEventSO>();
    [SerializeField] private List<ChoiceEventSO> _eventList_Quiz = new List<ChoiceEventSO>();
    [SerializeField] private List<ChoiceEventSO> _eventList_Relic = new List<ChoiceEventSO>();
    [SerializeField] private List<ChoiceEventSO> _eventList_Finish = new List<ChoiceEventSO>();
    [SerializeField] private List<ChoiceEventSO> _eventList_Event = new List<ChoiceEventSO>();

    public List<ChoiceEventSO> EventList_Event => _eventList_Event;
    public List<ChoiceEventSO> EventList_Finish => _eventList_Finish;
    public List<ChoiceEventSO> EventList_Quiz => _eventList_Quiz;
    public List<ChoiceEventSO> EventList_Relic => _eventList_Relic;
    public List<ChoiceEventSO> EventList_Battle => _eventList_Battle;

    public bool SetEvent(Dictionary<string, ChoiceEventSO> eventList)
    {
        if (eventList == null)
        {
            Debug.LogError("[ChoiceEventDB] 선택지 이벤트 DB를 찾을 수 없습니다.");
            return false;
        }
        _eventList = eventList;

        return true;
    }
    public bool SetDictionary()
    {
        for(int i=0; i< _eventList_Battle.Count; i++)
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

    public ChoiceEventSO GetRandomEvent()
    {
        return _eventList_Event[Random.Range(0, _eventList_Event.Count)];
    }

    public ChoiceEventSO GetRandomTypeEvent(EventChoiceCategory eventChoiceCategory)
    {
        ChoiceEventSO tempSO;
        switch (eventChoiceCategory)
        {
            case EventChoiceCategory.Relic:
                tempSO = _eventList_Relic[Random.Range(0, _eventList_Relic.Count)];
                break;
            case EventChoiceCategory.Quiz:
                int randResult = Random.Range(0, _eventList_Quiz.Count);
                Debug.Log($"[ChoiceEventDB] 랜덤 퀴즈 인덱스 : {randResult}");
                tempSO = _eventList_Quiz[randResult];
                break;
            case EventChoiceCategory.Battle:
                tempSO = _eventList_Battle[Random.Range(0, _eventList_Battle.Count)];
                break;
            default:
                Debug.LogError("[ChoiceEventDB] 타겟의 카테고리가 잘못되었습니다.");
                tempSO = null;
                break;
        }
        return tempSO;
    }
    public ChoiceEventSO GetEventById(string id)
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
