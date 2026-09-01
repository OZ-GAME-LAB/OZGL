using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OzGameLab01.Combat;
using OzGameLab01.Managers;

namespace OzGameLab01.UI
{
    public class BattleRewardPanel : MonoBehaviour
    {
        private const float ExpPerBattle = 40f;
        private const string TitleLabel = "승리!";

        [SerializeField] private GameObject panel;
        [SerializeField] private Transform cellsContainer;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TMP_FontAsset koreanFont;
        [SerializeField] private float fillAnimationDuration = 2.4f;

        private struct CellVisual
        {
            public Image fillImage;
            public TextMeshProUGUI levelText;
            public float beforeRatio;
            public float afterRatio;
            public bool leveledUp;
            public int finalLevel;
        }

        private Action _onContinue;
        private readonly List<CellVisual> _cells = new List<CellVisual>();

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
            _cells.Clear();

            // 클래스별로 경험치를 한 번씩만 적용 (같은 클래스 유닛이 여러 명이어도 중복 지급하지 않음)
            Dictionary<Unit.SkillType, (float before, float after, bool leveledUp)> progressByType =
                new Dictionary<Unit.SkillType, (float, float, bool)>();

            foreach (Unit unit in participatingUnits)
            {
                if (!progressByType.ContainsKey(unit.Skill))
                {
                    float before = SceneTransitioner.GetAllyExpRatio(unit.Skill);
                    bool leveledUp = SceneTransitioner.AddExp(unit.Skill, ExpPerBattle);
                    float after = SceneTransitioner.GetAllyExpRatio(unit.Skill);
                    progressByType[unit.Skill] = (before, after, leveledUp);
                }

                var progress = progressByType[unit.Skill];
                CreateCell(unit, progress.before, progress.after, progress.leveledUp);
            }

            if (panel != null)
            {
                panel.SetActive(true);
            }

            StopAllCoroutines();
            StartCoroutine(AnimateFillBars());
        }

        private void CreateCell(Unit unit, float beforeRatio, float afterRatio, bool leveledUp)
        {
            GameObject cell = new GameObject(unit.name, typeof(RectTransform));
            cell.transform.SetParent(cellsContainer, false);

            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(cell.transform, false);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 1f);
            iconRt.anchorMax = new Vector2(0.5f, 1f);
            iconRt.pivot = new Vector2(0.5f, 1f);
            iconRt.anchoredPosition = new Vector2(0f, 0f);
            iconRt.sizeDelta = new Vector2(56f, 56f);

            SpriteRenderer spriteRenderer = unit.GetComponentInChildren<SpriteRenderer>();
            Image icon = iconObj.GetComponent<Image>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                icon.sprite = spriteRenderer.sprite;
            }
            else
            {
                icon.color = new Color(1f, 1f, 1f, 0.3f);
            }

            GameObject barBg = new GameObject("BarBackground", typeof(RectTransform), typeof(Image));
            barBg.transform.SetParent(cell.transform, false);
            RectTransform barBgRt = barBg.GetComponent<RectTransform>();
            barBgRt.anchorMin = new Vector2(0.5f, 1f);
            barBgRt.anchorMax = new Vector2(0.5f, 1f);
            barBgRt.pivot = new Vector2(0.5f, 1f);
            barBgRt.anchoredPosition = new Vector2(0f, -62f);
            barBgRt.sizeDelta = new Vector2(90f, 10f);
            barBg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            GameObject barFill = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
            barFill.transform.SetParent(barBg.transform, false);
            RectTransform barFillRt = barFill.GetComponent<RectTransform>();
            barFillRt.anchorMin = Vector2.zero;
            barFillRt.anchorMax = Vector2.one;
            barFillRt.offsetMin = Vector2.zero;
            barFillRt.offsetMax = Vector2.zero;
            Image fillImage = barFill.GetComponent<Image>();
            fillImage.color = new Color(0.3f, 0.8f, 1f, 1f);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = beforeRatio;

            TextMeshProUGUI levelText = CreateText(cell.transform, $"Lv.{SceneTransitioner.GetAllyLevel(unit.Skill) - (leveledUp ? 1 : 0)}", 16, TextAlignmentOptions.Top);
            RectTransform levelRt = levelText.rectTransform;
            levelRt.anchorMin = new Vector2(0.5f, 1f);
            levelRt.anchorMax = new Vector2(0.5f, 1f);
            levelRt.pivot = new Vector2(0.5f, 1f);
            levelRt.anchoredPosition = new Vector2(0f, -76f);
            levelRt.sizeDelta = new Vector2(90f, 20f);

            _cells.Add(new CellVisual
            {
                fillImage = fillImage,
                levelText = levelText,
                beforeRatio = beforeRatio,
                afterRatio = afterRatio,
                leveledUp = leveledUp,
                finalLevel = SceneTransitioner.GetAllyLevel(unit.Skill)
            });
        }

        private IEnumerator AnimateFillBars()
        {
            float half = fillAnimationDuration * 0.5f;

            // 1단계: 레벨업하는 유닛은 100%까지, 아니면 최종 값까지 채움
            float t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float ratio = Mathf.Clamp01(t / half);
                foreach (CellVisual cell in _cells)
                {
                    float target = cell.leveledUp ? 1f : cell.afterRatio;
                    cell.fillImage.fillAmount = Mathf.Lerp(cell.beforeRatio, target, ratio);
                }
                yield return null;
            }

            foreach (CellVisual cell in _cells)
            {
                cell.fillImage.fillAmount = cell.leveledUp ? 1f : cell.afterRatio;
            }

            List<CellVisual> leveledCells = _cells.FindAll(c => c.leveledUp);
            if (leveledCells.Count == 0)
            {
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.3f);

            // 2단계: 레벨업한 유닛만 0으로 리셋 후 이월 경험치만큼 다시 채움 + 레벨 텍스트 갱신
            foreach (CellVisual cell in leveledCells)
            {
                cell.fillImage.fillAmount = 0f;
                cell.levelText.text = $"Lv.{cell.finalLevel}";
            }

            t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float ratio = Mathf.Clamp01(t / half);
                foreach (CellVisual cell in leveledCells)
                {
                    cell.fillImage.fillAmount = Mathf.Lerp(0f, cell.afterRatio, ratio);
                }
                yield return null;
            }

            foreach (CellVisual cell in leveledCells)
            {
                cell.fillImage.fillAmount = cell.afterRatio;
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

            TextMeshProUGUI label = CreateText(buttonObj.transform, "확인", 26, TextAlignmentOptions.Center);
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
