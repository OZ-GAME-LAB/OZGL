using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public sealed class TutorialOutlinePulseView : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField] private Outline outline;

        private Tween pulseTween;

        private void Awake()
        {
            ResolveOutline();

            if (outline != null)
                outline.enabled = false;
        }

        private void OnDisable()
        {
            StopHighlight();
        }

        public void PlayHighlight(
            Color outlineColor,
            Vector2 minDistance,
            Vector2 maxDistance,
            float minAlpha,
            float maxAlpha,
            float halfDuration,
            Ease ease,
            bool ignoreTimeScale)
        {
            ResolveOutline();

            if (outline == null)
                return;

            StopTween();

            outline.enabled = true;
            ApplyValue(
                0f,
                outlineColor,
                minDistance,
                maxDistance,
                minAlpha,
                maxAlpha);

            float value = 0f;

            pulseTween = DOTween
                .To(
                    () => value,
                    newValue =>
                    {
                        value = newValue;
                        ApplyValue(
                            value,
                            outlineColor,
                            minDistance,
                            maxDistance,
                            minAlpha,
                            maxAlpha);
                    },
                    1f,
                    Mathf.Max(0.01f,halfDuration))
                .SetEase(ease)
                .SetLoops(-1,LoopType.Yoyo)
                .SetUpdate(ignoreTimeScale);
        }

        public void StopHighlight()
        {
            StopTween();

            if (outline != null)
                outline.enabled = false;
        }

        private void ResolveOutline()
        {
            if (outline != null)
                return;

            outline = GetComponent<Outline>();

            if (outline == null)
                outline = gameObject.AddComponent<Outline>();

            outline.useGraphicAlpha = false;
        }

        private void ApplyValue(
            float value,
            Color outlineColor,
            Vector2 minDistance,
            Vector2 maxDistance,
            float minAlpha,
            float maxAlpha)
        {
            if (outline == null)
                return;

            outline.effectDistance =
                Vector2.Lerp(minDistance,maxDistance,value);

            Color color = outlineColor;
            color.a = Mathf.Lerp(minAlpha,maxAlpha,value);
            outline.effectColor = color;
        }

        private void StopTween()
        {
            pulseTween?.Kill();
            pulseTween = null;
        }

#if UNITY_EDITOR
        [ContextMenu("Test/Play Outline Pulse")]
        private void TestPlayOutlinePulse()
        {
            if (!Application.isPlaying)
                return;

            PlayHighlight(
                new Color(1f,0.8f,0.2f,1f),
                new Vector2(2f,2f),
                new Vector2(8f,8f),
                0.4f,
                1f,
                0.8f,
                Ease.InOutSine,
                true);
        }

        [ContextMenu("Test/Stop Outline Pulse")]
        private void TestStopOutlinePulse()
        {
            StopHighlight();
        }
#endif
    }
}
