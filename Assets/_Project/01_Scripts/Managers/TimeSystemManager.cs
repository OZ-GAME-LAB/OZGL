using OzGameLab01.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.Managers
{
    /// <summary>
    /// [샌드박스 검증용] 시간 시스템입니다.
    ///
    /// 주사위로 얻은 행동력을 모두 소모했다고 자동으로 턴이 끝나지 않습니다.
    /// 플레이어가 "턴 종료" 버튼을 눌러야 비로소 한 턴이 지나갑니다.
    /// 일정 턴마다 밤 시간이 되어 이벤트 팝업을 띄웁니다.
    ///
    /// 검증이 끝나면 이 로직을 BoardRunData / BoardSceneController에 옮겨 반영합니다.
    /// </summary>
    public class TimeSystemManager : MonoBehaviour
    {
        [Header("시간 시스템 설정")]
        [Tooltip("몇 턴마다 밤 시간이 되는지 설정합니다. (그레이박스 기본값: 3턴)")]
        [SerializeField] private int _nightInterval = 3;

        [Tooltip("밤 시간 도달을 알리는 팝업입니다. 비워두면 씬에서 자동으로 찾거나 새로 생성합니다.")]
        [SerializeField] private NightEventPopupView _nightEventPopup;

        [Tooltip("\"밤까지 N턴\"을 상시 표시하는 HUD입니다. 비워두면 씬에서 자동으로 찾거나 새로 생성합니다.")]
        [SerializeField] private TimeStatusHUDView _timeStatusHud;

        [Tooltip("턴 종료 버튼입니다. 비워두면 자동으로 생성합니다.")]
        [SerializeField] private Button _endTurnButton;

        [Tooltip("버튼 생성 시 사용할 한글 폰트입니다.")]
        [SerializeField] private TMP_FontAsset _koreanFont;

        private int _turnCount;

        private void Start()
        {
            if (_endTurnButton == null)
            {
                _endTurnButton = CreateEndTurnButton();
            }

            _endTurnButton.onClick.AddListener(EndTurn);

            UpdateTimeStatusHud();
        }

        /// <summary>
        /// 행동력을 모두 소모한 뒤 플레이어가 "턴 종료" 버튼을 눌렀을 때 호출됩니다.
        /// 행동력을 소모한 것만으로는 턴이 끝나지 않습니다.
        /// </summary>
        public void EndTurn()
        {
            _turnCount++;

            Debug.Log($"[TimeSystemManager] 턴 종료. TurnCount: {_turnCount}", this);

            if (_nightInterval > 0 && _turnCount % _nightInterval == 0)
            {
                ShowNightEvent();
            }

            UpdateTimeStatusHud();
        }

        /// <summary>
        /// [테스트 전용] 버튼 클릭 없이 턴 종료를 시뮬레이션합니다.
        /// 샌드박스 검증이 끝나면 이 메서드는 제거합니다.
        /// </summary>
        public void DebugSimulateTurn()
        {
            EndTurn();
        }

        /// <summary>
        /// "밤까지 N턴" HUD 표시를 최신 턴 수 기준으로 갱신합니다.
        /// </summary>
        private void UpdateTimeStatusHud()
        {
            if (_nightInterval <= 0)
            {
                return;
            }

            if (_timeStatusHud == null)
            {
                _timeStatusHud = FindFirstObjectByType<TimeStatusHUDView>();
            }

            if (_timeStatusHud == null)
            {
                _timeStatusHud = new GameObject("TimeStatusHUD").AddComponent<TimeStatusHUDView>();
            }

            int turnsUntilNight = _nightInterval - (_turnCount % _nightInterval);
            _timeStatusHud.SetTurnsUntilNight(turnsUntilNight);
        }

        private void ShowNightEvent()
        {
            if (_nightEventPopup == null)
            {
                _nightEventPopup = FindFirstObjectByType<NightEventPopupView>();
            }

            if (_nightEventPopup == null)
            {
                _nightEventPopup = new GameObject("NightEventPopup").AddComponent<NightEventPopupView>();
            }

            Debug.Log($"[TimeSystemManager] 밤 시간 도달. TurnCount: {_turnCount}", this);

            _nightEventPopup.Show(
                $"{_turnCount}턴째, 밤이 되었습니다.\n(발생 이벤트 미정 - 추후 구현)");
        }

        private Button CreateEndTurnButton()
        {
            GameObject canvasObj = new GameObject(
                "EndTurnCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            canvasObj.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            GameObject buttonObj = new GameObject("EndTurnButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObj.transform.SetParent(canvasObj.transform, false);

            RectTransform rt = buttonObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-20f, 20f);
            rt.sizeDelta = new Vector2(160f, 48f);
            buttonObj.GetComponent<Image>().color = new Color(0.8f, 0.3f, 0.2f, 1f);

            GameObject labelObj = new GameObject("Label", typeof(RectTransform));
            labelObj.transform.SetParent(buttonObj.transform, false);

            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = "턴 종료";
            label.fontSize = 20;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;

            if (_koreanFont != null)
            {
                label.font = _koreanFont;
            }

            RectTransform labelRt = label.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            return buttonObj.GetComponent<Button>();
        }
    }
}
