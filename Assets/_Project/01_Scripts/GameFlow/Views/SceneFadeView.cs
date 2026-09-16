using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace OzGameLab01.GameFlow.Views
{
    /// <summary>일시정지와 독립된 화면 페이드 표시</summary>
    public sealed class SceneFadeView
    {
        private readonly Image _image;
        private readonly float _duration;
        public SceneFadeView(Image image, float duration) { _image = image; _duration = duration; }
        public IEnumerator Fade(float fromAlpha, float toAlpha)
        {
            if (_image == null)
            {
                yield break;
            }

            float elapsed = 0f;
            Color color = _image.color;

            while (elapsed < _duration)
            {
                // elapsed += Time.deltaTime;
                // 일시정지 상태에서도 페이드가 진행되도록 실제 시간 사용
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / _duration);
                _image.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            _image.color = new Color(color.r, color.g, color.b, toAlpha);
        }

    }
}
