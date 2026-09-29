using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class TooltipView : MonoBehaviour
    {
        /// <summary>
        /// 외부에서 전달하는 효과 표시 데이터입니다.
        /// IsActive는 외부에서 결정하며 View는 색상만 적용합니다.
        /// </summary>
        [Serializable]
        public struct EffectData
        {
            public string Text;
            public bool IsActive;

            public EffectData(string text, bool isActive = true)
            {
                Text = text;
                IsActive = isActive;
            }
        }

        [Header("References")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private RectTransform tooltipPanel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;

        [Header("Effects")]
        [SerializeField] private RectTransform effectListRoot;
        [SerializeField] private EffectRowView effectRowTemplate;

        [Tooltip("간격용 오브젝트가 있을 때만 연결합니다.")]
        [SerializeField] private GameObject effectSpacer;

        [SerializeField]
        private Color activeEffectColor = Color.white;

        [SerializeField]
        private Color inactiveEffectColor = new (0.45f, 0.45f, 0.45f, 1f);

        [Header("Layout")]
        [Tooltip("Canvas 좌표 기준 가장자리 여백입니다.")]
        [SerializeField, Min(0f)]
        private float edgePadding = 12f;

        [SerializeField, Min(0f)] private float contentHorizontalPadding = 20f;
        [SerializeField, Min(0f)] private float contentTopPadding = 8f;
        [SerializeField, Min(0f)] private float contentBottomPadding = 12f;
        [SerializeField, Min(0f)] private float sectionSpacing = 6f;
        [SerializeField, Min(1f)] private float minimumPanelWidth = 600f;
        [SerializeField, Min(1f)] private float maximumPanelWidth = 1200f;
        [SerializeField, Min(1f)] private float minimumPanelHeight = 80f;

        private readonly List<EffectRowView> effectRows = new();

        private readonly Vector3[] panelCorners = new Vector3[4];

        private bool initialized;

        #region Properties

        public Canvas RootCanvas => rootCanvas;
        public RectTransform TooltipPanel => tooltipPanel;
        public TMP_Text TitleText => titleText;
        public TMP_Text DescriptionText => descriptionText;

        public bool IsVisible => gameObject.activeSelf;

        public string Title
        {
            get => titleText != null ? titleText.text : string.Empty;
            set
            {
                if (titleText != null)
                    titleText.text = value ?? string.Empty;
            }
        }

        public string Description
        {
            get => descriptionText != null ? descriptionText.text : string.Empty;
            set
            {
                if (descriptionText != null)
                    descriptionText.text = value ?? string.Empty;
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
        }

        #endregion

        #region Visibility API

        /// <summary>
        /// 현재 설정된 내용으로 표시합니다.
        /// 표시 시점은 외부에서 결정합니다.
        /// </summary>
        public void Show()
        {
            Initialize();
            gameObject.SetActive(true);
            RebuildContentLayout();
        }

        /// <summary>
        /// 제목과 설명을 설정하고 표시합니다.
        /// 이전 효과 목록은 비웁니다.
        /// </summary>
        public void Show(string title, string description)
        {
            Show(title, description, null);
        }

        public void Show(string title, string description,IReadOnlyList<EffectData> effects)
        {
            SetContent(title, description, effects);
            Show();
        }

        /// <summary>
        /// 내용을 유지한 채 숨깁니다.
        /// 숨김 시점은 외부에서 결정합니다.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion

        #region Content API

        /// <summary>
        /// 표시 여부를 변경하지 않고 내용만 갱신합니다.
        /// effects가 null이면 이전 효과 목록을 비웁니다.
        /// </summary>
        public void SetContent(
            string title,
            string description,
            IReadOnlyList<EffectData> effects = null)
        {
            Initialize();

            SetTitle(title);
            SetTitleVisible(!string.IsNullOrEmpty(title));
            SetDescription(description);
            if (descriptionText != null)
                descriptionText.gameObject.SetActive(!string.IsNullOrWhiteSpace(description));
            SetEffects(effects);

            if (gameObject.activeInHierarchy)
                RebuildContentLayout();
        }

        /// <summary>
        /// 표시 여부를 변경하지 않고 모든 내용을 비웁니다.
        /// </summary>
        public void ClearContent()
        {
            SetContent(string.Empty, string.Empty, null);
        }

        public void SetTitle(string value)
        {
            Title = value;
        }

        public void SetDescription(string value)
        {
            Description = value;
        }

        public void SetTitleVisible(bool visible)
        {
            if (titleText != null)
                titleText.gameObject.SetActive(visible);
        }

        public void SetEffects(IReadOnlyList<EffectData> effects)
        {
            Initialize();

            int count = effects != null ? effects.Count : 0;

            if (count == 0)
            {
                HideEffectRows();
                return;
            }

            if (effectListRoot == null || effectRowTemplate == null)
            {
                HideEffectRows();

                Debug.LogWarning("[TooltipView] Effect List Root와 " +"Effect Row Template을 연결하세요.", this);

                return;
            }

            while (effectRows.Count < count)
            {
                effectRows.Add(CreateEffectRow());
            }

            for (int i = 0; i < effectRows.Count; i++)
            {
                bool visible = i < count;
                EffectRowView row = effectRows[i];

                if (row == null)
                {
                    if (!visible)
                        continue;

                    row = CreateEffectRow();
                    effectRows[i] = row;
                }

                if (visible)
                {
                    EffectData effect = effects[i];
                    row.Bind(effect.Text,effect.IsActive ? activeEffectColor : inactiveEffectColor);
                }

                row.gameObject.SetActive(visible);
            }

            SetEffectAreaVisible(true);
            RebuildContentLayout();
        }

        #endregion

        #region Layout API

        /// <summary>
        /// 외부에서 사용할 Canvas를 명시적으로 지정할 수 있습니다.
        /// </summary>
        public void SetRootCanvas(Canvas canvas)
        {
            rootCanvas = canvas;
        }

        public void SetPosition(Vector2 anchoredPosition)
        {
            if (tooltipPanel != null)
                tooltipPanel.anchoredPosition = anchoredPosition;
        }

        public void SetPivot(Vector2 pivot)
        {
            if (tooltipPanel != null)
                tooltipPanel.pivot = pivot;
        }

        public void SetSize(Vector2 size)
        {
            if (tooltipPanel != null)
                tooltipPanel.sizeDelta = size;
        }

        public void RebuildContentLayout()
        {
            Initialize();

            if (tooltipPanel == null)
                return;

            float panelWidth = GetPreferredPanelWidth();
            tooltipPanel.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                panelWidth);

            float contentWidth = Mathf.Max(1f, panelWidth - contentHorizontalPadding * 2f);
            float cursor = contentTopPadding;

            if (IsTextVisible(titleText))
            {
                float titleHeight = GetPreferredTextHeight(titleText, contentWidth);
                ConfigureSection(titleText, cursor, titleHeight);
                cursor += titleHeight;
            }

            if (IsTextVisible(descriptionText))
            {
                if (cursor > contentTopPadding)
                    cursor += sectionSpacing;

                float descriptionHeight = GetPreferredTextHeight(descriptionText, contentWidth);
                ConfigureSection(descriptionText, cursor, descriptionHeight);
                cursor += descriptionHeight;
            }

            if (effectListRoot != null && effectListRoot.gameObject.activeSelf)
            {
                if (cursor > contentTopPadding)
                    cursor += sectionSpacing;

                float effectHeight = GetEffectListHeight(contentWidth);
                ConfigureTopSection(effectListRoot, cursor, effectHeight);
                cursor += effectHeight;
            }

            tooltipPanel.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                Mathf.Max(minimumPanelHeight, cursor + contentBottomPadding));
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipPanel);
        }

        /// <summary>
        /// 외부에서 전달받은 화면 좌표에 배치합니다.
        /// 마우스 위치 추적이나 오프셋 계산은 하지 않습니다.
        /// 배치 후 Canvas 경계 안으로 위치를 보정합니다.
        /// </summary>
        public void SetScreenPosition(Vector2 screenPosition,Camera eventCamera = null)
        {
            Initialize();

            Canvas canvas = ResolveCanvas();

            if (canvas == null || tooltipPanel == null)
                return;

            RectTransform parentRect = tooltipPanel.parent as RectTransform;

            if (parentRect == null)
                return;

            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : eventCamera != null ? eventCamera : canvas.worldCamera;

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parentRect, screenPosition, camera, out Vector3 worldPosition))
            {
                tooltipPanel.position = worldPosition;
                ClampToCanvas();
            }
        }

        /// <summary>
        /// 내용, 크기 또는 위치 변경 후 필요할 때 호출합니다.
        /// 패널 자체가 Canvas보다 크면 중앙에 맞춥니다.
        /// </summary>
        public void ClampToCanvas()
        {
            Canvas canvas = ResolveCanvas();

            if (canvas == null || tooltipPanel == null)
                return;

            RectTransform canvasRect = canvas.transform as RectTransform;

            if (canvasRect == null)
                return;

            if (tooltipPanel.gameObject.activeInHierarchy)
                RebuildContentLayout();

            tooltipPanel.GetWorldCorners(panelCorners);

            Vector2 min = new Vector2( float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            foreach (Vector3 corner in panelCorners)
            {
                Vector3 localCorner = canvasRect.InverseTransformPoint(corner);
                Vector2 point = new ( localCorner.x, localCorner.y);

                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            Rect bounds = canvasRect.rect;

            float padding = Mathf.Clamp( edgePadding, 0f, Mathf.Min(bounds.width, bounds.height) * 0.5f);
            float offsetX = GetAxisCorrection( min.x, max.x, bounds.xMin + padding, bounds.xMax - padding);
            float offsetY = GetAxisCorrection(min.y, max.y, bounds.yMin + padding, bounds.yMax - padding);

            tooltipPanel.position += canvasRect.TransformVector(new Vector3(offsetX, offsetY, 0f));
        }

        #endregion

        #region Internal

        private void Initialize()
        {
            if (initialized)
                return;

            initialized = true;

            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            DisableRaycasts(gameObject);

            if (effectRowTemplate != null && effectRowTemplate.transform.IsChildOf(transform))
            {
                effectRowTemplate.gameObject.SetActive(false);
            }

            if (effectListRoot != null &&
                effectListRoot.TryGetComponent(out VerticalLayoutGroup effectLayout))
            {
                effectLayout.childControlWidth = true;
                effectLayout.childControlHeight = true;
                effectLayout.childForceExpandHeight = false;
            }
        }

        private Canvas ResolveCanvas()
        {
            return rootCanvas != null ? rootCanvas : GetComponentInParent<Canvas>(true);
        }

        private EffectRowView CreateEffectRow()
        {
            EffectRowView row = Instantiate(effectRowTemplate,effectListRoot,false);

            row.gameObject.SetActive(false);
            DisableRaycasts(row.gameObject);

            return row;
        }

        private void HideEffectRows()
        {
            foreach (EffectRowView row in effectRows)
            {
                if (row != null)
                    row.gameObject.SetActive(false);
            }

            SetEffectAreaVisible(false);
        }

        private void SetEffectAreaVisible(bool visible)
        {
            if (effectListRoot != null)
                effectListRoot.gameObject.SetActive(visible);

            if (effectSpacer != null)
                effectSpacer.SetActive(visible);
        }

        private float GetEffectListHeight(float availableWidth)
        {
            int visibleCount = 0;
            float height = 0f;

            foreach (EffectRowView row in effectRows)
            {
                if (row == null || !row.gameObject.activeSelf)
                    continue;

                row.RefreshLayout(availableWidth);
                height += row.PreferredHeight;
                visibleCount++;
            }

            if (effectListRoot != null &&
                effectListRoot.TryGetComponent(out VerticalLayoutGroup layout))
            {
                height += layout.padding.top + layout.padding.bottom;
                height += Mathf.Max(0, visibleCount - 1) * layout.spacing;
            }

            return height;
        }

        private float GetPreferredPanelWidth()
        {
            float preferredContentWidth = 0f;

            if (IsTextVisible(titleText))
                preferredContentWidth = Mathf.Max(preferredContentWidth, GetPreferredTextWidth(titleText));

            if (IsTextVisible(descriptionText))
                preferredContentWidth = Mathf.Max(preferredContentWidth, GetPreferredTextWidth(descriptionText));

            foreach (EffectRowView row in effectRows)
            {
                if (row == null || !row.gameObject.activeSelf)
                    continue;

                preferredContentWidth = Mathf.Max(preferredContentWidth, row.GetPreferredWidth());
            }

            float availableCanvasWidth = GetAvailableCanvasWidth();
            float maximumWidth = Mathf.Max(minimumPanelWidth, maximumPanelWidth);

            if (availableCanvasWidth > 0f)
                maximumWidth = Mathf.Min(maximumWidth, availableCanvasWidth);

            float minimumWidth = Mathf.Min(minimumPanelWidth, maximumWidth);
            float preferredWidth = Mathf.Ceil(
                preferredContentWidth + contentHorizontalPadding * 2f);

            return Mathf.Clamp(preferredWidth, minimumWidth, maximumWidth);
        }

        private float GetAvailableCanvasWidth()
        {
            Canvas canvas = ResolveCanvas();
            RectTransform canvasRect = canvas != null
                ? canvas.transform as RectTransform
                : null;

            if (canvasRect == null)
                return -1f;

            return Mathf.Max(1f, canvasRect.rect.width - edgePadding * 2f);
        }

        private void ConfigureSection(TMP_Text text, float top, float height)
        {
            if (text == null)
                return;

            RectTransform section = text.rectTransform.parent as RectTransform;
            if (section == null)
                section = text.rectTransform;

            ConfigureTopSection(section, top, height);

            if (section != text.rectTransform)
            {
                RectTransform textRect = text.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
            }
        }

        private void ConfigureTopSection(RectTransform section, float top, float height)
        {
            section.anchorMin = new Vector2(0f, 1f);
            section.anchorMax = new Vector2(1f, 1f);
            section.pivot = new Vector2(0.5f, 1f);
            section.anchoredPosition = new Vector2(0f, -top);
            section.sizeDelta = new Vector2(-contentHorizontalPadding * 2f, height);
        }

        private static bool IsTextVisible(TMP_Text text)
        {
            return text != null &&
                   text.gameObject.activeSelf &&
                   !string.IsNullOrWhiteSpace(text.text);
        }

        private static float GetPreferredTextHeight(TMP_Text text, float width)
        {
            return Mathf.Max(
                1f,
                Mathf.Ceil(text.GetPreferredValues(
                    text.text ?? string.Empty,
                    width,
                    Mathf.Infinity).y));
        }

        private static float GetPreferredTextWidth(TMP_Text text)
        {
            return Mathf.Max(
                1f,
                Mathf.Ceil(text.GetPreferredValues(
                    text.text ?? string.Empty,
                    Mathf.Infinity,
                    Mathf.Infinity).x));
        }

        private static void DisableRaycasts(GameObject target)
        {
            foreach (Graphic graphic in target.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }
        }

        private static float GetAxisCorrection(float min,float max,float limitMin,float limitMax)
        {
            if (max - min > limitMax - limitMin)
                return (limitMin + limitMax - min - max) * 0.5f;

            if (min < limitMin)
                return limitMin - min;

            if (max > limitMax)
                return limitMax - max;

            return 0f;
        }

        #endregion
    }
}
