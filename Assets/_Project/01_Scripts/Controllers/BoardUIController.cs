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
    }
}