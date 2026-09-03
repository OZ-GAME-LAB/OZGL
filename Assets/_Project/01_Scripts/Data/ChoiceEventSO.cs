using OzGameLab01.Managers;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChoiceEventSO_", menuName = "OZGL/Data/ChoiceEvent")]
public class ChoiceEventSO : ScriptableObject
{
    public int id;
    public string eventTitle;
    public string eventDialog;

    public List<EventChoice> choices;
}
//이벤트 매니저에서 각 선택지의 데이터를 관리할 클래스
[System.Serializable]
public class EventChoice
{
    [SerializeField]
    private int choiceIndex;
    [SerializeField]
    private string choiceDialog;
    [SerializeField]
    private EventCategory resultCategory;
    [SerializeField]
    private int resultTargetID;

    public string ChoiceDialog => choiceDialog;

    public bool Choice()
    {
        //ToDo : 유효성검사

        switch(resultCategory)
        {
            case EventCategory.Relic:
                Debug.Log($"Get [{resultTargetID}] Relic");
                break;
            case EventCategory.Unit:
                Debug.Log($"Get [{resultTargetID}] Unit");
                break;
            case EventCategory.Battle:
                Debug.Log($"Go To Battle Scene");
                break;
            case EventCategory.Event:
                //ToDo : 이벤트 DB 에서 해당 번호의 이벤트를 뽑아와 여기로 전달.
                //ChoiceEventManager.Instance.OpenChoiceEvent(resultTargetID); //
                #region 테스트용

                ChoiceEventManager.Instance.OpenChoiceEvent(ChoiceEventManager.Instance.testNextEventSO);
                return false;
                #endregion
            case EventCategory.Upgrade:
                break;
            case EventCategory.Flag:
                break;
            default:
                return false;
        }

        return true;
    }
    //public ChoiceEventResultSO choiceResult;
}
//전체 이벤트 리스트를 관리할 용도
public class ChoiceEventList : IDataList<ChoiceEventSO>
{
    public List<ChoiceEventSO> eventList;
    public List<ChoiceEventSO> GetList() => eventList;
}

public enum EventCategory
{
    Relic,
    Unit,
    Battle,
    Event,
    Heal,
    Upgrade,
    Flag,

}