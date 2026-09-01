using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    /// <summary>
    /// 화면에 "밤까지 N턴"을 상시 표시하는 HUD입니다.
    ///
    /// NightEventPopupView와 동일하게 자체적으로 Canvas/Text를 생성하므로
    /// 별도 프리팹 없이 씬에 컴포넌트만 추가해도 동작합니다.
    /// </summary>
    public sealed class TimeStatusHUDView : MonoBehaviour
    {
        [SerializeField] private TMP_Text turnsUntilNightText;
        [SerializeField] private TMP_FontAsset koreanFont;

        private void Awake()
        {
            if (GetComponent<Canvas>() == null && GetComponentInParent<Canvas>() == null)
            {
                Canvas canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 50;
                gameObject.AddComponent<CanvasScaler>();
                gameObject.AddComponent<GraphicRaycaster>();
            }

            if (turnsUntilNightText == null)
            {
                turnsUntilNightText = CreateText();
            }
        }

        public void SetTurnsUntilNight(int turns)
        {
            if (turnsUntilNightText != null)
            {
                turnsUntilNightText.text = $"밤까지 {turns}턴";
            }
        }

        private TMP_Text CreateText()
        {
            GameObject textObj = new GameObject("TurnsUntilNightText", typeof(RectTransform));
            textObj.transform.SetParent(transform, false);

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.fontSize = 22;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.color = Color.white;

            if (koreanFont != null)
            {
                text.font = koreanFont;
            }

            RectTransform rt = text.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(20f, -20f);
            rt.sizeDelta = new Vector2(240f, 40f);

            return text;
        }
    }
}
