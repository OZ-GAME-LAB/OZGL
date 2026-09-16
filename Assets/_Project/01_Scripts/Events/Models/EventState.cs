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
    }
}
