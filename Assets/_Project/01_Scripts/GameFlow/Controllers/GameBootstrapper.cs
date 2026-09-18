using System.Collections;
using System.Collections.Generic;
using OzGameLab01.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OzGameLab01.Managers
{
    /// <summary>전역 루트 수명과 매니저 초기화 이후 타이틀 이동</summary>
    public class GameBootstrapper : MonoBehaviour, OzGameLab01.GameFlow.Contracts.IGameFlowNotificationSource
    {
        private readonly OzGameLab01.GameFlow.Controllers.GameFlowNotificationPublisher _notifications = new OzGameLab01.GameFlow.Controllers.GameFlowNotificationPublisher();
        public event System.Action<OzGameLab01.GameFlow.Models.GameFlowNotification> Notification
        {
            add => _notifications.Notification += value;
            remove => _notifications.Notification -= value;
        }

        [Header("초기화 대상 매니저")]
        [SerializeField] private List<MonoBehaviour> _managerComponents = new();
        private static GameBootstrapper _instance;
        private bool _isRootObjectValid;
        private OzGameLab01.Controllers.ManagerInitializationController _initialization;
        public bool IsInitializationComplete => _initialization != null && _initialization.IsInitializationComplete;

        private void InitializeManagers()
        {
            if (IsInitializationComplete) return;
            _initialization = new OzGameLab01.Controllers.ManagerInitializationController(_managerComponents, this);
            _initialization.Initialize();
            _notifications.Publish(IsInitializationComplete
                ? OzGameLab01.GameFlow.Models.GameFlowNotificationKind.ManagersReady
                : OzGameLab01.GameFlow.Models.GameFlowNotificationKind.ManagersFailed);
            if (IsInitializationComplete) StartCoroutine(LoadTitleSceneAfterInitialization());
        }

        private void Awake()
        {
            // DontDestroyOnLoad 적용 전 루트 오브젝트 여부 검사
            _isRootObjectValid = ValidateRootObject();

            // 루트 검증 실패 시 컴포넌트 비활성화 및 초기화 중단
            if (!_isRootObjectValid)
            {
                enabled = false;
                return;
            }

            // 이미 유지되고 있는 GameBootstrapper가 있다면
            // 새로 생성된 GlobalManagers 루트 전체를 제거
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning(
                    "[GameBootstrapper] 기존 GlobalManagers가 이미 존재하여 " +
                    $"중복 생성된 '{gameObject.name}' 오브젝트를 제거합니다.", this);

                // 제거되기 전 Start()가 실행되지 않도록 비활성화
                enabled = false;

                Destroy(gameObject);
                return;
            }

            // 최초 GameBootstrapper 인스턴스 등록
            _instance = this;

            // GlobalManagers 루트와 모든 자식 매니저 유지
            DontDestroyOnLoad(gameObject);

            Debug.Log(
                $"[GameBootstrapper] 전역 매니저 루트 등록 완료 | {gameObject.name}", this);
        }

        private void Start()
        {
            // Awake에서 루트 검증에 실패한 경우 초기화 중단
            if (!_isRootObjectValid)
            {
                return;
            }

            RegisterSystemBusManagers();

            // 모든 Awake() 콜백 완료 후 매니저 초기화 시작
            InitializeManagers();

        }

        /// <summary>
        /// Dice/Combat/Event/Player/RuntimeEffect/Relic/Save는 IGameManager를 구현하지만 씬 직접 실행
        /// 호환을 위해 각자 Awake에서 스스로 Initialize합니다. 아무도 먼저 .Instance를
        /// 건드리지 않으면 부팅 경로에서 아예 생성되지 않아 SystemBus.Get&lt;T&gt;()가
        /// 항상 null을 반환하므로, 여기서 생성을 강제하고 ManagerInitializationController의
        /// 등록 목록에도 추가해 준비 상태를 다른 매니저와 동일하게 추적합니다(이미
        /// 초기화되어 있으므로 실제 재초기화는 일어나지 않습니다).
        /// </summary>
        private void RegisterSystemBusManagers()
        {
            _managerComponents.Add(DiceManager.Instance);
            _managerComponents.Add(CombatManager.Instance);
            _managerComponents.Add(EventManager.Instance);
            _managerComponents.Add(PlayerInventoryManager.Instance);
            _managerComponents.Add(RuntimeEffectManager.Instance);
            _managerComponents.Add(RelicManager.Instance);
            _managerComponents.Add(SaveManager.Instance);
        }

        /// <summary>
        /// 이 컴포넌트가 씬 계층의 루트 오브젝트에 부착되어 있는지 확인합니다.
        /// </summary>
        private bool ValidateRootObject()
        {
            if (transform.parent == null)
            {
                return true;
            }

            Debug.LogError(
                $"[GameBootstrapper] '{gameObject.name}'은(는) " +
                $"루트 오브젝트가 아닙니다. 현재 부모: '{transform.parent.name}'. " +
                "GameBootstrapper를 GlobalManagers 루트 오브젝트에 부착해주세요.", this);

            return false;
        }

        private IEnumerator LoadTitleSceneAfterInitialization()
        {
            // 같은 프레임의 모든 Start() 호출이 끝날 때까지 대기
            yield return null;

            // 00_Boot에서 실행된 경우에만 자동 이동
            if (SceneManager.GetActiveScene().name != SceneNames.Boot)
            {
                yield break;
            }

            // SceneTransitioner 준비 여부 확인
            if (SceneTransitioner.Instance == null)
            {
                Debug.LogError(
                    "[GameBootstrapper] SceneTransitioner가 준비되지 않아 " +
                    "타이틀 씬으로 이동할 수 없습니다.", this);

                yield break;
            }

            Debug.Log(
                "[GameBootstrapper] 전역 매니저 준비 완료 | 타이틀 씬 이동", this);

            SceneTransitioner.Instance.LoadTitleScene();
        }
        private void OnDestroy()
        {
            // 중복 생성 후 제거된 GameBootstrapper는
            // 기존 전역 매니저의 종료 작업에 관여하지 않음
            if (_instance != this)
            {
                return;
            }

            // 이 부트스트래퍼가 직접 초기화한 매니저 정리
            _initialization?.ShutdownManagers();
            _notifications.ClearSubscribers();

            // 정적 인스턴스 참조 해제
            _instance = null;

            Debug.Log("[GameBootstrapper] 전역 매니저 루트 종료 완료", this);
        }
    }
}