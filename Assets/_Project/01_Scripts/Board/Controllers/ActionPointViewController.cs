using UnityEngine;
using TMPro; // TextMeshPro 사용

namespace OzGameLab01.UI
{
    /// <summary>
    /// 하단 메인 UI의 행동력(Action Point) 수치를 실시간으로 표시하는 전용 컨트롤러입니다.
    /// 플레이어가 이동하여 행동력이 깎이거나 주사위를 굴려 늘어나는 것을 모두 자동 감지합니다.
    /// </summary>
    public class ActionPointViewController : MonoBehaviour
    {
        [Header("UI 연결")]
        [Tooltip("행동력 숫자가 뜰 텍스트 (ActionPoint - ValueText)")]
        public TextMeshProUGUI valueText;

        // 숫자가 바뀔 때만 텍스트를 갱신하기 위해 이전 값을 기억해두는 변수입니다.
        private int _lastValue = -1;

        private void Update()
        {
            // 아직 플레이어 컨트롤러가 씬에 없다면 무시합니다.
            if (Controllers.BoardPlayerController.Instance == null) return;

            // 현재 플레이어의 진짜 남은 행동력을 가져옵니다.
            int currentActionPoint = Controllers.BoardPlayerController.Instance.CurrentDiceValue;

            // 만약 내가 화면에 띄워둔 숫자랑 현재 행동력이 다르다면? (즉, 변화가 생겼다면)
            if (currentActionPoint != _lastValue)
            {
                // 숫자를 업데이트하고 기억합니다.
                _lastValue = currentActionPoint;

                if (valueText != null)
                {
                    valueText.text = currentActionPoint.ToString();
                }
            }
        }
    }
}