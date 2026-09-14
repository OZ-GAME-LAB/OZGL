using UnityEngine;

namespace OzGameLab01.UI
{
    /// <summary>
    /// 자신이 켜질 때 다른 특정 UI를 임시로 끄고, 
    /// 자신이 꺼질 때 원래대로 다시 켜주는 유용한 도우미 컴포넌트입니다.
    /// </summary>
    public class UIVisibilityToggle : MonoBehaviour
    {
        [Header("가려둘 UI 설정")]
        [Tooltip("이 창이 열려있는 동안 잠시 숨겨둘 UI (예: ReadyUI 본체)")]
        public GameObject targetUIToHide;

        // 이 스크립트가 붙은 오브젝트(EventUI)가 활성화될 때 자동으로 실행
        private void OnEnable()
        {
            if (targetUIToHide != null)
            {
                targetUIToHide.SetActive(false);
            }
        }

        // 이 스크립트가 붙은 오브젝트(EventUI)가 닫힐(비활성화될) 때 자동으로 실행
        private void OnDisable()
        {
            if (targetUIToHide != null)
            {
                targetUIToHide.SetActive(true);
            }
        }
    }
}