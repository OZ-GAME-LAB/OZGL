using TMPro;
namespace OzGameLab01.Board.Views
{
    // 행동력 숫자 표시
    public sealed class BoardActionPointView
    {
        public void Show(TextMeshProUGUI text, int value) { if (text != null) { text.text = value.ToString(); } }
    }
}
