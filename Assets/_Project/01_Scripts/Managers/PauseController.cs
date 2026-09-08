using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OzGameLab01.Controllers;

namespace OzGameLab01.Managers
{
    public class PauseController : MonoBehaviour
    {
        [SerializeField] private Button pauseButton;
        [SerializeField] private TextMeshProUGUI pauseButtonLabel;
        [SerializeField] private CombatSceneController combatSceneController;

        private bool _isPaused;

        private void Awake()
        {
            if (combatSceneController == null)
            {
                combatSceneController = FindFirstObjectByType<CombatSceneController>(FindObjectsInactive.Include);
            }

            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(OnPauseClicked);
            }
        }

        private void OnPauseClicked()
        {
            _isPaused = !_isPaused;
            if (combatSceneController != null)
            {
                combatSceneController.SetPaused(_isPaused);
            }
            else
            {
                Time.timeScale = _isPaused ? 0f : 1f;
            }

            if (pauseButtonLabel != null)
            {
                pauseButtonLabel.text = _isPaused ? "Continue" : "Paused";
            }
        }

        private void OnDestroy()
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveListener(OnPauseClicked);
            }
        }
    }
}
