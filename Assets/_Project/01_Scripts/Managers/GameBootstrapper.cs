using System;
using System.Collections.Generic;
using OzGameLab01.Interfaces;
using UnityEngine;

namespace OzGameLab01.Managers
{
    /// <summary>
    /// 게임 시작 시 모든 매니저의 순차 초기화
    /// 게임 종료 시 초기화 역순으로 정리
    /// GlobalManagers 루트 오브젝트에 필수 부착
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        [Header("초기화 대상 매니저")]
        [Tooltip("위에서 아래 순서대로 초기화할 매니저 컴포넌트를 등록합니다.")]
        [SerializeField] private List<MonoBehaviour> _managerComponents = new();

        /// <summary>
        /// 정상적으로 초기화된 매니저를 순서대로 보관합니다.
        /// 종료 시 이 목록을 역순으로 순회합니다.
        /// </summary>
        private readonly List<IGameManager> _initializedManagers = new();

        /// <summary>
        /// 같은 매니저의 중복 등록을 검사하기 위한 목록입니다.
        /// </summary>
        private readonly HashSet<IGameManager> _registeredManagers = new();

        /// <summary>
        /// GameBootstrapper가 올바른 루트 오브젝트에 있는지 나타냅니다.
        /// </summary>
        private bool _isRootObjectValid;

        /// <summary>
        /// 등록된 모든 매니저의 초기화가 완료되었는지 반환합니다.
        /// </summary>
        public bool IsInitializationComplete { get; private set; }

        // ==================== 1단계: 준비 ====================

        private void Awake()
        {
            // DontDestroyOnLoad 적용 전 루트 오브젝트 여부 검사
            // 자식 오브젝트에 부착된 경우 씬 전환 후 유지되지 않을 수 있음
            _isRootObjectValid = ValidateRootObject();

            // 루트 검증 실패 시 컴포넌트 비활성화 및 초기화 중단
            if (!_isRootObjectValid)
            {
                enabled = false;
                return;
            }

            // GlobalManagers 루트와 모든 자식 매니저 유지
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Awake에서 루트 검증에 실패한 경우 초기화 중단
            if (!_isRootObjectValid)
            {
                return;
            }

            // 모든 Awake() 콜백 완료 후 매니저 초기화 시작
            InitializeManagers();
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
                $"[GameBootstrapper] '{gameObject.name}'은(는) 루트 오브젝트가 아닙니다. " +
                $"현재 부모: '{transform.parent.name}'. GameBootstrapper를 GlobalManagers 루트 오브젝트에 부착해주세요.", this);

            return false;
        }

        // ==================== 2단계: 초기화 ====================

        /// <summary>
        /// 등록된 모든 매니저를 인스펙터 목록 순서대로 초기화합니다.
        /// 한 매니저라도 실패하면 전체 초기화 실패로 처리하고,
        /// 이미 초기화된 매니저는 역순으로 정리합니다.
        /// </summary>
        private void InitializeManagers()
        {
            ResetBootstrapperState();

            // 매니저 목록 검증 실패 시 초기화 중단
            if (!ValidateManagerList())
            {
                Debug.LogError("[GameBootstrapper] 매니저 목록 검증에 실패했습니다.", this);
                return;
            }

            // 등록된 매니저 순차 초기화
            bool hasErrors = ProcessManagerInitialization();

            // 초기화 결과에 따른 최종 상태 결정
            DetermineFinalState(hasErrors);
        }

        /// <summary>
        /// 이전 부트스트래퍼 상태를 초기화합니다.
        /// 현재 구조에서는 게임 시작 시 한 번만 호출합니다.
        /// </summary>
        private void ResetBootstrapperState()
        {
            IsInitializationComplete = false;
            _initializedManagers.Clear();
            _registeredManagers.Clear();
        }

        /// <summary>
        /// 인스펙터의 매니저 목록 존재 여부를 확인하고,
        /// 한 개 이상의 항목을 가지고 있는지 확인합니다.
        /// </summary>
        private bool ValidateManagerList()
        {
            if (_managerComponents == null || _managerComponents.Count == 0)
            {
                Debug.LogError("[GameBootstrapper] 초기화할 매니저가 등록되지 않았습니다.", this);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 등록된 매니저를 하나씩 검사하고 초기화합니다.
        /// 오류가 하나라도 발생하면 true를 반환합니다.
        /// </summary>
        private bool ProcessManagerInitialization()
        {
            bool hasErrors = false;

            for (int index = 0; index < _managerComponents.Count; index++)
            {
                MonoBehaviour component = _managerComponents[index];

                // 인스펙터 목록의 빈 항목 검사
                if (!ValidateComponent(component, index))
                {
                    hasErrors = true;
                    continue;
                }

                // IGameManager 구현 여부 검사
                if (!TryGetManagerInterface(
                        component,
                        out IGameManager manager))
                {
                    hasErrors = true;
                    continue;
                }

                // 동일 매니저의 중복 등록 검사
                if (!CheckAndRegisterManager(manager, component))
                {
                    continue;
                }

                // 실제 매니저 초기화 실행
                if (!InitializeManager(manager, component))
                {
                    hasErrors = true;
                }
            }

            return hasErrors;
        }

        /// <summary>
        /// 등록된 컴포넌트가 null이 아닌지 확인합니다.
        /// </summary>
        private bool ValidateComponent(
            MonoBehaviour component,
            int index)
        {
            if (component != null)
            {
                return true;
            }

            Debug.LogError($"[GameBootstrapper] 매니저 목록의 {index}번 항목이 비어 있습니다.", this);

            return false;
        }

        /// <summary>
        /// 컴포넌트가 IGameManager 인터페이스를 구현했는지 확인합니다.
        /// </summary>
        private bool TryGetManagerInterface(
            MonoBehaviour component,
            out IGameManager manager)
        {
            if (component is IGameManager validManager)
            {
                manager = validManager;

                return true;
            }

            Debug.LogError(
                $"[GameBootstrapper] '{component.name}' 오브젝트의 {component.GetType().Name} 컴포넌트가 " +
                "IGameManager를 구현하지 않았습니다.", component);

            manager = null;
            return false;
        }

        /// <summary>
        /// 매니저가 중복 등록되었는지 확인한 후 등록합니다.
        /// </summary>
        private bool CheckAndRegisterManager(
            IGameManager manager,
            MonoBehaviour component)
        {
            if (_registeredManagers.Add(manager))
            {
                return true;
            }

            Debug.LogWarning(
                $"[GameBootstrapper] {component.GetType().Name}이(가) " +
                "중복 등록되어 두 번째 항목을 건너뜁니다.", component);

            return false;
        }

        /// <summary>
        /// 매니저를 초기화합니다.
        /// 이미 초기화된 매니저라면 Initialize() 호출을 건너뛰고
        /// 종료 대상 목록에만 등록합니다.
        /// </summary>
        private bool InitializeManager(
            IGameManager manager,
            MonoBehaviour component)
        {
            try
            {
                // 이미 초기화된 매니저의 중복 초기화 방지
                if (manager.IsInitialized)
                {
                    Debug.LogWarning(
                        $"[GameBootstrapper] {component.GetType().Name}은(는) " +
                        "이미 초기화되어 있어 Initialize() 호출을 건너뜁니다.", component);

                    _initializedManagers.Add(manager);
                    return true;
                }

                // 매니저 초기화 작업 실행
                manager.Initialize();

                // Initialize() 실행 후 실제 초기화 상태 검사
                if (!manager.IsInitialized)
                {
                    Debug.LogError(
                        $"[GameBootstrapper] {component.GetType().Name}의 " +
                        "Initialize() 호출 후에도 IsInitialized가 false입니다.", component);

                    return false;
                }

                // 정상 초기화된 매니저를 종료 대상 목록에 등록
                _initializedManagers.Add(manager);

                Debug.Log($"[GameBootstrapper] {component.GetType().Name} 초기화 완료", component);

                return true;
            }
            catch (Exception exception)
            {
                // 오류 설명과 원본 스택 트레이스 개별 출력
                Debug.LogError($"[GameBootstrapper] {component.GetType().Name} 초기화 중 예외가 발생했습니다.", component);

                Debug.LogException(exception, component);

                return false;
            }
        }

        /// <summary>
        /// 초기화 결과에 따라 최종 상태를 결정합니다.
        /// 하나라도 실패했다면 앞서 초기화된 매니저를
        /// 초기화의 역순으로 정리합니다.
        /// </summary>
        private void DetermineFinalState(bool hasErrors)
        {
            IsInitializationComplete = !hasErrors && _initializedManagers.Count > 0;

            if (IsInitializationComplete)
            {
                Debug.Log( $"[GameBootstrapper] 모든 매니저 ({_initializedManagers.Count}개) 초기화 완료", this);

                return;
            }

            Debug.LogError(
                $"[GameBootstrapper] 초기화 실패 (완료: {_initializedManagers.Count}, 오류 발생: {hasErrors})", this);

            // 전체 초기화 실패 시 초기화된 매니저 역순 정리
            ShutdownManagers();
        }

        // ==================== 3단계: 정리 ====================

        /// <summary>
        /// 초기화된 매니저를 초기화의 역순으로 종료합니다.
        /// </summary>
        private void ShutdownManagers()
        {
            for (int index = _initializedManagers.Count - 1; index >= 0; index--)
            {
                IGameManager manager = _initializedManagers[index];

                try
                {
                    // 초기화된 상태인 매니저만 종료
                    if (!manager.IsInitialized)
                    {
                        continue;
                    }

                    manager.Shutdown();

                    Debug.Log($"[GameBootstrapper] {manager.GetType().Name} 종료");
                }
                catch (Exception exception)
                {
                    // 한 매니저의 종료에 실패해도
                    // 나머지 매니저의 종료 작업은 계속 진행합니다.
                    Debug.LogError($"[GameBootstrapper] {manager.GetType().Name} 종료 중 예외가 발생했습니다.");

                    Debug.LogException(exception);
                }
            }

            // 부트스트래퍼 내부 상태 초기화
            _initializedManagers.Clear();
            _registeredManagers.Clear();
            IsInitializationComplete = false;
        }

        /// <summary>
        /// GlobalManagers 오브젝트가 제거되거나
        /// 애플리케이션이 종료될 때 매니저를 역순으로 정리합니다.
        /// DontDestroyOnLoad가 적용되어 있으므로 일반적인 씬 전환에서는 호출되지 않습니다.
        /// </summary>
        private void OnDestroy()
        {
            ShutdownManagers();
        }
    }
}