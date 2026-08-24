using System.Collections;
using UnityEngine;

namespace OzGameLab01.UI
{
    public class DiceRoller : MonoBehaviour
    {
        [SerializeField] private DiceFace diceFace;
        [SerializeField] private RectTransform diceTransform;
        [SerializeField] private float rollDuration = 0.6f;
        [SerializeField] private float cycleInterval = 0.05f;
        [SerializeField] private float wobbleAngle = 15f;

        public IEnumerator PlayRoll(int finalValue)
        {
            float elapsed = 0f;

            while (elapsed < rollDuration)
            {
                int fake = Random.Range(1, 7);
                if (diceFace != null)
                {
                    diceFace.SetFace(fake);
                }

                if (diceTransform != null)
                {
                    diceTransform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-wobbleAngle, wobbleAngle));
                }

                yield return new WaitForSeconds(cycleInterval);
                elapsed += cycleInterval;
            }

            if (diceFace != null)
            {
                diceFace.SetFace(finalValue);
            }

            if (diceTransform != null)
            {
                diceTransform.localRotation = Quaternion.identity;
            }
        }
    }
}
