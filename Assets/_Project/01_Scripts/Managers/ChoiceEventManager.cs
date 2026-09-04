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
        [SerializeField]
        private EventChoiceUI actionPrefab;
        [SerializeField]
        private EventUiView eventUIView;
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
            //eventTitleTxt.text = string.Empty;
            //eventDialogTxt.text = string.Empty;

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

            #region 이벤트 자체를 Action 형 이벤트와 Choice 형 이벤트로 나누면 이 쪽을 사용
            //if (!SetEventChoiceByCategory(choiceEventSO.eventCategory, _choices))
            //{
            //    Debug.Log("잘못된 카테고리");
            //    gameObject.SetActive(false);
            //    return false;
            //}
            #endregion

            ChoiceEventReset();

            _choiceEvent = choiceEventSO;

            List<EventChoice> tempChoices = choiceEventSO.choices;

            for (int i = 0; i < tempChoices.Count; i++)
            {
                _choices.Add(_choiceEvent.choices[i]);
                EventChoiceUI eventChoice;
                switch (tempChoices[i].ChoiceCategory)
                {
                    case EventChoiceCategory.Unit:
                    case EventChoiceCategory.Relic:
                        //SetEventChoice(choices);
                        eventChoice = Instantiate(choicePrefab, choiceArea, false);
                        eventChoice.SetEventChoiceUI(_choices[i]);
                        break;
                    case EventChoiceCategory.Event:
                    case EventChoiceCategory.Battle:
                    case EventChoiceCategory.Heal:
                    case EventChoiceCategory.Upgrade:
                    case EventChoiceCategory.Flag:
                    case EventChoiceCategory.Exit:
                        eventChoice = Instantiate(actionPrefab, choiceArea, false);
                        eventChoice.SetEventActionUI(_choices[i]);
                        break;
                    default:
                        Debug.Log("--");
                        return false;
                }
                
                eventChoice.eventChoiceIndex = i;
                eventChoice.OnChoice -= ChoiceResult;
                eventChoice.OnChoice += ChoiceResult;
            }

            eventUIView.SetTitle(choiceEventSO.eventTitle);
            eventUIView.SetDescription(choiceEventSO.eventDialog);
            //eventTitleTxt.text = choiceEventSO.eventTitle;
            //eventDialogTxt.text = choiceEventSO.eventDialog;

            return true;
        }
#region 이벤트 자체를 Action 형 이벤트와 Choice 형 이벤트로 나누면 이 쪽을 사용
        //public bool SetEventChoiceByCategory(EventCategory eventCategory, List<EventChoice> choices)
        //{
        //    switch (eventCategory)
        //    {
        //        case EventCategory.Choice:
        //            SetEventChoice(choices);
        //            break;
        //        case EventCategory.Action:
        //            SetEventAction(choices);
        //            break;
        //        default:
        //            Debug.Log("--");
        //            return false;
        //    }
        //    return true;
        //}
        //public bool SetEventChoice(List<EventChoice> choices)
        //{
        //    if (choices.Count == 0)
        //    {
        //        Debug.LogWarning("이벤트의 선택지가 0개 이하입니다.");
        //        return false;
        //    }

        //    for (int i = 0; i < choices.Count; i++)
        //    {
        //        _choices.Add(_choiceEvent.choices[i]);
        //        EventChoiceUI eventChoice = Instantiate(choicePrefab, choiceArea, false);
        //        eventChoice.SetEventChoiceUI(_choices[i]);
        //        eventChoice.eventChoiceIndex = i;
        //        eventChoice.OnChoice -= ChoiceResult;
        //        eventChoice.OnChoice += ChoiceResult;
        //    }

        //    return true;
        //}
        //public void SetEventAction(List<EventChoice> choices)
        //{
        //    for (int i = 0; i < choices.Count; i++)
        //    {
        //        _choices.Add(_choiceEvent.choices[i]);
        //        EventChoiceUI eventChoice = Instantiate(actionPrefab, choiceArea, false);
        //        eventChoice.SetEventActionUI(_choices[i]);
        //        eventChoice.eventChoiceIndex = i;
        //        eventChoice.OnChoice -= ChoiceResult;
        //        eventChoice.OnChoice += ChoiceResult;
        //    }
        //}

        #endregion
        //선택지 선택
        public void ChoiceResult(int choiceIndex)
        {
            //_choices[choiceIndex].choiceResult.Choice();//선택지 실행
            if (ExecuteChoice(_choices[choiceIndex]))//선택지 실행 > 다음 이벤트를 실행하는 경우가 아니라면 패널 닫기
            {
                gameObject.SetActive(false);
            }
        }
        public void RandomEventOpenTest()
        {
            OpenChoiceEvent(testChoiceEvents[Random.Range(0, testChoiceEvents.Length)]);
        }
        public bool ExecuteChoice(EventChoice selectedChoice)
        {
            switch (selectedChoice.ChoiceCategory)
            {
                case EventChoiceCategory.Relic:
                    Debug.Log($"Get [{selectedChoice.ResultTargetID}] Relic");
                    break;
                case EventChoiceCategory.Unit:
                    Debug.Log($"Get [{selectedChoice.ResultTargetID}] Unit");
                    break;
                case EventChoiceCategory.Battle:
                    Debug.Log($"Go To Battle Scene");
                    break;
                case EventChoiceCategory.Event:
                    //ToDo : 이벤트 DB 에서 해당 번호의 이벤트를 뽑아와 여기로 전달.
                    //ChoiceEventManager.Instance.OpenChoiceEvent(resultTargetID); //
                    #region 테스트용

                    OpenChoiceEvent(testNextEventSO);
                    return false;
                #endregion
                case EventChoiceCategory.Upgrade:
                    break;
                case EventChoiceCategory.Flag:
                    break;
                case EventChoiceCategory.Exit:
                    Debug.Log($"Event Exit");
                    break;
                default:
                    return false;
            }

            return true;
        }
    }
}