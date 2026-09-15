using System.Collections.Generic;

namespace OzGameLab01.Events
{
    /// <summary>
    /// 현재 열린 이벤트의 선택지 목록과 선택 실행 로직을 보관하는 순수 데이터 클래스입니다.
    /// </summary>
    public class EventState
    {
        private readonly List<EventChoice> _choices = new();

        public int ChoiceCount => _choices.Count;

        public void SetChoiceList(List<EventChoice> choices)
        {
            _choices.Clear();
            for (int i = 0; i < choices.Count; i++)
            {
                _choices.Add(choices[i]);
            }
        }

        public EventChoice GetChoice(int index)
        {
            if (index < 0 || index >= _choices.Count)
            {
                return null;
            }

            return _choices[index];
        }

        public void ExecuteChoice(EventChoice selectedChoice)
        {
            switch (selectedChoice.ChoiceCategory)
            {
                case EventChoiceCategory.Relic:
                    UnityEngine.Debug.Log($"Get [{selectedChoice.ResultTargetID}] Relic");
                    break;
                case EventChoiceCategory.Unit:
                    UnityEngine.Debug.Log($"Get [{selectedChoice.ResultTargetID}] Unit");
                    break;
                case EventChoiceCategory.Battle:
                    UnityEngine.Debug.Log("Go To Battle Scene");
                    break;
                case EventChoiceCategory.Event:
                    UnityEngine.Debug.LogWarning(
                        $"[EventState] 이벤트 연결 데이터가 아직 등록되지 않았습니다. Target ID: {selectedChoice.ResultTargetID}");
                    break;
                case EventChoiceCategory.Upgrade:
                case EventChoiceCategory.Flag:
                    UnityEngine.Debug.LogWarning(
                        $"[EventState] 선택지 효과가 아직 등록되지 않았습니다. Category: {selectedChoice.ChoiceCategory}");
                    break;
                case EventChoiceCategory.Heal:
                    UnityEngine.Debug.LogWarning("[EventState] 회복 효과가 아직 등록되지 않았습니다.");
                    break;
                case EventChoiceCategory.Exit:
                    UnityEngine.Debug.Log("Event Exit");
                    break;
            }
        }
    }
}
