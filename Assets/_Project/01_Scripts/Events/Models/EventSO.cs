using OzGameLab01.Managers;
using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.UI;

namespace OzGameLab01.Events
{
    [CreateAssetMenu(fileName = "EventSO_", menuName = "OzGameLab01/Data/Event")]
    public class EventSO : ScriptableObject
    {
        public string id;
        public string eventTitle;
        public string eventDialog;
        public EventCategory eventCategory;
        public EventChoiceCategory choiceCategory;

        public List<EventChoice> choices;
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
        private string resultTargetID;

        public int ChoiceIndex => choiceIndex;
        public string ChoiceDialog => choiceDialog;
        public Sprite ChoiceSprite => choiceSprite;
        public EventChoiceCategory ChoiceCategory => choiceCategory;
        public string ResultTargetID => resultTargetID;

        public void SetEventChoice(string dialog, string targetID, Sprite sprite = null)
        {
            choiceDialog = dialog;
            choiceSprite = sprite;
            resultTargetID = targetID;
        }
    }
    public enum EventChoiceCategory
    {
        Relic,
        Unit,
        Battle,
        Event,
        Heal,
        Quiz,
        Exit,
    }
    public enum EventCategory
    {
        Choice,
        Action
    }
}
