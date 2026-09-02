using System.Collections;
using UnityEngine;

namespace OzGameLab01.Combat
{
    public class AoeCircleVFX : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        public void Configure(float radius, Color color)
        {
            transform.localScale = Vector3.one * (radius * 2f);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = color;
            }
        }

        public void FadeOutAndDestroy(float duration)
        {
            StartCoroutine(FadeRoutine(duration));
        }

        private IEnumerator FadeRoutine(float duration)
        {
            if (spriteRenderer == null)
            {
                Destroy(gameObject);
                yield break;
            }

            float startAlpha = spriteRenderer.color.a;
            float t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                Color c = spriteRenderer.color;
                c.a = Mathf.Lerp(startAlpha, 0f, t / duration);
                spriteRenderer.color = c;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
