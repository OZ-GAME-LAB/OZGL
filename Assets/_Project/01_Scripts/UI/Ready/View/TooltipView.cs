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
            SetEffects(effects);
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
                LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipPanel);

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