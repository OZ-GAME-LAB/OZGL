using OzGameLab01.UI;
using UnityEngine;

namespace OzGameLab01.Controllers
{
    /// <summary>
    /// 로스터 준비 화면(ReadyUI)의 메인 화면과 유닛 편성 화면 전환을 관리합니다.
    ///
    /// ReadyMainView의 "유닛" 버튼과 UnitView의 닫기 버튼은 클릭 이벤트만 발생시킬 뿐
    /// 화면 전환 로직을 갖고 있지 않아, 이 컨트롤러가 그 사이를 연결합니다.
    /// </summary>
    public sealed class ReadyUIController : MonoBehaviour
    {
        [SerializeField] private ReadySceneView readySceneView;

        private void Awake()
        {
            if (readySceneView == null)
            {
                readySceneView = GetComponent<ReadySceneView>();
            }

            if (readySceneView == null)
            {
                Debug.LogError("[ReadyUIController] ReadySceneView가 연결되지 않았습니다.", this);
            }
        }

        private void OnEnable()
        {
            if (readySceneView == null)
            {
                return;
            }

            if (readySceneView.MainView != null)
            {
                readySceneView.MainView.UnitClicked += HandleUnitButtonClicked;
            }

            if (readySceneView.UnitView != null)
            {
                readySceneView.UnitView.CloseClicked += HandleUnitViewClosed;
            }
        }

        private void OnDisable()
        {
            if (readySceneView == null)
            {
                return;
            }

            if (readySceneView.MainView != null)
            {
                readySceneView.MainView.UnitClicked -= HandleUnitButtonClicked;
            }

            if (readySceneView.UnitView != null)
            {
                readySceneView.UnitView.CloseClicked -= HandleUnitViewClosed;
            }
        }

        /// <summary>
        /// "유닛" 버튼을 누르면 메인 화면을 닫고 유닛 편성 화면을 엽니다.
        /// </summary>
        private void HandleUnitButtonClicked(ReadyMainView view)
        {
            readySceneView.HideMainView();
            readySceneView.ShowUnitView();
        }

        /// <summary>
        /// 유닛 편성 화면의 닫기 버튼을 누르면 메인 화면으로 되돌아갑니다.
        /// </summary>
        private void HandleUnitViewClosed(UnitView view)
        {
            readySceneView.HideUnitView();
            readySceneView.ShowMainView();
        }
    }
}
