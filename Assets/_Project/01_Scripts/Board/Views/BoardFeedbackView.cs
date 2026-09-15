using TMPro;
using UnityEngine;

namespace OzGameLab01.Board.Views
{
    /// <summary>
    /// BoardUIController에서 분리된 텍스트 표시(주사위 결과, 경고 배너) 책임을 담당합니다.
    /// Inspector 참조는 BoardUIController가 그대로 들고 있고, 이 클래스는 그 값을 생성자로
    /// 전달받아 사용하는 순수 C# 클래스입니다(씬/프리팹 재배선 불필요) — UnitPresenter와
    /// 같은 패턴입니다. 타이밍(지연 후 숨기기 등)은 MonoBehaviour인 BoardUIController가
    /// 코루틴으로 소유하고, 이 클래스는 화면 표시 자체만 담당합니다.
    /// </summary>
    public sealed class BoardFeedbackView
    {
        private readonly TextMeshProUGUI resultText;
        private readonly TextMeshProUGUI warningText;

        public BoardFeedbackView(TextMeshProUGUI resultText, TextMeshProUGUI warningText)
        {
            this.resultText = resultText;
            this.warningText = warningText;
        }

        public void SetDiceResult(string text)
        {
            if (resultText != null)
            {
                resultText.text = text;
            }
        }

        public void ShowWarning(string message)
        {
            if (warningText != null)
            {
                warningText.text = message;
                warningText.color = Color.red;
                warningText.gameObject.SetActive(true);
            }
        }

        public void HideWarning()
        {
            if (warningText != null)
            {
                warningText.gameObject.SetActive(false);
            }
        }
    }
}
