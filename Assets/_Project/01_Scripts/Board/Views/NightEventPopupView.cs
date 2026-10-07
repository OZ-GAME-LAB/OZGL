using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    /// <summary>
    /// 밤 시간 도달을 알리는 팝업입니다.
    ///
    /// 그레이박스 단계에서는 밤에 발생할 실제 이벤트 내용이 정해지지 않아
    /// 메시지 표시와 확인 버튼만 제공합니다.
    ///
    /// BattleRewardPanel과 동일하게 자체적으로 배경/텍스트/버튼을 생성하므로
    /// 별도 프리팹 없이 씬에 컴포넌트만 추가해도 동작합니다.
    /// </summary>
    public sealed class NightEventPopupView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TMP_FontAsset koreanFont;

        private void Awake()
        {
            if (GetComponent<Canvas>() == null && GetComponentInParent<Canvas>() == null)
            {
                Canvas canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                gameObject.AddComponent<CanvasScaler>();
                gameObject.AddComponent<GraphicRaycaster>();
            }

            if (panel == null)
            {
                panel = CreatePanel();
            }

            if (messageText == null)
            {
                messageText = CreateMessageText();
            }

            if (confirmButton == null)
            {
                confirmButton = CreateConfirmButton();
            }

            confirmButton.onClick.AddListener(Hide);

            panel.SetActive(false);
        }

        public void Show(string message)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }

            transform.SetAsLastSibling();
            panel.SetActive(true);
        }

        public void Hide()
        {
            panel.SetActive(false);
        }

        private GameObject CreatePanel()
        {
            GameObject panelObj = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelObj.transform.SetParent(transform, false);

            RectTransform rt = panelObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            panelObj.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

            return panelObj;
        }

        private TMP_Text CreateMessageText()
        {
            GameObject textObj = new GameObject("Message", typeof(RectTransform));
            textObj.transform.SetParent(panel.transform, false);

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.fontSize = 28;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            if (koreanFont != null)
            {
                text.font = koreanFont;
            }

            RectTransform rt = text.rectTransform;
            rt.anchorMin = new Vector2(0.1f, 0.45f);
            rt.anchorMax = new Vector2(0.9f, 0.65f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return text;
        }

        private Button CreateConfirmButton()
        {
            GameObject buttonObj = new GameObject("ConfirmButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObj.transform.SetParent(panel.transform, false);

            RectTransform rt = buttonObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.35f);
            rt.anchorMax = new Vector2(0.5f, 0.35f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(160f, 48f);
            buttonObj.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.9f, 1f);

            GameObject labelObj = new GameObject("Label", typeof(RectTransform));
            labelObj.transform.SetParent(buttonObj.transform, false);

            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = "확인";
            label.fontSize = 20;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;

            if (koreanFont != null)
            {
                label.font = koreanFont;
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
