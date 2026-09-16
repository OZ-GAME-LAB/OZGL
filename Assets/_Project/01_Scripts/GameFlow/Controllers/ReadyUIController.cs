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
        [UnityEngine.Serialization.FormerlySerializedAs("readySceneView")]
        [SerializeField] private ReadySceneView _readySceneView;
        private ReadyMainView _subscribedMain;
        private UnitView _subscribedUnit;

        private void Awake()
        {
            if (_readySceneView == null)
            {
                _readySceneView = GetComponent<ReadySceneView>();
            }

            if (_readySceneView == null)
            {
                Debug.LogError("[ReadyUIController] ReadySceneView가 연결되지 않았습니다.", this);
            }
        }

        private void OnEnable()
        {
            if (_readySceneView == null)
            {
                return;
            }

            _subscribedMain = _readySceneView.MainView;
            _subscribedUnit = _readySceneView.UnitView;
            if (_subscribedMain != null)
            {
                _subscribedMain.UnitClicked += HandleUnitButtonClicked;
            }

            if (_subscribedUnit != null)
            {
                _subscribedUnit.CloseClicked += HandleUnitViewClosed;
            }
        }

        private void OnDisable()
        {
            if (_subscribedMain != null)
            {
                _subscribedMain.UnitClicked -= HandleUnitButtonClicked;
            }

            if (_subscribedUnit != null)
            {
                _subscribedUnit.CloseClicked -= HandleUnitViewClosed;
            }
            _subscribedMain = null;
            _subscribedUnit = null;
        }

        /// <summary>
        /// "유닛" 버튼을 누르면 메인 화면을 닫고 유닛 편성 화면을 엽니다.
        /// </summary>
        private void HandleUnitButtonClicked(ReadyMainView view)
        {
            _readySceneView.HideMainView();
            _readySceneView.ShowUnitView();
        }

        /// <summary>
        /// 유닛 편성 화면의 닫기 버튼을 누르면 메인 화면으로 되돌아갑니다.
        /// </summary>
        private void HandleUnitViewClosed(UnitView view)
        {
            _readySceneView.HideUnitView();
            _readySceneView.ShowMainView();
        }
    }
}
