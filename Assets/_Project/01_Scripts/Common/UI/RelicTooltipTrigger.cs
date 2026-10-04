using OzGameLab01.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OzGameLab01.UI
{
    /// <summary>
    /// 유물 아이콘의 포인터 진입/이탈에 맞춰 공용 TooltipView를 표시합니다.
    /// 동적으로 생성되는 이벤트 선택지와 전투 결과 아이콘에서도 같은 동작을 재사용합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RelicTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float ScreenOffset = 12f;

        private readonly Vector3[] _anchorCorners = new Vector3[4];
        private TooltipView _tooltipView;
        private RelicData _relicData;
        private RectTransform _anchor;
        private bool _isShowing;

        public void Bind(TooltipView tooltipView, RelicData relicData, RectTransform anchor = null)
        {
            HideTooltip();
            _tooltipView = tooltipView;
            _relicData = relicData;
            _anchor = anchor != null ? anchor : transform as RectTransform;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_tooltipView == null || _relicData == null || _anchor == null)
            {
                return;
            }

            _tooltipView.Show(_relicData.name, _relicData.description, null);

            Canvas canvas = _tooltipView.RootCanvas;
            Camera eventCamera = eventData?.enterEventCamera;
            if (canvas != null)
            {
                eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : eventCamera != null ? eventCamera : canvas.worldCamera;
            }

            _anchor.GetWorldCorners(_anchorCorners);
            Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(
                eventCamera,
                _anchorCorners[2]);

            _tooltipView.SetPivot(new Vector2(0f, 1f));
            _tooltipView.SetScreenPosition(
                screenPosition + Vector2.right * ScreenOffset,
                eventCamera);
            _isShowing = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HideTooltip();
        }

        private void OnDisable()
        {
            HideTooltip();
        }

        private void HideTooltip()
        {
            if (!_isShowing)
            {
                return;
            }

            _tooltipView?.Hide();
            _isShowing = false;
        }
    }
}
