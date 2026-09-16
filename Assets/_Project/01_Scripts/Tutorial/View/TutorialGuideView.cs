using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class TutorialGuideView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform contentRoot;

        [SerializeField] private Image characterImage;
        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private TMP_Text dialogueText;

        [Header("Show Animation")]
        [SerializeField, Min(0f)] private float showDuration = 0.3f;
        [SerializeField] private Vector2 showOffset = new(0f, -30f);
        [SerializeField, Range(0.1f, 1f)] private float showStartScale = 0.95f;

        [Header("Hide Animation")]
        [SerializeField, Min(0f)] private float hideDuration = 0.25f;

        private Vector2 defaultPosition;
        private Vector3 defaultScale;

        private Sequence currentSequence;


        #region Unity Lifecycle

        private void Awake()
        {
            CacheDefaultTransform();
            HideImmediate();
        }

        private void OnDisable()
        {
            KillTween();
        }

        #endregion


        #region Public API

        /// <summary>
        /// 캐릭터 이미지, 이름, 대사를 한 번에 적용합니다.
        /// </summary>
        public void SetData(Sprite characterSprite,string characterName,string dialogue)
        {
            SetCharacterSprite(characterSprite);
            SetCharacterName(characterName);
            SetDialogue(dialogue);
        }


        /// <summary>
        /// 안내 캐릭터 이미지를 교체합니다.
        /// </summary>
        public void SetCharacterSprite(Sprite sprite)
        {
            if (characterImage == null)
                return;

            characterImage.sprite = sprite;
            characterImage.enabled = sprite != null;
        }


        /// <summary>
        /// 안내 캐릭터 이름을 변경합니다.
        /// </summary>
        public void SetCharacterName(string characterName)
        {
            if (characterNameText == null)
                return;

            characterNameText.text = characterName ?? string.Empty;
        }


        /// <summary>
        /// 현재 튜토리얼 대사를 변경합니다.
        /// </summary>
        public void SetDialogue(string dialogue)
        {
            if (dialogueText == null)
                return;

            dialogueText.text = dialogue ?? string.Empty;
        }


        /// <summary>
        /// 현재 캐릭터 이미지를 표시하거나 숨깁니다.
        /// </summary>
        public void SetCharacterVisible(bool visible)
        {
            if (characterImage == null)
                return;

            characterImage.gameObject.SetActive(visible);
        }


        /// <summary>
        /// Show 연출을 재생합니다.
        /// </summary>
        public void Show()
        {
            KillTween();

            gameObject.SetActive(true);

            PrepareShowState();

            currentSequence = DOTween.Sequence()
                .SetUpdate(true);

            currentSequence.Join(
                canvasGroup
                    .DOFade(1f, showDuration)
                    .SetEase(Ease.OutQuad));

            currentSequence.Join(
                contentRoot
                    .DOAnchorPos(defaultPosition, showDuration)
                    .SetEase(Ease.OutCubic));

            currentSequence.Join(
                contentRoot
                    .DOScale(defaultScale, showDuration)
                    .SetEase(Ease.OutBack));

            currentSequence.OnComplete(() =>
            {
                currentSequence = null;
            });
        }


        /// <summary>
        /// Hide 연출을 재생한 후 비활성화합니다.
        /// </summary>
        public void Hide()
        {
            if (!gameObject.activeSelf)
                return;

            KillTween();

            currentSequence = DOTween.Sequence()
                .SetUpdate(true);

            currentSequence.Join(
                canvasGroup
                    .DOFade(0f, hideDuration)
                    .SetEase(Ease.InQuad));

            currentSequence.Join(
                contentRoot
                    .DOAnchorPos(
                        defaultPosition + showOffset,
                        hideDuration)
                    .SetEase(Ease.InCubic));

            currentSequence.Join(
                contentRoot
                    .DOScale(
                        defaultScale * showStartScale,
                        hideDuration)
                    .SetEase(Ease.InQuad));

            currentSequence.OnComplete(() =>
            {
                currentSequence = null;

                ResetContentTransform();
                gameObject.SetActive(false);
            });
        }


        /// <summary>
        /// 연출 없이 즉시 표시합니다.
        /// </summary>
        public void ShowImmediate()
        {
            KillTween();

            gameObject.SetActive(true);

            canvasGroup.alpha = 1f;

            ResetContentTransform();
        }


        /// <summary>
        /// 연출 없이 즉시 숨깁니다.
        /// </summary>
        public void HideImmediate()
        {
            KillTween();

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            ResetContentTransform();

            gameObject.SetActive(false);
        }


        /// <summary>
        /// 현재 재생 중인 View 연출을 중단합니다.
        /// </summary>
        public void StopAnimation()
        {
            KillTween();
        }

        #endregion


        #region Internal

        private void CacheDefaultTransform()
        {
            if (contentRoot == null)
                return;

            defaultPosition = contentRoot.anchoredPosition;
            defaultScale = contentRoot.localScale;
        }


        private void PrepareShowState()
        {
            canvasGroup.alpha = 0f;

            contentRoot.anchoredPosition = defaultPosition + showOffset;
            contentRoot.localScale = defaultScale * showStartScale;
        }


        private void ResetContentTransform()
        {
            if (contentRoot == null)
                return;

            contentRoot.anchoredPosition = defaultPosition;
            contentRoot.localScale = defaultScale;
        }


        private void KillTween()
        {
            if (currentSequence == null)
                return;

            currentSequence.Kill();
            currentSequence = null;
        }

        #endregion
    }
}