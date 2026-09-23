using System;
using System.Collections.Generic;
using DG.Tweening;
using OzGameLab01.Map;
using OzGameLab01.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OzGameLab01.Controllers
{
    [DisallowMultipleComponent]
    public sealed class TutorialSequenceController : MonoBehaviour
    {
        [Header("Controllers")]
        [SerializeField] private TutorialController tutorialController;
        [SerializeField] private ReadySceneView readySceneView;
        [SerializeField] private BoardUIController boardUIController;
        [SerializeField] private TutorialTargetRegistry targetRegistry;
        [SerializeField] private UnitFormationController unitFormationController;

        [Header("Board Tile Focus (Optional)")]
        [SerializeField] private BoardCameraController boardCameraController;
        [SerializeField] private BoardMoveRangeController boardMoveRangeController;
        [SerializeField] private MapGenerator mapGenerator;

        [Header("Sequence Order")]
        [SerializeField] private bool playOnStart = true;
        [Tooltip("각 Step SO를 실행할 순서대로 등록합니다.")]
        [SerializeField] private List<TutorialSequenceStepData> steps = new();

        private TutorialSequenceState<TutorialSequenceStepData> sequenceState;
        private Button waitingTriggerButton;
        private string waitingTriggerButtonKey;

        private Tween pendingShowTween;
        private TutorialHighlightPresenter highlightPresenter;
        private TutorialSequenceStepData guideAfterLocateStep;
        private bool tutorialLocateInProgress;
        private bool tutorialTileFocusVisible;
        private GameObject locateInputBlocker;

        public bool IsPlaying => sequenceState != null && sequenceState.IsPlaying;
        public int NextStepIndex => sequenceState != null
            ? sequenceState.NextStepIndex
            : 0;
        public string ActiveStepName => sequenceState?.ActiveStep != null
            ? sequenceState.ActiveStep.StepName
            : string.Empty;

        public event Action SequenceCompleted;

        private void Awake()
        {
            EnsureSequenceState();
            EnsureHighlightPresenter();
            ResolveBoardFocusReferences();
            ResolveUnitFormationController();
            CreateLocateInputBlocker();
        }

        private void OnEnable()
        {
            if (tutorialController != null)
                tutorialController.GuideDismissed += HandleGuideDismissed;

            if (readySceneView != null)
            {
                readySceneView.ViewVisibilityChanged += HandleReadyViewVisibilityChanged;

                if (readySceneView.UnitView != null)
                {
                    readySceneView.UnitView.Hidden += HandleUnitViewHidden;
                    readySceneView.UnitView.UnitPointerEntered +=
                        HandleOwnedUnitPointerEntered;
                }
            }

            ResolveBoardFocusReferences();
            ResolveUnitFormationController();
            EnsureHighlightPresenter();
            highlightPresenter.Configure(targetRegistry, readySceneView);
            BindLocateEvents();
        }

        private void Start()
        {
            if (boardUIController == null)
                boardUIController = FindFirstObjectByType<BoardUIController>();

            ResolveBoardFocusReferences();
            ResolveUnitFormationController();

            if (playOnStart)
                PlaySequence();
        }

        private void OnDisable()
        {
            if (tutorialController != null)
                tutorialController.GuideDismissed -= HandleGuideDismissed;

            if (readySceneView != null)
            {
                readySceneView.ViewVisibilityChanged -= HandleReadyViewVisibilityChanged;

                if (readySceneView.UnitView != null)
                {
                    readySceneView.UnitView.Hidden -= HandleUnitViewHidden;
                    readySceneView.UnitView.UnitPointerEntered -=
                        HandleOwnedUnitPointerEntered;
                }
            }

            UnbindLocateEvents();
            CancelTutorialLocatePresentation(false);

            UnbindWaitingButton();
            CancelPendingStep();
            highlightPresenter?.StopAll();
        }

        [ContextMenu("Play Tutorial Sequence")]
        public void PlaySequence()
        {
            EnsureSequenceState();
            StopSequence();

            if (tutorialController == null ||
                !sequenceState.HasConfiguredSteps)
                return;

            sequenceState.Start();

            PrepareNextStepTrigger();
            TryShowNextStep(TutorialStepTrigger.SequenceStarted);
        }

        [ContextMenu("Stop Tutorial Sequence")]
        public void StopSequence()
        {
            sequenceState?.Stop();

            UnbindWaitingButton();
            CancelPendingStep();
            highlightPresenter?.StopAll();
            CancelTutorialLocatePresentation(true);

            if (tutorialController != null && tutorialController.IsGuideVisible)
                tutorialController.HideAllImmediate();
        }

        public void NotifyManualTrigger(string triggerKey)
        {
            TutorialSequenceStepData step = GetNextStep();

            if (step == null || step.Trigger != TutorialStepTrigger.Manual)
                return;

            if (!string.Equals(
                    step.ManualTriggerKey,
                    triggerKey,
                    StringComparison.Ordinal))
                return;

            ScheduleNextStep();
        }

        public void NotifyButtonClicked(string buttonKey)
        {
            TutorialSequenceStepData step = GetNextStep();

            if (step == null || step.Trigger != TutorialStepTrigger.ButtonClicked)
                return;

            if (!string.Equals(
                    step.TriggerButtonKey,
                    buttonKey,
                    StringComparison.Ordinal))
                return;

            ScheduleNextStep();
        }

        public void NotifyReadyViewShown(ReadySceneViewType viewType)
        {
            HandleReadyViewVisibilityChanged(viewType,true);
        }

        public void NotifyReadyViewHidden(ReadySceneViewType viewType)
        {
            HandleReadyViewVisibilityChanged(viewType,false);
        }

        private void HandleGuideDismissed()
        {
            if (!IsPlaying || sequenceState.ActiveStep == null)
                return;

            TutorialSequenceStepData dismissedStep =
                sequenceState.ClearActive();
            highlightPresenter?.HandleGuideDismissed(dismissedStep);

            if (!sequenceState.HasRemainingSteps)
            {
                CompleteSequence();
                return;
            }

            PrepareNextStepTrigger();
            TryShowNextStep(TutorialStepTrigger.PreviousGuideDismissed);
        }

        private void HandleReadyViewVisibilityChanged(
            ReadySceneViewType viewType,
            bool isVisible)
        {
            if (!isVisible)
                highlightPresenter?.HandleReadyViewHidden(viewType);

            TryShowNextReadyViewStep(viewType,isVisible);
        }

        private void HandleUnitViewHidden(UnitView view)
        {
            HandleReadyViewVisibilityChanged(
                ReadySceneViewType.Unit,
                false);
        }

        private void HandleOwnedUnitPointerEntered(
            UnitItemView item,
            PointerEventData _)
        {
            TutorialSequenceStepData step = GetNextStep();
            if (step == null ||
                step.Trigger != TutorialStepTrigger.OwnedUnitHovered)
            {
                return;
            }

            if (step.TargetOwnedUnitId > 0)
            {
                ResolveUnitFormationController();
                if (unitFormationController == null ||
                    unitFormationController.GetUnitData(item)?.id !=
                    step.TargetOwnedUnitId)
                {
                    return;
                }
            }

            ScheduleNextStep();
        }

        private void TryShowNextReadyViewStep(
            ReadySceneViewType viewType,
            bool isVisible)
        {
            TutorialSequenceStepData step = GetNextStep();

            if (step == null)
                return;

            TutorialStepTrigger requiredTrigger = isVisible
                ? TutorialStepTrigger.ReadyViewShown
                : TutorialStepTrigger.ReadyViewHidden;

            if (step.Trigger != requiredTrigger ||
                step.TargetReadyView != viewType)
                return;

            ScheduleNextStep();
        }

        private void TryShowNextStep(TutorialStepTrigger trigger)
        {
            TutorialSequenceStepData step = GetNextStep();

            if (step == null || step.Trigger != trigger)
                return;

            ScheduleNextStep();
        }

        private TutorialSequenceStepData GetNextStep()
        {
            return sequenceState?.PeekNext();
        }

        private void ScheduleNextStep()
        {
            TutorialSequenceStepData step = GetNextStep();

            if (step == null)
                return;

            UnbindWaitingButton();
            if (!sequenceState.TryQueue(step))
                return;

            if (step.ShowDelay <= 0f)
            {
                ShowPendingStep();
                return;
            }

            pendingShowTween = DOVirtual.DelayedCall(
                step.ShowDelay,
                ShowPendingStep,
                step.IgnoreShowDelayTimeScale);
        }

        private void ShowPendingStep()
        {
            if (!IsPlaying || sequenceState.PendingStep == null)
                return;

            pendingShowTween = null;

            TutorialSequenceStepData step = sequenceState.ActivatePending();
            if (step == null)
                return;

            bool waitForLocateBeforeGuide =
                step.FocusBoardTile &&
                step.ShowGuide &&
                step.ShowGuideAfterLocate;

            if (!step.ShowGuide)
            {
                sequenceState.ClearActive();

                if (sequenceState.HasRemainingSteps)
                    PrepareNextStepTrigger();
            }

            bool locateStarted = step.FocusBoardTile &&
                                 TryStartTutorialTileLocate(
                                     step,
                                     waitForLocateBeforeGuide);
            bool showGuideNow = step.ShowGuide &&
                                (!waitForLocateBeforeGuide || !locateStarted);

            if (step.OpenRollView && showGuideNow && boardUIController != null)
            {
                boardUIController.RequestRollViewOpen(
                    () => ShowGuideForStep(step));
            }
            else
            {
                if (step.OpenRollView &&
                    (!step.ShowGuide || showGuideNow))
                    boardUIController?.RequestRollViewOpen();

                if (showGuideNow)
                    ShowGuideForStep(step);
            }

            if (!step.ShowGuide)
                highlightPresenter?.ShowActionStep(step);

            if (!step.ShowGuide &&
                !sequenceState.HasRemainingSteps &&
                !locateStarted)
                CompleteSequence();
        }

        private bool TryStartTutorialTileLocate(
            TutorialSequenceStepData step,
            bool showGuideOnComplete)
        {
            ResolveBoardFocusReferences();

            if (!TryResolveTileTarget(step, out MapNode node, out Transform target))
            {
                Debug.LogWarning(
                    $"[TutorialSequenceController] 타일 포커스 대상을 찾지 못했습니다. Step: {step.StepName}",
                    this);
                return false;
            }

            if (boardCameraController == null || boardCameraController.IsLocating)
            {
                Debug.LogWarning(
                    $"[TutorialSequenceController] Locate 카메라를 사용할 수 없습니다. Step: {step.StepName}",
                    this);
                return false;
            }

            bool focusVisible = boardMoveRangeController != null &&
                                boardMoveRangeController.ShowTutorialTileFocus(node);
            if (!focusVisible)
            {
                Debug.LogWarning(
                    $"[TutorialSequenceController] 타일 암전 효과를 표시하지 못했습니다. Step: {step.StepName}",
                    this);
            }

            SetLocateInputBlocked(true);
            boardCameraController.Locate(target);
            if (!boardCameraController.IsLocating)
            {
                SetLocateInputBlocked(false);

                if (focusVisible)
                {
                    boardMoveRangeController.ClearTutorialTileFocus(false);
                }

                return false;
            }

            tutorialLocateInProgress = true;
            tutorialTileFocusVisible = focusVisible;
            guideAfterLocateStep = showGuideOnComplete ? step : null;
            return true;
        }

        private bool TryResolveTileTarget(
            TutorialSequenceStepData step,
            out MapNode node,
            out Transform target)
        {
            node = null;
            target = null;

            if (mapGenerator == null || mapGenerator.NodeDict.Count == 0)
            {
                return false;
            }

            if (step.TileTargetMode == TutorialTileTargetMode.Position)
            {
                mapGenerator.NodeDict.TryGetValue(step.TilePosition, out node);
            }
            else
            {
                List<MapNode> matches = new List<MapNode>();
                foreach (MapNode candidate in mapGenerator.NodeDict.Values)
                {
                    if (candidate != null && candidate.Type == step.TileType)
                    {
                        matches.Add(candidate);
                    }
                }

                matches.Sort(CompareNodePosition);
                if (step.TileTypeOccurrence < matches.Count)
                {
                    node = matches[step.TileTypeOccurrence];
                }
            }

            GameObject nodeView = node != null
                ? mapGenerator.GetNodeView(node)
                : null;
            target = nodeView != null ? nodeView.transform : null;
            return node != null && target != null;
        }

        private static int CompareNodePosition(MapNode left, MapNode right)
        {
            int xComparison = left.Position.x.CompareTo(right.Position.x);
            return xComparison != 0
                ? xComparison
                : left.Position.y.CompareTo(right.Position.y);
        }

        private void HandleLocateCompleted()
        {
            TutorialSequenceStepData stepToShow = guideAfterLocateStep;
            bool wasTutorialLocate = tutorialLocateInProgress;

            tutorialLocateInProgress = false;
            guideAfterLocateStep = null;
            SetLocateInputBlocked(false);

            if (tutorialTileFocusVisible)
            {
                tutorialTileFocusVisible = false;
                bool restoreMoveRange = IsLocateCameraActive();
                boardMoveRangeController?.ClearTutorialTileFocus(restoreMoveRange);
            }

            // BoardCameraController는 비활성화될 때 진행 중인 Locate도 완료 이벤트로
            // 정리합니다. 씬 종료 과정에서는 이를 다음 Step 진행으로 취급하지 않습니다.
            if (!IsPlaying || !IsLocateCameraActive())
            {
                return;
            }

            if (wasTutorialLocate && stepToShow != null)
            {
                if (stepToShow.OpenRollView && boardUIController != null)
                {
                    boardUIController.RequestRollViewOpen(
                        () => ShowGuideForStep(stepToShow));
                }
                else
                {
                    ShowGuideForStep(stepToShow);
                }
            }

            if (wasTutorialLocate &&
                stepToShow == null &&
                sequenceState.ActiveStep == null &&
                !sequenceState.HasRemainingSteps)
            {
                CompleteSequence();
                return;
            }

            TryShowNextStep(TutorialStepTrigger.LocateCompleted);
        }

        private bool IsLocateCameraActive()
        {
            return boardCameraController != null &&
                   boardCameraController.isActiveAndEnabled;
        }

        private void ResolveBoardFocusReferences()
        {
            if (boardCameraController == null)
                boardCameraController = FindFirstObjectByType<BoardCameraController>();

            if (boardMoveRangeController == null)
                boardMoveRangeController = BoardMoveRangeController.Instance != null
                    ? BoardMoveRangeController.Instance
                    : FindFirstObjectByType<BoardMoveRangeController>();

            if (mapGenerator == null)
                mapGenerator = FindFirstObjectByType<MapGenerator>();
        }

        private void ResolveUnitFormationController()
        {
            if (unitFormationController == null)
            {
                unitFormationController =
                    FindFirstObjectByType<UnitFormationController>(
                        FindObjectsInactive.Include);
            }
        }

        private void BindLocateEvents()
        {
            if (boardCameraController == null)
                return;

            boardCameraController.LocateCompleted -= HandleLocateCompleted;
            boardCameraController.LocateCompleted += HandleLocateCompleted;
        }

        private void UnbindLocateEvents()
        {
            if (boardCameraController != null)
                boardCameraController.LocateCompleted -= HandleLocateCompleted;
        }

        private void CancelTutorialLocatePresentation(bool restoreMoveRange)
        {
            tutorialLocateInProgress = false;
            guideAfterLocateStep = null;
            SetLocateInputBlocked(false);

            if (!tutorialTileFocusVisible)
                return;

            tutorialTileFocusVisible = false;
            boardMoveRangeController?.ClearTutorialTileFocus(restoreMoveRange);
        }

        private void CreateLocateInputBlocker()
        {
            if (locateInputBlocker != null)
                return;

            locateInputBlocker = new GameObject(
                "TutorialLocateInputBlocker",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster));

            Canvas blockerCanvas = locateInputBlocker.GetComponent<Canvas>();
            blockerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            blockerCanvas.overrideSorting = true;
            blockerCanvas.sortingOrder = short.MaxValue;

            GameObject raycastBlocker = new GameObject(
                "RaycastBlocker",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            raycastBlocker.transform.SetParent(locateInputBlocker.transform, false);

            RectTransform blockerRect = raycastBlocker.GetComponent<RectTransform>();
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;

            Image blockerImage = raycastBlocker.GetComponent<Image>();
            blockerImage.color = Color.clear;
            blockerImage.raycastTarget = true;

            locateInputBlocker.SetActive(false);
        }

        private void SetLocateInputBlocked(bool blocked)
        {
            if (blocked && locateInputBlocker == null)
                CreateLocateInputBlocker();

            if (locateInputBlocker == null)
                return;

            if (blocked)
                BoardPlayerController.Instance?.ClearHover();

            locateInputBlocker.SetActive(blocked);
        }

        private void OnDestroy()
        {
            if (locateInputBlocker != null)
                Destroy(locateInputBlocker);
        }

        private void ShowGuideForStep(TutorialSequenceStepData step)
        {
            if (!IsPlaying || sequenceState.ActiveStep != step)
                return;

            tutorialController.ShowGuide(
                step.CharacterSprite,
                step.CharacterName,
                step.Dialogue,
                step.ShowCharacter);

            highlightPresenter?.ShowGuideStep(step);
        }

        private void CancelPendingStep()
        {
            if (pendingShowTween != null)
            {
                pendingShowTween.Kill();
                pendingShowTween = null;
            }

            sequenceState?.ClearPending();
        }

        private void PrepareNextStepTrigger()
        {
            UnbindWaitingButton();

            TutorialSequenceStepData step = GetNextStep();

            if (step == null ||
                step.Trigger != TutorialStepTrigger.ButtonClicked ||
                targetRegistry == null ||
                !targetRegistry.TryGetButton(step.TriggerButtonKey,out Button button))
                return;

            waitingTriggerButton = button;
            waitingTriggerButtonKey = step.TriggerButtonKey;
            waitingTriggerButton.onClick.AddListener(HandleWaitingButtonClicked);
        }

        private void HandleWaitingButtonClicked()
        {
            NotifyButtonClicked(waitingTriggerButtonKey);
        }

        private void UnbindWaitingButton()
        {
            if (waitingTriggerButton != null)
                waitingTriggerButton.onClick.RemoveListener(HandleWaitingButtonClicked);

            waitingTriggerButton = null;
            waitingTriggerButtonKey = string.Empty;
        }

        private void CompleteSequence()
        {
            sequenceState.Complete();
            UnbindWaitingButton();
            CancelPendingStep();
            highlightPresenter?.CompleteSequence();

            TutorialProgress.MarkCompleted();
            SequenceCompleted?.Invoke();
        }

        private void EnsureSequenceState()
        {
            sequenceState ??=
                new TutorialSequenceState<TutorialSequenceStepData>(steps);
        }

        private void EnsureHighlightPresenter()
        {
            highlightPresenter ??= new TutorialHighlightPresenter(
                targetRegistry,
                readySceneView,
                this);
        }
    }
}
