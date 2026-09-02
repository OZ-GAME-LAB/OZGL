using TMPro;
using UnityEngine;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class FeedbackView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text messageText;

        #region Properties

        public bool IsVisible => gameObject.activeSelf;

        public string Message
        {
            get => messageText != null ? messageText.text : string.Empty;
            private set
            {
                if (messageText != null)
                {
                    messageText.text = value ?? string.Empty;
                }
            }
        }

        #endregion

        #region API

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Show(string message)
        {
            SetMessage(message);
            Show();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetMessage(string message)
        {
            Message = message;
        }

        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (messageText == null)
            {
                messageText = GetComponentInChildren<TMP_Text>(true);
            }
        }
#endif
    }
}