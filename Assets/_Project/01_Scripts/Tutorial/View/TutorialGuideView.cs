using System;
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
        [Tooltip("비워 두면 ContentRoot 아래의 DialoguePanel을 찾아 Button을 자동으로 추가합니다.")]
        [SerializeField] private Button dialoguePanelButton;

        [Header("Show Animation")]
        [SerializeField, Min(0f)] private float showDuration = 0.3f;
        [SerializeField] private Vector2 showOffset = new(0f, -30f);
        [SerializeField, Range(0.1f, 1f)] private float showStartScale = 0.95f;

        [Header("Hide Animation")]
        [SerializeField, Min(0f)] private float hideDuration = 0.25f;

        private Vector2 defaultPosition;
        private Vector3 defaultScale;

        private Sequence currentSequence;
        private int originalSiblingIndex = -1;
        private bool isBroughtToFront;

        public event Action DialoguePanelClicked;


        #region Unity Lifecycle

        private void Awake()
        {
            ResolveDialoguePanelButton();
            CacheDefaultTransform();
            HideImmediate();
        }

        private void OnEnable()
        {
            ResolveDialoguePanelButton();

            if (dialoguePanelButton == null)
                return;

            dialoguePanelButton.onClick.RemoveListener(HandleDialoguePanelClicked);
            dialoguePanelButton.onClick.AddListener(HandleDialoguePanelClicked);
        }

        private void OnDisable()
        {
            if (dialoguePanelButton != null)
                dialoguePanelButton.onClick.RemoveListener(HandleDialoguePanelClicked);

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
            BringToFront();

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
            Hide(null);
        }


        /// <summary>
        /// Hide 연출이 끝난 뒤 콜백을 실행합니다.
        /// </summary>
        public void Hide(Action onComplete)
        {
            if (!gameObject.activeSelf)
            {
                onComplete?.Invoke();
                return;
            }

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
                RestoreSiblingOrder();
                gameObject.SetActive(false);
                onComplete?.Invoke();
            });
        }


        /// <summary>
        /// 연출 없이 즉시 표시합니다.
        /// </summary>
        public void ShowImmediate()
        {
            KillTween();

            gameObject.SetActive(true);
            BringToFront();

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
            RestoreSiblingOrder();

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


        private void ResolveDialoguePanelButton()
        {
            if (dialoguePanelButton != null)
                return;

            Transform dialoguePanel = FindChildRecursive(
                contentRoot != null ? contentRoot : transform,
                "DialoguePanel");

            if (dialoguePanel == null)
                return;

            dialoguePanelButton = dialoguePanel.GetComponent<Button>();

            if (dialoguePanelButton == null)
                dialoguePanelButton = dialoguePanel.gameObject.AddComponent<Button>();

            dialoguePanelButton.transition = Selectable.Transition.None;

            if (dialoguePanelButton.targetGraphic == null)
                dialoguePanelButton.targetGraphic = dialoguePanel.GetComponent<Graphic>();
        }


        private static Transform FindChildRecursive(Transform parent,string childName)
        {
            if (parent == null)
                return null;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);

                if (child.name == childName)
                    return child;

                Transform result = FindChildRecursive(child, childName);

                if (result != null)
                    return result;
            }

            return null;
        }


        private void HandleDialoguePanelClicked()
        {
            DialoguePanelClicked?.Invoke();
        }


        private void BringToFront()
        {
            if (isBroughtToFront || transform.parent == null)
                return;

            originalSiblingIndex = transform.GetSiblingIndex();
            transform.SetAsLastSibling();
            isBroughtToFront = true;
        }


        private void RestoreSiblingOrder()
        {
            if (!isBroughtToFront || transform.parent == null)
                return;

            int lastIndex = Mathf.Max(0,transform.parent.childCount - 1);
            transform.SetSiblingIndex(
                Mathf.Clamp(originalSiblingIndex,0,lastIndex));

            originalSiblingIndex = -1;
            isBroughtToFront = false;
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
