using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.Managers
{
    public class PauseController : MonoBehaviour
    {
        [SerializeField] private Button pauseButton;
        [SerializeField] private TextMeshProUGUI pauseButtonLabel;

        private bool _isPaused;

        private void Awake()
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(OnPauseClicked);
            }
        }

        private void OnPauseClicked()
        {
            _isPaused = !_isPaused;
            Time.timeScale = _isPaused ? 0f : 1f;

            if (pauseButtonLabel != null)
            {
                pauseButtonLabel.text = _isPaused ? "재개" : "일시정지";
            }
        }
    }
}
