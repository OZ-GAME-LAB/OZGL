using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OzGameLab01.Controllers;

namespace OzGameLab01.UI
{
    public class SkillDeckUI : MonoBehaviour
    {
        [SerializeField] private Slider timerGauge;
        [SerializeField] private RectTransform deckPileAnchor;
        [SerializeField] private RectTransform discardZoneAnchor;
        [SerializeField] private RectTransform centerAnchor;
        [SerializeField] private RectTransform drawnCard;
        [SerializeField] private Image drawnCardBackground;
        [SerializeField] private TextMeshProUGUI drawnCardLabel;
        [SerializeField] private float moveDuration = 0.5f;
        [SerializeField] private float glowDuration = 0.4f;
        [SerializeField] private Color glowColor = new Color(1f, 0.95f, 0.4f, 1f);

        private void Awake()
        {
            if (drawnCard != null)
            {
                drawnCard.gameObject.SetActive(false);
            }
        }

        public void SetTimerProgress(float ratio)
        {
            if (timerGauge != null)
            {
                timerGauge.value = ratio;
            }
        }

        public void ShowDrawnCard(Unit unit)
        {
            if (drawnCard == null)
            {
                return;
            }

            drawnCard.gameObject.SetActive(true);

            if (deckPileAnchor != null)
            {
                drawnCard.position = deckPileAnchor.position;
            }

            if (drawnCardLabel != null)
            {
                drawnCardLabel.text = GetLabel(unit);
            }
        }

        public IEnumerator MoveCardToCenter()
        {
            yield return MoveCardTo(centerAnchor);
        }

        public IEnumerator PlayGlow()
        {
            if (drawnCardBackground == null)
            {
                yield break;
            }

            Color baseColor = drawnCardBackground.color;
            float half = glowDuration / 2f;
            float t = 0f;

            while (t < half)
            {
                t += Time.deltaTime;
                drawnCardBackground.color = Color.Lerp(baseColor, glowColor, t / half);
                yield return null;
            }

            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                drawnCardBackground.color = Color.Lerp(glowColor, baseColor, t / half);
                yield return null;
            }

            drawnCardBackground.color = baseColor;
        }

        public IEnumerator MoveCardToDiscard()
        {
            yield return MoveCardTo(discardZoneAnchor);

            if (drawnCard != null)
            {
                drawnCard.gameObject.SetActive(false);
            }
        }

        private IEnumerator MoveCardTo(RectTransform destination)
        {
            if (drawnCard == null || destination == null)
            {
                yield break;
            }

            Vector3 startPos = drawnCard.position;
            Vector3 endPos = destination.position;
            float t = 0f;

            while (t < moveDuration)
            {
                t += Time.deltaTime;
                drawnCard.position = Vector3.Lerp(startPos, endPos, t / moveDuration);
                yield return null;
            }

            drawnCard.position = endPos;
        }

        public void ResetDiscardPile()
        {
        }

        private string GetLabel(Unit unit)
        {
            if (unit == null)
            {
                return "?";
            }

            switch (unit.Skill)
            {
                case Unit.SkillType.Warrior:
                    return "SWD";
                case Unit.SkillType.Archer:
                    return "BOW";
                case Unit.SkillType.Mage:
                    return "STF";
                default:
                    return "?";
            }
        }
    }
}
