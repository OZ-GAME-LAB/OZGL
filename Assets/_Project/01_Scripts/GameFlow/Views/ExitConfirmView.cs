using System;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI.Title
{
    public sealed class ExitConfirmView : MonoBehaviour
    {
        [UnityEngine.Serialization.FormerlySerializedAs("dimmer")]
        [SerializeField] private Image _dimmer; //팝업시 배경을 어둡게 처리하는 영역
        [UnityEngine.Serialization.FormerlySerializedAs("panel")]
        [SerializeField] private Image _panel; //버튼을 담는 팝업 패널
        [UnityEngine.Serialization.FormerlySerializedAs("confirmButton")]
        [SerializeField] private Button _confirmButton;
        [UnityEngine.Serialization.FormerlySerializedAs("cancelButton")]
        [SerializeField] private Button _cancelButton;

        #region Properties

        public bool IsVisible => gameObject.activeSelf;

        #endregion

        #region Events

        public event Action ConfirmRequested; //종료 확인 버튼을 누르면 발생하는 이벤트
        public event Action CancelRequested; //종료 취소 버튼을 누르면 발생하는 이벤트

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _confirmButton.onClick.AddListener(OnConfirmClicked);
            _cancelButton.onClick.AddListener(OnCancelClicked);
        }

        private void OnDestroy()
        {
            _confirmButton.onClick.RemoveListener(OnConfirmClicked);
            _cancelButton.onClick.RemoveListener(OnCancelClicked);
        }

        #endregion

        #region Public API

        public void Show() //팝업 오픈
        {
            gameObject.SetActive(true);
        }

        public void Hide() //팝업 닫기
        {
            gameObject.SetActive(false);
        }

        #endregion

        #region Private Methods

        private void OnConfirmClicked()
        {
            ConfirmRequested?.Invoke();
        }

        private void OnCancelClicked()
        {
            CancelRequested?.Invoke();
        }

        #endregion
    }
}
