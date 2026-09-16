using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.Controllers
{
    public class PauseController : MonoBehaviour
    {
        [UnityEngine.Serialization.FormerlySerializedAs("pauseButton")]
        [SerializeField] private Button _pauseButton;
        [UnityEngine.Serialization.FormerlySerializedAs("pauseButtonLabel")]
        [SerializeField] private TextMeshProUGUI _pauseButtonLabel;
        [UnityEngine.Serialization.FormerlySerializedAs("combatSceneController")]
        [SerializeField] private CombatSceneController _combatSceneController;

        private bool _isPaused;
        private OzGameLab01.GameFlow.Views.PauseButtonView _view;

        private void Awake()
        {
            _view = new OzGameLab01.GameFlow.Views.PauseButtonView(_pauseButtonLabel);
            if (_combatSceneController == null)
            {
                _combatSceneController = FindFirstObjectByType<CombatSceneController>(FindObjectsInactive.Include);
            }

            if (_pauseButton != null)
            {
                _pauseButton.onClick.AddListener(OnPauseClicked);
            }
        }

        private void OnPauseClicked()
        {
            _isPaused = !_isPaused;
            if (_combatSceneController != null)
            {
                _combatSceneController.SetPaused(_isPaused);
            }
            else
            {
                Debug.LogError("[PauseController] CombatSceneController가 연결되지 않아 일시정지를 적용할 수 없습니다.", this);
            }

            _view.Render(_isPaused);
        }

        private void OnDestroy()
        {
            if (_pauseButton != null)
            {
                _pauseButton.onClick.RemoveListener(OnPauseClicked);
            }
        }
    }
}
