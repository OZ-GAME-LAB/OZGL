using System;
using System.Collections.Generic;
using OzGameLab01.UI;
using UnityEngine;

namespace OzGameLab01.Managers
{
    public class ChoiceEventManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform choiceArea;
        [SerializeField] private EventUiView eventUIView;

        [Header("Runtime Event Data")]
        [SerializeField] private ChoiceEventSO[] eventPool;

        private readonly List<EventChoice> _choices = new();
        private ChoiceEventSO currentEvent;


        /// <summary>
        /// 선택지 효과가 정상 처리되어 이벤트 UI가 닫힐 때 발생합니다.
        /// </summary>
        public event Action EventCompleted;

        private void Awake()
        {
            ResetState();
        }

        private void ResetState()
        {
            currentEvent = null;
            _choices.Clear();
        }

        public void ChoiceEventReset()
        {
            _choices.Clear();

            if (choiceArea == null)
            {
                return;
            }

            for (int i = choiceArea.childCount - 1; i >= 0; i--)
            {
                Transform child = choiceArea.GetChild(i);
                EventChoiceUI choice = child.GetComponent<EventChoiceUI>();
                if (choice != null)
                {
                    choice.OnChoice -= ChoiceResult;
                }
                Destroy(child.gameObject);
            }
        }
        public void CloseCanvas()
        {
            Debug.Log("canvas close");
            gameObject.SetActive(false);
        }

        public bool OpenRandomEvent()
        {
            if (eventPool == null || eventPool.Length == 0)
            {
                Debug.LogWarning("[ChoiceEventManager] 등록된 런타임 이벤트 데이터가 없습니다.", this);
                return false;
            }

            List<ChoiceEventSO> validEvents = new();

            foreach (ChoiceEventSO choiceEvent in eventPool)
            {
                if (choiceEvent != null && choiceEvent.choices != null && choiceEvent.choices.Count > 0)
                {
                    validEvents.Add(choiceEvent);
                }
            }

            if (validEvents.Count == 0)
            {
                Debug.LogWarning("[ChoiceEventManager] 유효한 런타임 이벤트 데이터가 없습니다.", this);
                return false;
            }

            return OpenChoiceEvent(validEvents[UnityEngine.Random.Range(0, validEvents.Count)]);
        }

        public bool OpenChoiceEvent(ChoiceEventSO choiceEvent)
        {
            if (choiceEvent == null || choiceEvent.choices == null || choiceEvent.choices.Count == 0)
            {
                Debug.LogWarning("[ChoiceEventManager] 유효한 이벤트 데이터가 없어 이벤트를 열 수 없습니다.", this);
                return false;
            }

            //if (choiceArea == null || choicePrefab == null || actionPrefab == null || eventUIView == null)
            if (choiceArea == null || eventUIView == null)
            {
                Debug.LogError("[ChoiceEventManager] 이벤트 UI 참조가 완전히 연결되지 않았습니다.", this);
                return false;
            }

            gameObject.SetActive(true);

            eventUIView.SetTitle(choiceEvent.eventTitle);
            eventUIView.SetDescription(choiceEvent.eventDialog);
            
            if (choiceEvent.eventCategory == EventCategory.Choice)
            {
                eventUIView.ShowChoices(choiceEvent.choices, ChoiceResult);
                SetChoiceList(choiceEvent.choices);
            }
            else if (choiceEvent.eventCategory == EventCategory.Action)
            {
                eventUIView.ShowAction(choiceEvent.choices, ChoiceResult);
                SetChoiceList(choiceEvent.choices);
            }

            return true;
            
            //gameObject.SetActive(true);
            //ChoiceEventReset();
            //currentEvent = choiceEvent;

            //foreach (EventChoice choiceData in choiceEvent.choices)
            //{
            //    if (choiceData == null)
            //    {
            //        continue;
            //    }

            //    choices.Add(choiceData);
            //    EventChoiceUI choiceView;

            //    switch (choiceData.ChoiceCategory)
            //    {
            //        case EventChoiceCategory.Unit:
            //        case EventChoiceCategory.Relic:
            //            choiceView = Instantiate(choicePrefab, choiceArea, false);
            //            choiceView.SetEventChoiceUI(choiceData);
            //            break;
            //        case EventChoiceCategory.Event:
            //        case EventChoiceCategory.Battle:
            //        case EventChoiceCategory.Heal:
            //        case EventChoiceCategory.Upgrade:
            //        case EventChoiceCategory.Flag:
            //        case EventChoiceCategory.Exit:
            //            choiceView = Instantiate(actionPrefab, choiceArea, false);
            //            choiceView.SetEventActionUI(choiceData);
            //            break;
            //        default:
            //            Debug.LogWarning($"[ChoiceEventManager] 지원하지 않는 선택지 유형입니다: {choiceData.ChoiceCategory}", this);
            //            continue;
            //    }

            //    choiceView.eventChoiceIndex = choices.Count - 1;
            //    choiceView.OnChoice -= ChoiceResult;
            //    choiceView.OnChoice += ChoiceResult;
            //}

            //eventUIView.SetTitle(choiceEvent.eventTitle);
            //eventUIView.SetDescription(choiceEvent.eventDialog);
            //return choices.Count > 0;

        }
        public bool SetChoiceList(List<EventChoice> choices)
        {
            if (choiceArea == null) 
            {
                Debug.LogWarning("[ChoiceEventManager] 이벤트의 선택지들이 비어있습니다..", this);
                return false; 
            }

            for (int i = 0; i < choices.Count; i++)
            {
                _choices.Add(choices[i]);
            }
            return true;
        }
        public void ChoiceResult(int choiceIndex)
        {
            if (choiceIndex < 0 || choiceIndex >= _choices.Count)
            {
                Debug.LogWarning($"[ChoiceEventManager] 잘못된 선택 인덱스입니다: {choiceIndex} ChoiceCount : {_choices.Count}", this);
                return;
            }

            ExecuteChoice(_choices[choiceIndex]);

            CloseCanvas();
            EventCompleted?.Invoke();
            //if (ExecuteChoice(choices[choiceIndex]))
            //{
            //    gameObject.SetActive(false);
            //}
        }

        private void ExecuteChoice(EventChoice selectedChoice)
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
                    Debug.Log("Go To Battle Scene");
                    break;
                case EventChoiceCategory.Event:
                    Debug.LogWarning(
                        $"[ChoiceEventManager] 이벤트 연결 데이터가 아직 등록되지 않았습니다. Target ID: {selectedChoice.ResultTargetID}",
                        this);
                    break;
                case EventChoiceCategory.Upgrade:
                case EventChoiceCategory.Flag:
                    Debug.LogWarning(
                        $"[ChoiceEventManager] 선택지 효과가 아직 등록되지 않았습니다. Category: {selectedChoice.ChoiceCategory}",
                        this);
                    break;
                case EventChoiceCategory.Heal:
                    Debug.LogWarning("[ChoiceEventManager] 회복 효과가 아직 등록되지 않았습니다.", this);
                    break;
                case EventChoiceCategory.Exit:
                    Debug.Log("Event Exit");
                    break;
                default:
                    return;// false;
            }
            //gameObject.SetActive(false);

            return; // true;


        }
        private bool ExcuteChoicea(EventChoiceCategory choiceCategory, string targetID)
        {

            return true;
        }
        private void ExcuteChoicec(EventChoice choice)
        {

            
        }
    }
}
