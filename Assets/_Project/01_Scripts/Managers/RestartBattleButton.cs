using UnityEngine;
using UnityEngine.UI;
using OzGameLab01.Controllers;

namespace OzGameLab01.Combat
{
    public class RestartBattleButton : MonoBehaviour
    {
        [SerializeField] private CombatSceneController combatSceneController;

        private void Awake()
        {
            if (combatSceneController == null)
            {
                combatSceneController = FindFirstObjectByType<CombatSceneController>(FindObjectsInactive.Include);
            }

            Button button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(Restart);
            }
        }

        private void Restart()
        {
            if (combatSceneController == null)
            {
                Debug.LogError("[RestartBattleButton] CombatSceneController가 연결되지 않아 전투를 재시작할 수 없습니다.", this);
                return;
            }

            combatSceneController.RestartBattle();
        }
    }
}
