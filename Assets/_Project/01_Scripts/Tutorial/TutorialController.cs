using System;
using OzGameLab01.Managers;
using OzGameLab01.UI;
using UnityEngine;

namespace OzGameLab01.Controllers
{
    [DisallowMultipleComponent]
    public sealed class TutorialController : MonoBehaviour
    {
        [Header("View Root")]
        [SerializeField] private RectTransform viewRoot;

        [Header("Scene Views (Optional)")]
        [SerializeField] private TutorialGuideView guideView;
        [SerializeField] private TutorialHintView hintView;

        [Header("Prefabs (Used When Scene View Is Empty)")]
        [SerializeField] private TutorialGuideView guidePrefab;
        [SerializeField] private TutorialHintView hintPrefab;

        private bool isGuideDismissing;
        private bool guideDismissEnabled = true;

        public TutorialGuideView GuideView => guideView;
        public TutorialHintView HintView => hintView;
        public bool IsGuideVisible =>
            guideView != null && guideView.gameObject.activeSelf;

        public event Action GuideShown;
        public event Action GuideDismissed;

        public void ConfigureRuntime(
            RectTransform runtimeViewRoot,
            TutorialGuideView runtimeGuidePrefab,
            TutorialHintView runtimeHintPrefab = null)
        {
            UnbindGuideView();

            viewRoot = runtimeViewRoot;
            guidePrefab = runtimeGuidePrefab;
            hintPrefab = runtimeHintPrefab;

            CreateViewsIfNeeded();

            if (isActiveAndEnabled)
                BindGuideView();
        }

        public void ConfigureSceneViews(
            RectTransform runtimeViewRoot,
            TutorialGuideView sceneGuideView,
            TutorialHintView sceneHintView = null)
        {
            UnbindGuideView();

            viewRoot = runtimeViewRoot;
            guideView = sceneGuideView;
            hintView = sceneHintView;

            if (isActiveAndEnabled)
                BindGuideView();
        }

        public void SetGuideDismissEnabled(bool enabled)
        {
            guideDismissEnabled = enabled;
        }

        private void Awake()
        {
            CreateViewsIfNeeded();
        }

        private void OnEnable()
        {
            BindGuideView();
        }

        private void OnDisable()
        {
            UnbindGuideView();
        }

        public void ShowGuide(
            Sprite characterSprite,
            string characterName,
            string dialogue,
            bool showCharacter = true)
        {
            if (guideView == null)
                return;

            isGuideDismissing = false;

            guideView.SetData(characterSprite,characterName,dialogue);
            guideView.SetCharacterVisible(showCharacter);
            guideView.Show();

            GuideShown?.Invoke();
        }

        public void DismissGuide()
        {
            if (guideView == null || isGuideDismissing)
                return;

            if (!guideView.gameObject.activeSelf)
                return;

            isGuideDismissing = true;
            SoundConnector.RequestSfx(SoundId.TutorialDialog);
            guideView.Hide(CompleteGuideDismissal);
        }

        public void DismissGuideImmediate()
        {
            if (guideView == null || !guideView.gameObject.activeSelf)
                return;

            isGuideDismissing = false;
            guideView.HideImmediate();
            GuideDismissed?.Invoke();
        }

        public void ShowHint(
            string message,
            RectTransform target,
            Vector2 anchoredOffset)
        {
            if (hintView == null)
                return;

            hintView.SetText(message);
            hintView.SetAnchor(target,anchoredOffset);
            hintView.Show();
        }

        public void HideHint()
        {
            if (hintView != null)
                hintView.Hide();
        }

        public void HideAllImmediate()
        {
            isGuideDismissing = false;

            // 씬에 비활성 상태로 배치된 View는 아직 Awake가 호출되지 않아
            // 기본 Transform 값이 캐시되지 않았을 수 있습니다. 이 상태에서
            // HideImmediate를 호출하면 ContentRoot의 scale이 Vector3.zero로
            // 덮일 수 있으므로 실제로 활성화된 View만 초기화합니다.
            if (guideView != null && guideView.gameObject.activeSelf)
                guideView.HideImmediate();

            if (hintView != null)
                hintView.HideImmediate();
        }

        private void CreateViewsIfNeeded()
        {
            Transform parent = viewRoot != null ? viewRoot : transform;

            if (guideView == null && guidePrefab != null)
                guideView = Instantiate(guidePrefab,parent);

            if (hintView == null && hintPrefab != null)
                hintView = Instantiate(hintPrefab,parent);
        }

        private void BindGuideView()
        {
            if (guideView == null)
                return;

            guideView.DialoguePanelClicked -= HandleDialoguePanelClicked;
            guideView.DialoguePanelClicked += HandleDialoguePanelClicked;
        }

        private void UnbindGuideView()
        {
            if (guideView != null)
                guideView.DialoguePanelClicked -= HandleDialoguePanelClicked;
        }

        private void HandleDialoguePanelClicked()
        {
            if (guideDismissEnabled)
                DismissGuide();
        }

        private void CompleteGuideDismissal()
        {
            isGuideDismissing = false;
            GuideDismissed?.Invoke();
        }
    }
}
