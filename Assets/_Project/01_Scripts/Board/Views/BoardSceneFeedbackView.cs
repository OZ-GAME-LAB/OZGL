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
        public void Show(string message, bool createIfMissing = true, System.Action confirmed = null)
        {
            if (_popup == null) { _popup = Object.FindFirstObjectByType<NightEventPopupView>(); }
            if (_popup == null && createIfMissing) { _popup = new GameObject("NightEventPopup").AddComponent<NightEventPopupView>(); }
            _popup?.Show(message, confirmed);
        }
        public void ShowUnit(string name, int ownedCount) { Show($"New Unit Acquired!\n[{name}]\n(Currently {ownedCount} units owned)"); }
        public void ShowTurns(int remainingTurns)
        {
            if (_hud == null) { _hud = Object.FindFirstObjectByType<TimeStatusHUDView>(); }
            if (_hud == null) { _hud = new GameObject("TimeStatusHUD").AddComponent<TimeStatusHUDView>(); }
            _hud.SetTurnsUntilNight(remainingTurns);
        }
    }
}
