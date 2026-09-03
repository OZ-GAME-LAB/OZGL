using TMPro;
using UnityEngine;
using System.Collections.Generic;
using OzGameLab01.UI;

namespace OzGameLab01.Managers
{
    public class ChoiceEventManager : Singleton<ChoiceEventManager>
    {
        //인스펙터에 등록할 외부변수
        [SerializeField]
        private TextMeshProUGUI eventTitleTxt;
        [SerializeField]
        private TextMeshProUGUI eventDialogTxt;
        [SerializeField]
        private Transform choiceArea;
        [SerializeField]
        private EventChoiceUI choicePrefab;
        //public Image eventIllustImg;
        

        #region 테스트용
        public ChoiceEventSO[] testChoiceEvents;
        public ChoiceEventSO testNextEventSO;
        #endregion
        //내부변수
        private List<EventChoice> _choices;
        private ChoiceEventSO _choiceEvent;
        
        protected override void Awake()
        {
            base.Awake();
            Init();
        }
        public void Init()
        {
            _choiceEvent = null;
            _choices = new List<EventChoice>();
        }
        //매 이벤트를 불러오기 전 초기화 작업
        public void ChoiceEventReset()
        {
            eventTitleTxt.text = string.Empty;
            eventDialogTxt.text = string.Empty;

            _choices.Clear();

            for (int i = choiceArea.childCount - 1; i >= 0; i--)
            {
                EventChoiceUI tempChoiceUI = choiceArea.GetChild(i).GetComponent<EventChoiceUI>();
                tempChoiceUI.OnChoice -= ChoiceResult;
                Destroy(tempChoiceUI.gameObject);
            }
        }
        //이벤트 오픈
        public bool OpenChoiceEvent(ChoiceEventSO choiceEventSO)
        {
            //ToDo: choiceEvent 유효성검사
            /*
             * return false;
             */

            gameObject.SetActive(true);

            ChoiceEventReset();

            _choiceEvent = choiceEventSO;

            eventTitleTxt.text = choiceEventSO.eventTitle;
            eventDialogTxt.text = choiceEventSO.eventDialog;

            for (int i=0; i<choiceEventSO.choices.Count; i++)
            {
                _choices.Add(_choiceEvent.choices[i]);
                EventChoiceUI eventChoice = Instantiate(choicePrefab,choiceArea,false);
                eventChoice.SetEventChoiceUI(_choices[i]);
                eventChoice.eventChoiceIndex = i;
                eventChoice.OnChoice -= ChoiceResult;
                eventChoice.OnChoice += ChoiceResult;
            }

            return true;
        }
        //선택지 선택
        public void ChoiceResult(int choiceIndex)
        {
            //_choices[choiceIndex].choiceResult.Choice();//선택지 실행
            if (_choices[choiceIndex].Choice())//선택지 실행 > 다음 이벤트를 실행하는 경우가 아니라면 패널 닫기
            {
                gameObject.SetActive(false);
            }
        }
        public void RandomEventOpenTest()
        {
            OpenChoiceEvent(testChoiceEvents[Random.Range(0, testChoiceEvents.Length)]);
        }
    }
}