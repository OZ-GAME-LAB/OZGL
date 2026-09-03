using TMPro;
using UnityEngine;

namespace OzGameLab01.UI.Battle
{
    [DisallowMultipleComponent]
    public sealed class BattleTimerView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text timerText;

        #region Properties

        public TMP_Text TimerText => timerText;

        public bool IsVisible => gameObject.activeSelf;

        #endregion

        #region API

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetTimeText(string value)
        {
            if (timerText != null)
            {
                timerText.text = value ?? string.Empty;
            }
        }

        #endregion
    }
}