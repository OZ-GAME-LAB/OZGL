using OzGameLab01.Managers;
using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.UI;

[CreateAssetMenu(fileName = "ChoiceEventSO_", menuName = "OZGL/Data/ChoiceEvent")]
public class ChoiceEventSO : ScriptableObject
{
    public int id;
    public string eventTitle;
    public string eventDialog;
   // public EventCategory eventCategory;

    public List<EventChoice> choices;
    //public List<EventChoiceDisplayData> choices_;
}
//이벤트 매니저에서 각 선택지의 데이터를 관리할 클래스
[System.Serializable]
public class EventChoice
{
    [SerializeField]
    private int choiceIndex;
    [SerializeField]
    private Sprite choiceSprite;
    [SerializeField]
    private string choiceDialog;
    [SerializeField]
    private EventChoiceCategory choiceCategory;
    [SerializeField]
    private int resultTargetID;

    public string ChoiceDialog => choiceDialog;
    public Sprite ChoiceSprite => choiceSprite;
    public EventChoiceCategory ChoiceCategory => choiceCategory;
    public int ResultTargetID => resultTargetID;
}
//전체 이벤트 리스트를 관리할 용도
public class ChoiceEventList : IDataList<ChoiceEventSO>
{
    public List<ChoiceEventSO> eventList;
    public List<ChoiceEventSO> GetList() => eventList;
}

public enum EventChoiceCategory
{
    Relic,
    Unit,
    Battle,
    Event,
    Heal,
    Upgrade,
    Flag,
    Exit,

}
public enum EventCategory
{
    Choice,
    Action
}