using OzGameLab01.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    /// <summary>
    /// 버튼의 클릭과 호버에 지정한 SoundId의 효과음을 재생합니다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonSound : MonoBehaviour, IPointerEnterHandler
    {
        [Header("Sound")]
        [SerializeField] private SoundId clickSound = SoundId.UiButtonClick;
        [SerializeField] private SoundId hoverSound = SoundId.UiButtonHover;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClicked);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClicked);
        }

        /// <summary>
        /// 마우스 진입 시 선택 가능한 버튼의 호버 효과음
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_button.IsActive() && _button.IsInteractable())
            {
                Play(hoverSound);
            }
        }

        /// <summary>
        /// 버튼 클릭 시 효과음 요청
        /// </summary>
        private void OnClicked()
        {
            Play(clickSound);
        }

        /// <summary>
        /// 전역 커넥터로의 효과음 요청 이벤트 발행
        /// </summary>
        private void Play(SoundId id)
        {
            if (id == SoundId.None)
            {
                return;
            }

            SoundConnector.RequestSfx(id);
        }
    }
}
