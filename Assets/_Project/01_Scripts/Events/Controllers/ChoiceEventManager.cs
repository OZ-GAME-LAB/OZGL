using System;
using System.Collections.Generic;
using OzGameLab01.Data;
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
        [SerializeField] private ChoiceEventDB eventDB;

        private readonly List<EventChoice> _choices = new();
        private ChoiceEventSO _currentEvent;

        /// <summary>
        /// 선택지 효과가 정상 처리되어 이벤트 UI가 닫힐 때 발생합니다.
        /// </summary>
        public event Action EventCompleted;

        private void Awake()
        {
            eventDB.SetDictionary();
            ResetState();
        }

        private void ResetState()
        {
            _currentEvent = null;
            _choices.Clear();
        }

        public void CloseCanvas()
        {
            gameObject.SetActive(false);
        }

        public bool OpenRandomEvent()
        {
            if (eventDB == null || eventDB.EventList_Event.Count == 0)
            {
                Debug.LogWarning("[ChoiceEventManager] 등록된 런타임 이벤트 데이터가 없습니다.", this);
                return false;
            }

            List<ChoiceEventSO> validEvents = new();

            foreach (ChoiceEventSO choiceEvent in eventDB.EventList_Event)
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

            if (choiceArea == null || eventUIView == null)
            {
                Debug.LogError("[ChoiceEventManager] 이벤트 UI 참조가 완전히 연결되지 않았습니다.", this);
                return false;
            }

            ResetState();

            _currentEvent = choiceEvent;

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
                for(int i=0; i<choiceEvent.choices.Count; i++)
                {
                    //유물의 경우 랜덤 유물의 이미지와 타겟 생성
                    if (choiceEvent.choices[i].ChoiceCategory == EventChoiceCategory.Relic)
                    {
                        RelicData tempRelic = RelicManager.Instance.AcquireRandomRelic();

                        choiceEvent.choices[i].SetEventChoice(tempRelic.name, tempRelic.id.ToString(), null);
                    }
                }
                eventUIView.ShowAction(choiceEvent.choices, ChoiceResult);
                SetChoiceList(choiceEvent.choices);
            }

            return true;
        }
        public bool SetChoiceList(List<EventChoice> choices)
        {
            if (choiceArea == null) 
            {
                Debug.LogWarning("[ChoiceEventManager] 이벤트의 선택지들이 비어있습니다..", this);
                return false; 
            }

            _choices.Clear();

            int a = DataManager.Relics.GetAll().Count;            
            Debug.Log("[setChoiceList]  호출");

            for (int i = 0; i < choices.Count; i++)
            {
                if (choices[i].ChoiceCategory == EventChoiceCategory.Relic)
                {
                    Debug.Log($"랜덤 렐릭 : [{choices[i].ChoiceDialog}]/[{choices[i].ResultTargetID}]");
                }
                else
                {
                    Debug.Log($"add Choice : [{choices[i].ChoiceDialog}]/[{choices[i].ResultTargetID}]");
                }
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

            EventCompleted?.Invoke();
        }

        private void ExecuteChoice(EventChoice selectedChoice)
        {
            switch (selectedChoice.ChoiceCategory)
            {
                case EventChoiceCategory.Relic:
                    Debug.Log($"Get [{selectedChoice.ResultTargetID}] Relic");
                    CloseCanvas();
                    break;
                case EventChoiceCategory.Unit:
                    Debug.Log($"Get [{selectedChoice.ResultTargetID}] Unit");
                    CloseCanvas();
                    break;
                case EventChoiceCategory.Battle:
                    Debug.Log("Go To Battle Scene");
                    CloseCanvas();
                    break;
                case EventChoiceCategory.Quiz:
                    OpenChoiceEvent(eventDB.GetRandomTypeEvent(EventChoiceCategory.Quiz));
                    break;
                case EventChoiceCategory.Event:
                    OpenChoiceEvent(eventDB.GetEventById(selectedChoice.ResultTargetID));
                    Debug.Log($"다음 선택지로 이동[{selectedChoice.ResultTargetID}]");
                    break;
                case EventChoiceCategory.Heal:
                    Debug.LogWarning("[ChoiceEventManager] 회복 효과가 아직 등록되지 않았습니다.", this);
                    CloseCanvas();
                    break;
                case EventChoiceCategory.Exit:
                    Debug.Log("Event Exit");
                    CloseCanvas();
                    break;
                default:
                    return;// false;
            }
            //gameObject.SetActive(false);
            return; // true;
        }
    }
}
