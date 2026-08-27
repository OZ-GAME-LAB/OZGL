using System;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    public sealed class ExitConfirmView : MonoBehaviour
    {
        [SerializeField] private Image dimmer; //팝업시 배경을 어둡게 처리하는 영역
        [SerializeField] private Image panel; //버튼을 담는 팝업 패널
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

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
            confirmButton.onClick.AddListener(OnConfirmClicked);
            cancelButton.onClick.AddListener(OnCancelClicked);
        }

        private void OnDestroy()
        {
            confirmButton.onClick.RemoveListener(OnConfirmClicked);
            cancelButton.onClick.RemoveListener(OnCancelClicked);
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

        #region Internal API
        
        /// <summary>
        /// UI 리소스를 적용합니다.
        /// 타이틀 ui뷰가 전체 타이틀 ui리소스를 적용하는 단계에서 호출되요
        /// </summary>
        /// <param name="resources"></param>
        internal void ApplyResources(TitleExitConfirmResources resources)
        {
            if (resources == null)
                return;

            TitleUiStyleApplier.Apply(dimmer, resources.dimmer);
            TitleUiStyleApplier.Apply(panel, resources.panel);

            TitleUiStyleApplier.Apply(confirmButton, resources.confirmButton);
            TitleUiStyleApplier.Apply(cancelButton, resources.cancelButton);
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
