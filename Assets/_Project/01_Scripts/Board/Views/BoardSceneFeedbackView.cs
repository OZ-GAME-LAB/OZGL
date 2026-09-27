using OzGameLab01.UI;
using UnityEngine;
namespace OzGameLab01.Board.Views
{
    // 보드 안내 팝업 및 시간 HUD 표시
    public sealed class BoardSceneFeedbackView
    {
        private NightEventPopupView _popup;
        private TimeStatusHUDView _hud;
        public BoardSceneFeedbackView(NightEventPopupView popup, TimeStatusHUDView hud) { _popup = popup; _hud = hud; }
        public void Show(string message, bool createIfMissing = true)
        {
            if (_popup == null) { _popup = Object.FindFirstObjectByType<NightEventPopupView>(); }
            if (_popup == null && createIfMissing) { _popup = new GameObject("NightEventPopup").AddComponent<NightEventPopupView>(); }
            _popup?.Show(message);
        }
        public void ShowUnit(string name, int ownedCount) { Show($"새 유닛 획득!\n[{name}]\n(현재 보유 유닛 {ownedCount}명)"); }
        public void ShowTurns(int remainingTurns)
        {
            if (_hud == null) { _hud = Object.FindFirstObjectByType<TimeStatusHUDView>(); }
            _hud?.SetTurnsUntilNight(remainingTurns);
        }
    }
}
