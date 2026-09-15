using TMPro;

namespace OzGameLab01.GameFlow.Views
{
    /// <summary>일시정지 버튼 상태 문구 표시</summary>
    public sealed class PauseButtonView
    {
        private readonly TextMeshProUGUI _label;
        public PauseButtonView(TextMeshProUGUI label) => _label = label;
        public void Render(bool paused)
        {
            if (_label != null) _label.text = paused ? "Continue" : "Paused";
        }
    }
}
