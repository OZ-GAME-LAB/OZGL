using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OzGameLab01.Combat;

namespace OzGameLab01.UI
{
    /// <summary>
    /// 승리 보상 팝업. 클래스별 경험치 바 연출은 레벨업 기능 폐지로 제거했습니다.
    /// 유물 시스템이 만들어지면 cellsContainer에 획득한 유물을 표시할 예정이라
    /// 지금은 배경/제목/확인 버튼만 있는 빈 패널입니다.
    /// </summary>
    public class BattleRewardPanel : MonoBehaviour
    {
        private const string TitleLabel = "victory!";

        [SerializeField] private GameObject panel;
        [SerializeField] private Transform cellsContainer;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TMP_FontAsset koreanFont;

        private Action _onContinue;

        private void Awake()
        {
            if (panel != null)
            {
                EnsureBackground(panel);
                CreateTitle(panel.transform);
                panel.SetActive(false);
            }

            if (cellsContainer == null)
            {
                cellsContainer = CreateCellsContainer();
            }

            if (confirmButton == null)
            {
                confirmButton = CreateConfirmButton();
            }

            confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        public void Show(IEnumerable<Unit> participatingUnits, Action onContinue)
        {
            _onContinue = onContinue;

            // ResultPanel 등 같은 Canvas의 다른 형제보다 항상 위에 그려지도록 보장
            transform.SetAsLastSibling();

            foreach (Transform child in cellsContainer)
            {
                Destroy(child.gameObject);
            }

            if (panel != null)
            {
                panel.SetActive(true);
            }
        }

        private void EnsureBackground(GameObject targetPanel)
        {
            Image background = targetPanel.GetComponent<Image>();
            if (background == null)
            {
                background = targetPanel.AddComponent<Image>();
            }

            background.enabled = true;
            background.color = new Color(0f, 0f, 0f, 0.85f);

            RectTransform rt = targetPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }

        private void CreateTitle(Transform parent)
        {
            TextMeshProUGUI title = CreateText(parent, TitleLabel, 48, TextAlignmentOptions.Center);
            RectTransform rt = title.rectTransform;
            rt.anchorMin = new Vector2(0.1f, 0.82f);
            rt.anchorMax = new Vector2(0.9f, 0.95f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private Transform CreateCellsContainer()
        {
            GameObject container = new GameObject("Cells", typeof(RectTransform), typeof(GridLayoutGroup));
            container.transform.SetParent(panel != null ? panel.transform : transform, false);

            RectTransform rt = container.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.2f);
            rt.anchorMax = new Vector2(0.5f, 0.78f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(700f, 0f);

            GridLayoutGroup layout = container.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(130f, 100f);
            layout.spacing = new Vector2(20f, 20f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;
            layout.childAlignment = TextAnchor.UpperCenter;

            return container.transform;
        }

        private Button CreateConfirmButton()
        {
            GameObject buttonObj = new GameObject("ConfirmButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObj.transform.SetParent(panel != null ? panel.transform : transform, false);

            RectTransform rt = buttonObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.08f);
            rt.anchorMax = new Vector2(0.5f, 0.08f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(200f, 60f);
            buttonObj.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.9f, 1f);

            TextMeshProUGUI label = CreateText(buttonObj.transform, "Check", 26, TextAlignmentOptions.Center);
            RectTransform labelRt = label.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            return buttonObj.GetComponent<Button>();
        }

        private TextMeshProUGUI CreateText(Transform parent, string content, int fontSize, TextAlignmentOptions alignment)
        {
            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(parent, false);

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;

            if (koreanFont != null)
            {
                text.font = koreanFont;
            }

            return text;
        }

        private void OnConfirmClicked()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }

            _onContinue?.Invoke();
        }
    }
}
