using UnityEngine;

namespace OzGameLab01.Controllers
{
    public class BoardUIController : MonoBehaviour
    {
        [Header("Overlay Views")]
        [Tooltip("주사위 화면 (RollView)")]
        public GameObject rollView;

        [Tooltip("인벤토리 화면 (추후 구현 시 할당)")]
        public GameObject inventoryView;

        [Tooltip("설정 화면 (추후 구현 시 할당)")]
        public GameObject settingsView;

        /// <summary>
        /// 화면 구석에 있는 주사위 아이콘 등의 버튼을 눌렀을 때 호출됩니다.
        /// 창을 껐다 켰다(Toggle) 하는 역할을 합니다.
        /// </summary>
        public void ToggleRollView()
        {
            if (rollView == null) return;

            // 켜져 있으면 끄고, 꺼져 있으면 켭니다.
            bool isActive = !rollView.activeSelf;

            // 창을 켤 때는 다른 오버레이 창들을 전부 닫아주는 센스!
            if (isActive)
            {
                CloseAllViews();
            }

            rollView.SetActive(isActive);
        }

        /// <summary>
        /// 열려있는 모든 오버레이 창을 강제로 닫습니다.
        /// (예: ESC 키를 눌렀을 때 호출하거나, 새로운 창이 열릴 때 기존 창들을 정리할 때 사용)
        /// </summary>
        public void CloseAllViews()
        {
            if (rollView != null) rollView.SetActive(false);
            if (inventoryView != null) inventoryView.SetActive(false);
            if (settingsView != null) settingsView.SetActive(false);
        }
        [Header("Settings")]
        [Tooltip("주사위를 굴린 후 창이 자동으로 닫히기까지의 지연 시간(초)")]
        public float rollViewCloseDelay = 1.0f;

        private void Start()
        {
            // 주사위 굴림 이벤트를 구독합니다.
            if (Managers.DiceManager.Instance != null)
            {
                Managers.DiceManager.Instance.OnDiceRolled += HandleDiceRolled;
            }
        }

        private void OnDestroy()
        {
            // 메모리 누수를 방지하기 위해 이벤트를 구독 해제합니다.
            if (Managers.DiceManager.Instance != null)
            {
                Managers.DiceManager.Instance.OnDiceRolled -= HandleDiceRolled;
            }
        }

        /// <summary>
        /// 주사위를 굴렸을 때 DiceManager로부터 호출됩니다.
        /// </summary>
        private void HandleDiceRolled(int diceValue)
        {
            // 주사위 뷰가 켜져있다면 지정된 시간 후에 닫는 코루틴을 시작합니다.
            if (rollView != null && rollView.activeSelf)
            {
                StartCoroutine(CloseRollViewRoutine());
            }
        }

        private System.Collections.IEnumerator CloseRollViewRoutine()
        {
            // 인스펙터에서 설정한 시간만큼 대기합니다.
            yield return new WaitForSeconds(rollViewCloseDelay);
            
            // 대기가 끝나면 주사위 창을 닫습니다.
            if (rollView != null)
            {
                rollView.SetActive(false);
            }
        }
    }
}