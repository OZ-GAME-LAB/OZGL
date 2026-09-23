using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OzGameLab01.Data;
using OzGameLab01.Common;

namespace OzGameLab01.Managers
{
    public class SceneTransitioner : MonoBehaviour, OzGameLab01.GameFlow.Contracts.IGameFlowNotificationSource
    {
        public enum CombatEntryMode
        {
            Normal,
            Tutorial
        }

        private readonly OzGameLab01.GameFlow.Controllers.GameFlowNotificationPublisher _notifications = new OzGameLab01.GameFlow.Controllers.GameFlowNotificationPublisher();
        public event System.Action<OzGameLab01.GameFlow.Models.GameFlowNotification> Notification
        {
            add => _notifications.Notification += value;
            remove => _notifications.Notification -= value;
        }

        public static SceneTransitioner Instance { get; set; }
        public static int MapTileIndex { get; set; } = 0;

        /// <summary>
        /// 유닛 편성 화면(UnitFormationCombatLink)이 채우는 배치 결과(인덱스 0-8, 3x3 row-major).
        /// CombatSession이 이 데이터가 있으면 우선 사용하고, 비어 있으면 저장된 편성을 복원합니다.
        /// </summary>
        public static UnitData[] AllyFormationData { get; set; }

        /// <summary>
        /// ProtoBoardScene에서 CombatScene으로 진입하기 직전의 보드 위치(MapNode.Position).
        /// 전투 종료 후 ProtoBoardScene으로 복귀할 때 이 위치에 플레이어를 되돌려 놓는다.
        /// 기본값 (0,0)은 시작 노드 위치와 같아 별도 플래그 없이도 "복귀 위치 없음"과 자연히 일치한다.
        /// </summary>
        public static Vector2Int BoardReturnPosition { get; set; }

        [UnityEngine.Serialization.FormerlySerializedAs("fadeImage")]

        [SerializeField] private Image _fadeImage;
        [UnityEngine.Serialization.FormerlySerializedAs("fadeDuration")]
        [SerializeField] private float _fadeDuration = 0.3f;

        // ==================== 씬 전환 리팩터링 ====================

        /// <summary>
        /// 현재 씬 전환이 진행 중인지 나타냅니다.
        /// </summary>
        private OzGameLab01.GameFlow.Models.SceneTransitionModel _transition = new OzGameLab01.GameFlow.Models.SceneTransitionModel();
        private OzGameLab01.GameFlow.Views.SceneFadeView _fadeView;
        private Coroutine _initialFade;
        private AsyncOperation _activeLoad;
        private CombatEntryMode _combatEntryMode = CombatEntryMode.Normal;
        private string _combatEntryFromScene = string.Empty;
        private string _combatEntryToScene = string.Empty;
        private bool _combatEntryPending;

        /// <summary>
        /// 외부에서 현재 씬 전환 여부를 확인할 수 있습니다.
        /// </summary>
        public bool IsTransitioning => _transition.IsTransitioning;
        public CombatEntryMode CurrentCombatEntryMode => _combatEntryMode;
        public bool IsTutorialCombat =>
            _combatEntryPending &&
            _combatEntryMode == CombatEntryMode.Tutorial;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _transition = new OzGameLab01.GameFlow.Models.SceneTransitionModel(SystemBus.Operations);
            _fadeView = new OzGameLab01.GameFlow.Views.SceneFadeView(_fadeImage, _fadeDuration);
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (Instance == this && !IsTransitioning) _initialFade = StartCoroutine(Fade(1f, 0f));
        }

        // ==================== 씬 전환 기능 ====================
        /// <summary>
        /// 지정한 씬을 비동기로 불러옵니다.
        /// 씬 전환 중에는 추가 요청을 받지 않습니다.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            LoadScene(sceneName,null);
        }

        private void LoadScene(
            string sceneName,
            CombatEntryMode? combatEntryMode)
        {
            // 중복 씬 전환 요청 방지
            if (IsTransitioning || (_activeLoad != null && !_activeLoad.isDone))
            {
                Debug.LogWarning(
                    $"[SceneTransitioner] 씬 전환 중이므로 '{sceneName}' 요청을 건너뜁니다.", this);

                return;
            }

            // 빈 씬 이름 방지
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[SceneTransitioner] 씬 이름이 비어 있어 전환할 수 없습니다.", this);

                return;
            }

            // Scene List에 등록되지 않은 씬 요청 방지
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError(
                    $"[SceneTransitioner] '{sceneName}' 씬을 불러올 수 없습니다. " +
                    "Build Profiles의 Scene List 등록 여부를 확인해주세요.", this);

                return;
            }

            if (_initialFade != null) { StopCoroutine(_initialFade); _initialFade = null; }
            StartCoroutine(LoadSceneRoutine(sceneName,combatEntryMode));
        }

        // ==================== 공용 씬 이동 메서드 ====================

        /// <summary>
        /// 타이틀 씬으로 이동합니다.
        /// </summary>
        public void LoadTitleScene()
        {
            LoadScene(SceneNames.Title);
        }

        /// <summary>
        /// 보드 씬으로 이동합니다.
        /// </summary>
        public void LoadBoardScene()
        {
            LoadScene(SceneNames.Board);
        }

        /// <summary>
        /// 전투 씬으로 이동합니다.
        /// </summary>
        public void LoadCombatScene()
        {
            CombatEntryMode entryMode =
                SceneManager.GetActiveScene().name == SceneNames.Tutorial
                ? CombatEntryMode.Tutorial
                : CombatEntryMode.Normal;

            LoadScene(SceneNames.Combat,entryMode);
        }

        /// <summary>
        /// 튜토리얼 씬으로 이동합니다.
        /// </summary>
        public void LoadTutorialScene()
        {
            LoadScene(SceneNames.Tutorial);
        }

        public bool TryConsumeTutorialCombatEntry()
        {
            bool isTutorialEntry =
                _combatEntryPending &&
                _combatEntryMode == CombatEntryMode.Tutorial &&
                string.Equals(
                    _combatEntryFromScene,
                    SceneNames.Tutorial,
                    System.StringComparison.Ordinal) &&
                string.Equals(
                    _combatEntryToScene,
                    SceneNames.Combat,
                    System.StringComparison.Ordinal) &&
                string.Equals(
                    SceneManager.GetActiveScene().name,
                    SceneNames.Combat,
                    System.StringComparison.Ordinal);

            ClearCombatEntryContext();
            return isTutorialEntry;
        }

        private IEnumerator LoadSceneRoutine(
            string sceneName,
            CombatEntryMode? combatEntryMode)
        {
            // 씬 전환 시작
            string previousSceneName = SceneManager.GetActiveScene().name;
            if (!_transition.TryBegin(previousSceneName, sceneName)) yield break;

            if (combatEntryMode.HasValue && sceneName == SceneNames.Combat)
            {
                SetCombatEntryContext(
                    previousSceneName,
                    sceneName,
                    combatEntryMode.Value);
            }
            else
            {
                ClearCombatEntryContext();
            }

            PublishTransition(OzGameLab01.GameFlow.Models.GameFlowNotificationKind.TransitionStarted);
            if (!IsTransitioning)
            {
                ClearCombatEntryContext();
                yield break;
            }

            Debug.Log(
                $"[SceneTransitioner] 씬 전환 시작 | " +
                $"{previousSceneName} → {sceneName}",
                this);

            // 화면을 어둡게 전환
            yield return Fade(0f, 1f);

            // 일시정지 상태가 다음 씬까지 이어지지 않도록 복구
            Time.timeScale = 1f;

            // 씬 비동기 로드 시작
            AsyncOperation loadOperation = null;
            try
            {
                loadOperation = SceneManager.LoadSceneAsync(sceneName);
                _activeLoad = loadOperation;
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, this);
            }

            if (loadOperation == null)
            {
                Debug.LogError(
                    $"[SceneTransitioner] '{sceneName}' 씬의 " +
                    "비동기 로드를 시작하지 못했습니다.",
                    this);

                yield return Fade(1f, 0f);

                _transition.Cancel();
                ClearCombatEntryContext();
                PublishTransition(OzGameLab01.GameFlow.Models.GameFlowNotificationKind.TransitionFailed);
                yield break;
            }

            // 씬 로드 완료 대기
            while (!loadOperation.isDone)
            {
                yield return null;
            }

            PublishTransition(OzGameLab01.GameFlow.Models.GameFlowNotificationKind.SceneActivated);
            if (!IsTransitioning)
            {
                ClearCombatEntryContext();
                yield break;
            }

            // 새 씬의 초기 콜백 실행을 위해 한 프레임 대기
            yield return null;

            // 화면을 다시 밝게 전환
            yield return Fade(1f, 0f);

            // 씬 전환 완료
            _transition.Finish();

            _activeLoad = null;
            PublishTransition(OzGameLab01.GameFlow.Models.GameFlowNotificationKind.TransitionCompleted);
            Debug.Log($"[SceneTransitioner] 씬 전환 완료 | {sceneName}", this);
        }

        private IEnumerator Fade(float fromAlpha, float toAlpha) => _fadeView.Fade(fromAlpha, toAlpha);

        private void SetCombatEntryContext(
            string fromScene,
            string toScene,
            CombatEntryMode entryMode)
        {
            _combatEntryFromScene = fromScene ?? string.Empty;
            _combatEntryToScene = toScene ?? string.Empty;
            _combatEntryMode = entryMode;
            _combatEntryPending = true;
        }

        private void ClearCombatEntryContext()
        {
            _combatEntryFromScene = string.Empty;
            _combatEntryToScene = string.Empty;
            _combatEntryMode = CombatEntryMode.Normal;
            _combatEntryPending = false;
        }

        private void PublishTransition(OzGameLab01.GameFlow.Models.GameFlowNotificationKind kind)
            => _notifications.Publish(kind, _transition.FromScene, _transition.ToScene);

        private void OnDisable()
        {
            if (Instance != this) return;
            StopAllCoroutines();
            if (!IsTransitioning) return;
            // 코루틴 중단으로 이미 시작된 엔진 로드가 취소되지는 않는다.
            // 실제 로드가 끝날 때까지 전역 작업 잠금을 유지한다.
            if (_activeLoad != null && !_activeLoad.isDone)
                _activeLoad.completed += _ => _transition.Cancel();
            else
                _transition.Cancel();
            ClearCombatEntryContext();
            PublishTransition(OzGameLab01.GameFlow.Models.GameFlowNotificationKind.TransitionInterrupted);
        }

        private void OnDestroy()
        {
            // 현재 인스턴스가 제거될 때만 정적 참조 해제
            if (Instance == this)
            {
                _notifications.ClearSubscribers();
                Instance = null;
            }
        }
    }

}
