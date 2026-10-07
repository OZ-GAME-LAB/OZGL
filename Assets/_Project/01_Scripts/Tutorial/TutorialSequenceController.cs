using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using OzGameLab01.Board.Models;
using OzGameLab01.Events;
using OzGameLab01.Managers;
using OzGameLab01.Map;
using OzGameLab01.Data;
using OzGameLab01.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OzGameLab01.Controllers
{
    [DisallowMultipleComponent]
    public sealed class TutorialSequenceController : MonoBehaviour
    {
        public static TutorialSequenceController Active { get; private set; }

        [Header("Controllers")]
        [SerializeField] private TutorialController tutorialController;
        [SerializeField] private ReadySceneView readySceneView;
        [SerializeField] private BoardUIController boardUIController;
        [SerializeField] private TutorialTargetRegistry targetRegistry;
        [SerializeField] private UnitFormationController unitFormationController;
        [Tooltip("비워두면 비활성 오브젝트를 포함해 씬에서 자동으로 찾습니다.")]
        [SerializeField] private EventSession eventSession;

        [Header("Board Tile Focus (Optional)")]
        [SerializeField] private BoardCameraController boardCameraController;
        [SerializeField] private BoardMoveRangeController boardMoveRangeController;
        [SerializeField] private MapGenerator mapGenerator;

        [Header("Sequence Order")]
        [SerializeField] private bool playOnStart = true;
        [Tooltip("각 Step SO를 실행할 순서대로 등록합니다.")]
        [SerializeField] private List<TutorialSequenceStepData> steps = new();

        [Header("Tutorial Board Rules")]
        [Tooltip("현재 Step SO의 Set Next Dice Roll이 꺼져 있거나 예약값을 모두 사용했을 때만 적용되는 기본 눈입니다.")]
        [SerializeField, Range(1,6)] private int defaultDiceRollValue = 2;

        [Header("Tutorial Completion Result")]
        [Tooltip("04_Tutorial 씬에 미리 배치한 ResultCanvas/RunResultUI의 RunResultAnimationView입니다.")]
        [SerializeField] private RunResultAnimationView runResultView;

        private TutorialSequenceState<TutorialSequenceStepData> sequenceState;
        private Button waitingTriggerButton;
        private string waitingTriggerButtonKey;

        private Tween pendingShowTween;
        private Coroutine pendingBossSpawnRoutine;
        private TutorialHighlightPresenter highlightPresenter;
        private TutorialSequenceStepData guideAfterLocateStep;
        private bool tutorialLocateInProgress;
        private bool tutorialTileFocusVisible;
        private GameObject locateInputBlocker;
        private GameObject buttonInteractionBlocker;
        private RectTransform allowedButtonProxyRect;
        private Image allowedButtonProxyImage;
        private Button allowedButtonProxy;
        private Button requiredInteractionButton;
        private TutorialSequenceStepData pendingButtonInteractionStep;
        private GameObject previousSelectedObject;
        private string activeButtonInteractionFeedback;
        private bool hasReservedDiceRoll;
        private int reservedDiceRollValue;
        private MapNode requiredMoveTarget;
        private string invalidMoveFeedback;
        private float invalidMoveFeedbackDuration;
        private Coroutine invalidMoveFeedbackRoutine;
        private TutorialSequenceStepData runtimeBoardTileTargetStep;
        private MapNode runtimeBoardTileTarget;
        private bool returningToTitle;

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
            ResolveEventSession();
            ResolveRunResultView();
            CreateLocateInputBlocker();
            CreateButtonInteractionBlocker();
        }

        private void OnEnable()
        {
            Active = this;

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
            ResolveEventSession();
            EnsureHighlightPresenter();
            highlightPresenter.Configure(targetRegistry, readySceneView);
            BindLocateEvents();
            BindRunResultView();
            BindEventSession();
        }

        private void Start()
        {
            if (boardUIController == null)
                boardUIController = FindFirstObjectByType<BoardUIController>();

            ResolveBoardFocusReferences();
            ResolveUnitFormationController();

            if (playOnStart)
            {
                if (TutorialSessionState.TryConsumeBoardResume(
                        out int resumeStepIndex))
                {
                    PlaySequenceFrom(
                        resumeStepIndex,
                        TutorialStepTrigger.ReturnedFromCombat);
                }
                else
                {
                    PlaySequence();
                }
            }
        }

        private void LateUpdate()
        {
            if (buttonInteractionBlocker == null ||
                !buttonInteractionBlocker.activeSelf)
            {
                return;
            }

            SyncAllowedButtonProxy();
        }

        private void OnDisable()
        {
            if (Active == this)
                Active = null;

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
            UnbindRunResultView();
            UnbindEventSession();
            CancelTutorialLocatePresentation(false);

            UnbindWaitingButton();
            CancelPendingStep();
            CancelPendingBossSpawn();
            highlightPresenter?.StopAll();
            ClearRuntimeRules();
        }

        [ContextMenu("Play Tutorial Sequence")]
        public void PlaySequence()
        {
            TutorialSessionState.BeginNewSession();
            PlaySequenceFrom(0, TutorialStepTrigger.SequenceStarted);
        }

        private void PlaySequenceFrom(
            int nextStepIndex,
            TutorialStepTrigger initialTrigger)
        {
            EnsureSequenceState();
            StopSequence();

            if (tutorialController == null ||
                !sequenceState.HasConfiguredSteps)
                return;

            sequenceState.StartAt(nextStepIndex);
            TutorialSessionState.RecordBoardProgress(
                sequenceState.NextStepIndex);

            PrepareNextStepTrigger();
            TryShowNextStep(initialTrigger);
        }

        [ContextMenu("Stop Tutorial Sequence")]
        public void StopSequence()
        {
            sequenceState?.Stop();

            UnbindWaitingButton();
            CancelPendingStep();
            CancelPendingBossSpawn();
            highlightPresenter?.StopAll();
            CancelTutorialLocatePresentation(true);
            ClearRuntimeRules();
            HideRunResult();

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

        private void HandleEventUIHidden(EventSession _)
        {
            TryShowNextStep(TutorialStepTrigger.EventUIHidden);
        }

        public int ConsumeDiceRollValue()
        {
            if (!hasReservedDiceRoll)
                return Mathf.Clamp(defaultDiceRollValue,1,6);

            hasReservedDiceRoll = false;
            return Mathf.Clamp(reservedDiceRollValue,1,6);
        }

        public bool CanMoveToTile(MapNode target)
        {
            if (requiredMoveTarget == null || target == requiredMoveTarget)
                return true;

            ShowInvalidMoveFeedback();
            return false;
        }

        public void NotifyPlayerArrived(MapNode arrivedNode)
        {
            if (requiredMoveTarget == null || arrivedNode != requiredMoveTarget)
                return;

            ClearRequiredMoveRule();
            TryShowNextStep(TutorialStepTrigger.RequiredTileReached);
        }

        private void HandleGuideDismissed()
        {
            if (!IsPlaying || sequenceState.ActiveStep == null)
                return;

            TutorialSequenceStepData dismissedStep =
                sequenceState.ClearActive();
            highlightPresenter?.HandleGuideDismissed(dismissedStep);
            ActivatePendingButtonInteractionRule(dismissedStep);

            if (dismissedStep.ShowRunResultOnComplete)
            {
                CompleteSequence();
                ShowRunResult();
                return;
            }

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

            if (step.SpawnBossTileNearPlayer)
            {
                pendingBossSpawnRoutine =
                    StartCoroutine(SpawnBossTileThenExecuteStep(step));
                return;
            }

            ExecuteActivatedStep(step);
        }

        private void ExecuteActivatedStep(TutorialSequenceStepData step)
        {
            TutorialSessionState.RecordBoardProgress(
                sequenceState.NextStepIndex);

            ApplyRuntimeRules(step);

            if (!step.ShowGuide && step.ShowRunResultOnComplete)
            {
                sequenceState.ClearActive();
                CompleteSequence();
                ShowRunResult();
                return;
            }

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

        private IEnumerator SpawnBossTileThenExecuteStep(
            TutorialSequenceStepData step)
        {
            while (IsPlaying && sequenceState.ActiveStep == step)
            {
                ResolveBoardFocusReferences();

                BoardPlayerController playerController =
                    BoardPlayerController.Instance;
                if (mapGenerator != null &&
                    mapGenerator.IsPresentationComplete &&
                    playerController != null &&
                    playerController.CurrentNode != null)
                {
                    break;
                }

                yield return null;
            }

            if (!IsPlaying || sequenceState.ActiveStep != step)
            {
                pendingBossSpawnRoutine = null;
                yield break;
            }

            if (!TrySpawnBossTileNearPlayer(step,out MapNode bossNode))
            {
                pendingBossSpawnRoutine = null;
                Debug.LogError(
                    $"[TutorialSequenceController] 플레이어 주변에 보스 타일을 생성하지 못했습니다. " +
                    $"Normal 타일과 거리 설정을 확인해주세요. Step: {step.StepName}",
                    this);
                sequenceState.Stop();
                yield break;
            }

            // 기존 타일 오브젝트 제거가 프레임 종료에 반영된 뒤 Guide와 Locate를 시작합니다.
            yield return null;

            pendingBossSpawnRoutine = null;
            if (IsPlaying && sequenceState.ActiveStep == step && bossNode != null)
                ExecuteActivatedStep(step);
        }

        private bool TrySpawnBossTileNearPlayer(
            TutorialSequenceStepData step,
            out MapNode bossNode)
        {
            bossNode = null;
            ResolveBoardFocusReferences();

            BoardPlayerController playerController = BoardPlayerController.Instance;
            MapNode playerNode = playerController != null
                ? playerController.CurrentNode
                : null;

            if (mapGenerator == null || playerNode == null)
                return false;

            int minDistance = Mathf.Max(1,step.BossSpawnMinDistance);
            int maxDistance = Mathf.Max(
                minDistance,
                step.BossSpawnMaxDistance);
            IReadOnlyDictionary<MapNode,int> reachableNodes =
                BoardPathfinder.GetReachableNodeDistances(
                    playerNode,
                    maxDistance);

            MapNode existingBoss = null;
            int existingBossDistance = -1;
            MapNode normalCandidate = null;
            int normalCandidateDistance = -1;

            foreach (KeyValuePair<MapNode,int> pair in reachableNodes)
            {
                MapNode candidate = pair.Key;
                int distance = pair.Value;
                if (candidate == null || distance < minDistance)
                    continue;

                if (candidate.Type == NodeType.Boss &&
                    !BoardRunData.IsSpecialTileConsumed(candidate.Position) &&
                    IsBetterBossSpawnCandidate(
                        candidate,
                        distance,
                        existingBoss,
                        existingBossDistance))
                {
                    existingBoss = candidate;
                    existingBossDistance = distance;
                    continue;
                }

                if (candidate.Type == NodeType.Normal &&
                    !BoardRunData.IsSpecialTileConsumed(candidate.Position) &&
                    IsBetterBossSpawnCandidate(
                        candidate,
                        distance,
                        normalCandidate,
                        normalCandidateDistance))
                {
                    normalCandidate = candidate;
                    normalCandidateDistance = distance;
                }
            }

            bossNode = existingBoss != null ? existingBoss : normalCandidate;
            if (bossNode == null)
                return false;

            if (bossNode.Type != NodeType.Boss)
            {
                bossNode.Type = NodeType.Boss;
                mapGenerator.ReplaceTileVisual(bossNode);

                if (mapGenerator.GetNodeView(bossNode) == null)
                {
                    bossNode.Type = NodeType.Normal;
                    mapGenerator.ReplaceTileVisual(bossNode);
                    bossNode = null;
                    return false;
                }
            }

            bossNode.EncounterMonsterId = step.BossMonsterId;
            runtimeBoardTileTargetStep = step;
            runtimeBoardTileTarget = bossNode;
            BoardRunData.SaveObjectivePosition(bossNode.Position);
            return true;
        }

        private static bool IsBetterBossSpawnCandidate(
            MapNode candidate,
            int candidateDistance,
            MapNode current,
            int currentDistance)
        {
            return current == null ||
                   candidateDistance > currentDistance ||
                   (candidateDistance == currentDistance &&
                    CompareNodePosition(candidate,current) < 0);
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

            if (!TryResolveTileNode(step,out node))
                return false;

            GameObject nodeView = mapGenerator.GetNodeView(node);
            target = nodeView != null ? nodeView.transform : null;
            return target != null;
        }

        private bool TryResolveTileNode(
            TutorialSequenceStepData step,
            out MapNode node)
        {
            ResolveBoardFocusReferences();
            node = null;

            if (mapGenerator == null || mapGenerator.NodeDict.Count == 0)
                return false;

            if (step == runtimeBoardTileTargetStep &&
                runtimeBoardTileTarget != null)
            {
                node = runtimeBoardTileTarget;
                return true;
            }

            return TryResolveConfiguredTileNode(
                step.TileTargetMode,
                step.TilePosition,
                step.TileType,
                step.TileTypeOccurrence,
                out node);
        }

        private bool TryResolveMoveTargetNode(
            TutorialSequenceStepData step,
            out MapNode node)
        {
            if (!step.UseSeparateMoveTarget)
                return TryResolveTileNode(step,out node);

            ResolveBoardFocusReferences();
            return TryResolveConfiguredTileNode(
                step.MoveTargetMode,
                step.MoveTilePosition,
                step.MoveTileType,
                step.MoveTileTypeOccurrence,
                out node);
        }

        private bool TryResolveConfiguredTileNode(
            TutorialTileTargetMode targetMode,
            Vector2Int targetPosition,
            NodeType targetType,
            int targetTypeOccurrence,
            out MapNode node)
        {
            node = null;
            if (mapGenerator == null || mapGenerator.NodeDict.Count == 0)
                return false;

            if (targetMode == TutorialTileTargetMode.Position)
            {
                mapGenerator.NodeDict.TryGetValue(targetPosition,out node);
            }
            else
            {
                List<MapNode> matches = new List<MapNode>();
                foreach (MapNode candidate in mapGenerator.NodeDict.Values)
                {
                    if (candidate != null && candidate.Type == targetType)
                    {
                        matches.Add(candidate);
                    }
                }

                matches.Sort(CompareNodePosition);
                if (targetTypeOccurrence >= 0 &&
                    targetTypeOccurrence < matches.Count)
                {
                    node = matches[targetTypeOccurrence];
                }
            }

            return node != null;
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

        private void ResolveEventSession()
        {
            if (eventSession == null)
            {
                eventSession = FindFirstObjectByType<EventSession>(
                    FindObjectsInactive.Include);
            }
        }

        private void BindEventSession()
        {
            ResolveEventSession();
            if (eventSession == null)
                return;

            eventSession.Hidden -= HandleEventUIHidden;
            eventSession.Hidden += HandleEventUIHidden;
        }

        private void UnbindEventSession()
        {
            if (eventSession != null)
                eventSession.Hidden -= HandleEventUIHidden;
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

        private void CreateButtonInteractionBlocker()
        {
            if (buttonInteractionBlocker != null)
                return;

            buttonInteractionBlocker = new GameObject(
                "TutorialButtonInteractionBlocker",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster));

            Canvas blockerCanvas =
                buttonInteractionBlocker.GetComponent<Canvas>();
            blockerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            blockerCanvas.overrideSorting = true;
            blockerCanvas.sortingOrder = short.MaxValue;

            GameObject raycastBlocker = new GameObject(
                "RaycastBlocker",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            raycastBlocker.transform.SetParent(
                buttonInteractionBlocker.transform,
                false);

            RectTransform blockerRect =
                raycastBlocker.GetComponent<RectTransform>();
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;

            Image blockerImage = raycastBlocker.GetComponent<Image>();
            blockerImage.color = Color.clear;
            blockerImage.raycastTarget = true;

            GameObject allowedButtonObject = new GameObject(
                "AllowedButtonProxy",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            allowedButtonObject.transform.SetParent(
                buttonInteractionBlocker.transform,
                false);

            allowedButtonProxyRect =
                allowedButtonObject.GetComponent<RectTransform>();
            allowedButtonProxyRect.anchorMin = new Vector2(0.5f,0.5f);
            allowedButtonProxyRect.anchorMax = new Vector2(0.5f,0.5f);
            allowedButtonProxyRect.pivot = new Vector2(0.5f,0.5f);

            allowedButtonProxyImage =
                allowedButtonObject.GetComponent<Image>();
            allowedButtonProxyImage.color = Color.clear;
            allowedButtonProxyImage.raycastTarget = true;

            allowedButtonProxy = allowedButtonObject.GetComponent<Button>();
            allowedButtonProxy.transition = Selectable.Transition.None;
            allowedButtonProxy.targetGraphic = allowedButtonProxyImage;
            Navigation navigation = allowedButtonProxy.navigation;
            navigation.mode = Navigation.Mode.None;
            allowedButtonProxy.navigation = navigation;
            allowedButtonProxy.onClick.AddListener(
                HandleAllowedButtonProxyClicked);

            buttonInteractionBlocker.SetActive(false);
        }

        private void PrepareButtonInteractionRule(
            TutorialSequenceStepData step)
        {
            if (!step.BlockOtherInteractionsUntilButtonClicked)
                return;

            if (step.ShowGuide)
            {
                pendingButtonInteractionStep = step;
                return;
            }

            TryActivateButtonInteractionRule(step);
        }

        private void ActivatePendingButtonInteractionRule(
            TutorialSequenceStepData dismissedStep)
        {
            if (dismissedStep == null ||
                pendingButtonInteractionStep != dismissedStep)
            {
                return;
            }

            pendingButtonInteractionStep = null;
            TryActivateButtonInteractionRule(dismissedStep);
        }

        private bool TryActivateButtonInteractionRule(
            TutorialSequenceStepData step)
        {
            if (targetRegistry == null ||
                !targetRegistry.TryGetButton(
                    step.RequiredInteractionButtonKey,
                    out Button targetButton))
            {
                Debug.LogWarning(
                    $"[TutorialSequenceController] 입력 제한 대상 버튼을 찾지 못했습니다. " +
                    $"Step: {step.StepName}, Key: {step.RequiredInteractionButtonKey}",
                    this);
                return false;
            }

            if (buttonInteractionBlocker == null)
                CreateButtonInteractionBlocker();

            requiredInteractionButton = targetButton;
            BoardPlayerController.Instance?.ClearHover();

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null)
                previousSelectedObject = eventSystem.currentSelectedGameObject;

            buttonInteractionBlocker.SetActive(true);
            SyncAllowedButtonProxy();
            ShowButtonInteractionFeedback(step);
            return true;
        }

        private void ShowButtonInteractionFeedback(
            TutorialSequenceStepData step)
        {
            FeedbackView feedbackView = readySceneView != null
                ? readySceneView.FeedbackView
                : null;
            if (feedbackView == null)
                return;

            activeButtonInteractionFeedback =
                step.RequiredButtonFeedback;
            if (string.IsNullOrWhiteSpace(activeButtonInteractionFeedback))
            {
                activeButtonInteractionFeedback =
                    $"{step.RequiredInteractionButtonKey} 버튼을 눌러주세요.";
            }

            feedbackView.Show(activeButtonInteractionFeedback);
        }

        private void HideButtonInteractionFeedback()
        {
            FeedbackView feedbackView = readySceneView != null
                ? readySceneView.FeedbackView
                : null;
            if (feedbackView != null &&
                !string.IsNullOrWhiteSpace(activeButtonInteractionFeedback) &&
                string.Equals(
                    feedbackView.Message,
                    activeButtonInteractionFeedback,
                    StringComparison.Ordinal))
            {
                feedbackView.Hide();
            }

            activeButtonInteractionFeedback = string.Empty;
        }

        private void SyncAllowedButtonProxy()
        {
            if (allowedButtonProxyRect == null ||
                allowedButtonProxy == null ||
                requiredInteractionButton == null)
            {
                ReleaseButtonInteractionRule();
                return;
            }

            RectTransform targetRect =
                requiredInteractionButton.transform as RectTransform;
            bool targetVisible = targetRect != null &&
                                 requiredInteractionButton.gameObject
                                     .activeInHierarchy;
            allowedButtonProxyRect.gameObject.SetActive(targetVisible);
            if (!targetVisible)
            {
                LockEventSystemSelection(null);
                return;
            }

            allowedButtonProxy.interactable =
                requiredInteractionButton.IsInteractable();

            Vector3[] worldCorners = new Vector3[4];
            targetRect.GetWorldCorners(worldCorners);

            Canvas targetCanvas = targetRect.GetComponentInParent<Canvas>();
            Camera targetCamera = targetCanvas != null &&
                                  targetCanvas.renderMode !=
                                  RenderMode.ScreenSpaceOverlay
                ? targetCanvas.worldCamera
                : null;
            RectTransform blockerRoot =
                buttonInteractionBlocker.transform as RectTransform;

            Vector2 min = new(float.PositiveInfinity,float.PositiveInfinity);
            Vector2 max = new(float.NegativeInfinity,float.NegativeInfinity);
            for (int i = 0; i < worldCorners.Length; i++)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                    targetCamera,
                    worldCorners[i]);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    blockerRoot,
                    screenPoint,
                    null,
                    out Vector2 localPoint);

                min = Vector2.Min(min,localPoint);
                max = Vector2.Max(max,localPoint);
            }

            allowedButtonProxyRect.anchoredPosition = (min + max) * 0.5f;
            allowedButtonProxyRect.sizeDelta = max - min;
            allowedButtonProxyRect.SetAsLastSibling();
            LockEventSystemSelection(allowedButtonProxy.gameObject);
        }

        private static void LockEventSystemSelection(GameObject selection)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null &&
                eventSystem.currentSelectedGameObject != selection)
            {
                eventSystem.SetSelectedGameObject(selection);
            }
        }

        private void HandleAllowedButtonProxyClicked()
        {
            Button targetButton = requiredInteractionButton;
            if (targetButton == null ||
                !targetButton.gameObject.activeInHierarchy ||
                !targetButton.IsInteractable())
            {
                return;
            }

            ReleaseButtonInteractionRule();
            targetButton.onClick.Invoke();
        }

        private void ReleaseButtonInteractionRule()
        {
            requiredInteractionButton = null;
            HideButtonInteractionFeedback();

            if (buttonInteractionBlocker != null)
                buttonInteractionBlocker.SetActive(false);

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null &&
                eventSystem.currentSelectedGameObject ==
                allowedButtonProxy?.gameObject)
            {
                GameObject selection = previousSelectedObject != null &&
                                       previousSelectedObject.activeInHierarchy
                    ? previousSelectedObject
                    : null;
                eventSystem.SetSelectedGameObject(selection);
            }

            previousSelectedObject = null;
        }

        private void OnDestroy()
        {
            if (Active == this)
                Active = null;

            UnbindRunResultView();

            if (locateInputBlocker != null)
                Destroy(locateInputBlocker);

            if (buttonInteractionBlocker != null)
                Destroy(buttonInteractionBlocker);
        }

        private void ResolveRunResultView()
        {
            if (runResultView == null)
            {
                runResultView = FindFirstObjectByType<RunResultAnimationView>(
                    FindObjectsInactive.Include);
            }
        }

        private void BindRunResultView()
        {
            ResolveRunResultView();
            if (runResultView == null)
                return;

            runResultView.MainButtonClicked -= HandleRunResultMainButtonClicked;
            runResultView.MainButtonClicked += HandleRunResultMainButtonClicked;
        }

        private void UnbindRunResultView()
        {
            if (runResultView != null)
            {
                runResultView.MainButtonClicked -=
                    HandleRunResultMainButtonClicked;
            }
        }

        private void ShowRunResult()
        {
            ResolveRunResultView();
            if (runResultView == null)
            {
                Debug.LogError(
                    "[TutorialSequenceController] 씬에 배치된 RunResultAnimationView를 찾지 못했습니다.",
                    this);
                return;
            }

            BindRunResultView();
            ClearRuntimeRules();
            CancelTutorialLocatePresentation(false);
            readySceneView?.HideAllOverlayViews();

            bool wasActive = runResultView.gameObject.activeSelf;
            runResultView.gameObject.SetActive(true);
            if (wasActive)
                runResultView.Replay();
        }

        private void HideRunResult()
        {
            if (runResultView != null && runResultView.gameObject.activeSelf)
                runResultView.Hide();

            returningToTitle = false;
        }

        private void HandleRunResultMainButtonClicked(
            RunResultAnimationView view)
        {
            if (returningToTitle)
                return;

            SceneTransitioner transitioner = SceneTransitioner.Instance;
            if (transitioner == null || transitioner.IsTransitioning)
                return;

            returningToTitle = true;
            if (view != null && view.MainButton != null)
                view.MainButton.interactable = false;

            Time.timeScale = 1f;
            TutorialProgress.MarkCompleted();

            transitioner.LoadTitleScene();
        }

        private void ApplyRuntimeRules(TutorialSequenceStepData step)
        {
            PrepareButtonInteractionRule(step);

            if (step.SetNextDiceRoll)
            {
                reservedDiceRollValue = Mathf.Clamp(step.NextDiceRollValue,1,6);
                hasReservedDiceRoll = true;
            }

            if (!step.RequireMoveToTargetTile)
                return;

            if (!TryResolveMoveTargetNode(step,out MapNode target))
            {
                Debug.LogWarning(
                    $"[TutorialSequenceController] 이동 제한 대상을 찾지 못했습니다. Step: {step.StepName}",
                    this);
                return;
            }

            requiredMoveTarget = target;
            invalidMoveFeedback = step.InvalidMoveFeedback;
            invalidMoveFeedbackDuration = step.InvalidMoveFeedbackDuration;
        }

        private void ShowInvalidMoveFeedback()
        {
            if (readySceneView == null ||
                string.IsNullOrWhiteSpace(invalidMoveFeedback))
            {
                return;
            }

            FeedbackView feedbackView = readySceneView.FeedbackView;
            if (feedbackView == null)
                return;

            if (invalidMoveFeedbackRoutine != null)
                StopCoroutine(invalidMoveFeedbackRoutine);

            feedbackView.Show(invalidMoveFeedback);
            invalidMoveFeedbackRoutine =
                StartCoroutine(HideInvalidMoveFeedbackRoutine());
        }

        private System.Collections.IEnumerator HideInvalidMoveFeedbackRoutine()
        {
            if (invalidMoveFeedbackDuration > 0f)
                yield return new WaitForSecondsRealtime(invalidMoveFeedbackDuration);

            HideInvalidMoveFeedback();
            invalidMoveFeedbackRoutine = null;
        }

        private void ClearRequiredMoveRule()
        {
            requiredMoveTarget = null;
            invalidMoveFeedback = string.Empty;
            invalidMoveFeedbackDuration = 0f;

            if (invalidMoveFeedbackRoutine != null)
            {
                StopCoroutine(invalidMoveFeedbackRoutine);
                invalidMoveFeedbackRoutine = null;
            }

            HideInvalidMoveFeedback();
        }

        private void HideInvalidMoveFeedback()
        {
            if (readySceneView == null)
                return;

            FeedbackView feedbackView = readySceneView.FeedbackView;
            if (feedbackView != null)
                feedbackView.Hide();
        }

        private void ClearRuntimeRules()
        {
            hasReservedDiceRoll = false;
            reservedDiceRollValue = 0;
            runtimeBoardTileTargetStep = null;
            runtimeBoardTileTarget = null;
            pendingButtonInteractionStep = null;
            ReleaseButtonInteractionRule();
            ClearRequiredMoveRule();
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

        private void CancelPendingBossSpawn()
        {
            if (pendingBossSpawnRoutine == null)
                return;

            StopCoroutine(pendingBossSpawnRoutine);
            pendingBossSpawnRoutine = null;
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
