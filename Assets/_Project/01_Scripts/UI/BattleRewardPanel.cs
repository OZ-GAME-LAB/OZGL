using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Combat;
using OzGameLab01.Managers;

namespace OzGameLab01.UI
{
    public class BattleRewardPanel : MonoBehaviour
    {
        private const float ExpPerBattle = 40f;

        [SerializeField] private GameObject panel;
        [SerializeField] private Transform rowsContainer;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TMP_FontAsset koreanFont;
        [SerializeField] private float fillAnimationDuration = 0.6f;

        private struct RowVisual
        {
            public Image fillImage;
            public TextMeshProUGUI levelText;
            public float beforeRatio;
            public float afterRatio;
            public bool leveledUp;
            public int finalLevel;
        }

        private Action _onContinue;
        private readonly List<RowVisual> _rows = new List<RowVisual>();

        private void Awake()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }

            if (rowsContainer == null)
            {
                rowsContainer = CreateRowsContainer();
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

            foreach (Transform child in rowsContainer)
            {
                Destroy(child.gameObject);
            }
            _rows.Clear();

            // 클래스별로 경험치를 한 번씩만 적용 (같은 클래스 유닛이 여러 명이어도 중복 지급하지 않음)
            Dictionary<Unit.SkillType, (float before, float after, bool leveledUp)> progressByType =
                new Dictionary<Unit.SkillType, (float, float, bool)>();

            Dictionary<Unit.SkillType, int> instanceCounters = new Dictionary<Unit.SkillType, int>();

            foreach (Unit unit in participatingUnits)
            {
                if (!progressByType.ContainsKey(unit.Skill))
                {
                    float before = SceneTransitioner.GetAllyExpRatio(unit.Skill);
                    bool leveledUp = SceneTransitioner.AddExp(unit.Skill, ExpPerBattle);
                    float after = SceneTransitioner.GetAllyExpRatio(unit.Skill);
                    progressByType[unit.Skill] = (before, after, leveledUp);
                }

                instanceCounters.TryGetValue(unit.Skill, out int count);
                count++;
                instanceCounters[unit.Skill] = count;

                var progress = progressByType[unit.Skill];
                string label = $"{SkillTypeLabel(unit.Skill)} #{count}";
                CreateRow(label, unit.Skill, progress.before, progress.after, progress.leveledUp);
            }

            if (panel != null)
            {
                panel.SetActive(true);
            }

            StopAllCoroutines();
            StartCoroutine(AnimateFillBars());
        }

        private static string SkillTypeLabel(Unit.SkillType skillType)
        {
            switch (skillType)
            {
                case Unit.SkillType.Warrior:
                    return "전사";
                case Unit.SkillType.Archer:
                    return "궁수";
                case Unit.SkillType.Mage:
                    return "마법사";
                default:
                    return skillType.ToString();
            }
        }

        private void CreateRow(string label, Unit.SkillType skillType, float beforeRatio, float afterRatio, bool leveledUp)
        {
            GameObject row = new GameObject(label, typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(rowsContainer, false);
            row.GetComponent<LayoutElement>().preferredHeight = 60f;

            TextMeshProUGUI nameText = CreateText(row.transform, label, 22, TextAlignmentOptions.MidlineLeft);
            RectTransform nameRt = nameText.rectTransform;
            nameRt.anchorMin = new Vector2(0f, 0f);
            nameRt.anchorMax = new Vector2(0.3f, 1f);
            nameRt.offsetMin = Vector2.zero;
            nameRt.offsetMax = Vector2.zero;

            GameObject barBg = new GameObject("BarBackground", typeof(RectTransform), typeof(Image));
            barBg.transform.SetParent(row.transform, false);
            RectTransform barBgRt = barBg.GetComponent<RectTransform>();
            barBgRt.anchorMin = new Vector2(0.32f, 0.3f);
            barBgRt.anchorMax = new Vector2(0.85f, 0.7f);
            barBgRt.offsetMin = Vector2.zero;
            barBgRt.offsetMax = Vector2.zero;
            barBg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

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

            TextMeshProUGUI levelText = CreateText(row.transform, $"Lv.{SceneTransitioner.GetAllyLevel(skillType) - (leveledUp ? 1 : 0)}", 22, TextAlignmentOptions.MidlineRight);
            RectTransform levelRt = levelText.rectTransform;
            levelRt.anchorMin = new Vector2(0.87f, 0f);
            levelRt.anchorMax = new Vector2(1f, 1f);
            levelRt.offsetMin = Vector2.zero;
            levelRt.offsetMax = Vector2.zero;

            _rows.Add(new RowVisual
            {
                fillImage = fillImage,
                levelText = levelText,
                beforeRatio = beforeRatio,
                afterRatio = afterRatio,
                leveledUp = leveledUp,
                finalLevel = SceneTransitioner.GetAllyLevel(skillType)
            });
        }

        private IEnumerator AnimateFillBars()
        {
            float half = fillAnimationDuration * 0.5f;

            // 1단계: 레벨업하는 유닛은 100%까지, 아니면 최종 값까지 채움
            float t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                float ratio = Mathf.Clamp01(t / half);
                foreach (RowVisual row in _rows)
                {
                    float target = row.leveledUp ? 1f : row.afterRatio;
                    row.fillImage.fillAmount = Mathf.Lerp(row.beforeRatio, target, ratio);
                }
                yield return null;
            }

            foreach (RowVisual row in _rows)
            {
                row.fillImage.fillAmount = row.leveledUp ? 1f : row.afterRatio;
            }

            List<RowVisual> leveledRows = _rows.FindAll(r => r.leveledUp);
            if (leveledRows.Count == 0)
            {
                yield break;
            }

            // 2단계: 레벨업한 유닛만 0으로 리셋 후 이월 경험치만큼 다시 채움 + 레벨 텍스트 갱신
            foreach (RowVisual row in leveledRows)
            {
                row.fillImage.fillAmount = 0f;
                row.levelText.text = $"Lv.{row.finalLevel}";
            }

            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                float ratio = Mathf.Clamp01(t / half);
                foreach (RowVisual row in leveledRows)
                {
                    row.fillImage.fillAmount = Mathf.Lerp(0f, row.afterRatio, ratio);
                }
                yield return null;
            }

            foreach (RowVisual row in leveledRows)
            {
                row.fillImage.fillAmount = row.afterRatio;
            }
        }

        private Transform CreateRowsContainer()
        {
            GameObject container = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            container.transform.SetParent(panel != null ? panel.transform : transform, false);

            RectTransform rt = container.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.15f, 0.2f);
            rt.anchorMax = new Vector2(0.85f, 0.85f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = container.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            return container.transform;
        }

        private Button CreateConfirmButton()
        {
            GameObject buttonObj = new GameObject("ConfirmButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObj.transform.SetParent(panel != null ? panel.transform : transform, false);

            RectTransform rt = buttonObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.05f);
            rt.anchorMax = new Vector2(0.5f, 0.05f);
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
