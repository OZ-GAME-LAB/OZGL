using UnityEngine;
using UnityEngine.UI;

namespace Combat
{
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private Slider slider;

        public void Init(float maxHP)
        {
            slider.maxValue = maxHP;
            slider.value = maxHP;
        }

        public void SetHP(float currentHP)
        {
            slider.value = currentHP;
        }
    }
}
