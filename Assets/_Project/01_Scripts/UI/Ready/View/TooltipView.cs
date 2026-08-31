using TMPro;
using UnityEngine;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class TooltipView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private RectTransform tooltipPanel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;

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
                {
                    titleText.text = value ?? string.Empty;
                }
            }
        }

        public string Description
        {
            get => descriptionText != null ? descriptionText.text : string.Empty;
            set
            {
                if (descriptionText != null)
                {
                    descriptionText.text = value ?? string.Empty;
                }
            }
        }

        #endregion

        #region API

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Show(string title, string description)
        {
            SetTitle(title);
            SetDescription(description);
            Show();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
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
            {
                titleText.gameObject.SetActive(visible);
            }
        }

        public void SetPosition(Vector2 anchoredPosition)
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.anchoredPosition = anchoredPosition;
            }
        }

        public void SetPivot(Vector2 pivot)
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.pivot = pivot;
            }
        }

        public void SetSize(Vector2 size)
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.sizeDelta = size;
            }
        }

        public void SetScreenPosition(Vector2 screenPosition, Camera eventCamera = null)
        {
            if (rootCanvas == null || tooltipPanel == null)
            {
                return;
            }

            RectTransform canvasRect = rootCanvas.transform as RectTransform;

            if (canvasRect == null)
            {
                return;
            }

            Camera camera = GetCanvasCamera(eventCamera);

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,screenPosition,camera,out Vector2 localPosition))
            {
                tooltipPanel.anchoredPosition = localPosition;
            }
        }

        #endregion

        #region Private Methods

        private Camera GetCanvasCamera(Camera eventCamera)
        {
            if (rootCanvas == null || rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return eventCamera != null ? eventCamera : rootCanvas.worldCamera;
        }

        #endregion
    }
}