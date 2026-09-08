using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OzGameLab01.Controllers;
using OzGameLab01.Managers;
using OzGameLab01.UI;

namespace OzGameLab01.Combat
{
    public class BattleResultChecker : MonoBehaviour
    {
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private Button continueButton;
        [SerializeField] private BattleRewardPanel battleRewardPanel;

        [Header("전투 상태")]
        [SerializeField] private CombatSceneController combatSceneController;

        private void Awake()
        {
            if (resultPanel != null)
            {
                resultPanel.SetActive(false);
            }

            if (combatSceneController == null)
            {
                combatSceneController = FindFirstObjectByType<CombatSceneController>(FindObjectsInactive.Include);
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
            }
        }

        private void OnEnable()
        {
            if (combatSceneController != null)
            {
                combatSceneController.OnBattleResolved += HandleBattleResolved;
            }
        }

        private void OnDisable()
        {
            if (combatSceneController != null)
            {
                combatSceneController.OnBattleResolved -= HandleBattleResolved;
            }
        }

        private void OnDestroy()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinueClicked);
            }
        }

        private void HandleBattleResolved(bool victory)
        {
            if (victory && battleRewardPanel != null && CombatManager.Instance != null)
            {
                // BattleRewardPanel이 자체 배경/제목을 갖춘 독립 팝업이라 ResultPanel은 승리 시 띄우지 않음
                battleRewardPanel.Show(CombatManager.Instance.GetParticipatingAllyUnits(), OnContinueClicked);
                return;
            }

            if (resultText != null)
            {
                resultText.text = victory ? "victory!" : "defeat...";
            }

            if (resultPanel != null)
            {
                resultPanel.SetActive(true);
            }
        }

        private void OnContinueClicked()
        {
            if (combatSceneController == null)
            {
                Debug.LogError("[BattleResultChecker] CombatSceneController가 연결되지 않아 보드로 복귀할 수 없습니다.", this);
                return;
            }

            combatSceneController.ReturnToBoard();
        }
    }
}
