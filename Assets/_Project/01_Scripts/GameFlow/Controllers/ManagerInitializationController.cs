using System;
using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.Interfaces;
namespace OzGameLab01.GameFlow.Controllers
{
    /// <summary>등록 매니저 사전 검증·순차 초기화·역순 종료</summary>
    public sealed class ManagerInitializationController
    {
        private readonly List<MonoBehaviour> _managerComponents;
        private readonly UnityEngine.Object _context;
        private readonly List<IGameManager> _initializedManagers = new();
        private readonly HashSet<IGameManager> _registeredManagers = new();
        private readonly List<MonoBehaviour> _validatedManagerComponents = new();
        private int _readyManagerCount;
        public bool IsInitializationComplete { get; private set; }
        public ManagerInitializationController(List<MonoBehaviour> components, UnityEngine.Object context) { _managerComponents = components; _context = context; }
        public void Initialize()
        {
            // ==================== 리팩터링 추가 ====================

            // 초기화 완료 후 중복 호출 방지
            if (IsInitializationComplete)
            {
                Debug.LogWarning(
                    "[GameBootstrapper] 모든 매니저가 이미 준비되어 있어 " +
                    "InitializeManagers() 호출을 건너뜁니다.", _context);

                return;
            }

            ResetBootstrapperState();

            // 매니저 목록 존재 여부 검사
            if (!ValidateManagerList())
            {
                Debug.LogError(
                    "[GameBootstrapper] 매니저 목록 검증에 실패했습니다.", _context);

                return;
            }

            // 모든 매니저 항목을 먼저 검증
            // 하나라도 잘못된 항목이 있으면 초기화를 시작하지 않음
            if (!ValidateAllManagerComponents())
            {
                Debug.LogError(
                    "[GameBootstrapper] 매니저 전체 사전 검증에 실패하여 " +
                    "초기화를 시작하지 않습니다.", _context);

                // 사전 검증 과정에서 수집한 임시 정보 정리
                ResetBootstrapperState();
                return;
            }

            // 사전 검증을 통과한 매니저만 순차 초기화
            bool hasErrors = ProcessManagerInitialization();

            // 초기화 결과에 따른 최종 상태 결정
            DetermineFinalState(hasErrors);

        }
        private void ResetBootstrapperState()
        {
            IsInitializationComplete = false;
            _initializedManagers.Clear();
            _readyManagerCount = 0;
            _registeredManagers.Clear();
            _validatedManagerComponents.Clear();
        }

        /// <summary>
        /// 인스펙터의 매니저 목록 존재 여부를 확인하고,
        /// 한 개 이상의 항목을 가지고 있는지 확인합니다.
        /// </summary>
        private bool ValidateManagerList()
        {
            if (_managerComponents == null ||
                _managerComponents.Count == 0)
            {
                Debug.LogError(
                    "[GameBootstrapper] 초기화할 매니저가 등록되지 않았습니다.", _context);

                return false;
            }

            return true;
        }

        /// <summary>
        /// 인스펙터에 등록된 모든 매니저 항목을 초기화 전에 검증합니다.
        ///
        /// null, IGameManager 미구현, 중복 등록 항목을 검사하고
        /// 검증을 통과한 컴포넌트를 별도 목록에 보관합니다.
        /// </summary>
        private bool ValidateAllManagerComponents()
        {
            bool hasErrors = false;

            for (int index = 0; index < _managerComponents.Count; index++)
            {
                MonoBehaviour component = _managerComponents[index];

                // null 항목 검사
                if (!ValidateComponent(component, index))
                {
                    hasErrors = true;
                    continue;
                }

                // IGameManager 구현 여부 검사
                if (!TryGetManagerInterface(component, out IGameManager manager))
                {
                    hasErrors = true;
                    continue;
                }

                // 동일 매니저의 중복 등록 검사
                if (!CheckAndRegisterManager(manager, component))
                {
                    hasErrors = true;
                    continue;
                }

                // 모든 검증을 통과한 컴포넌트 보관
                _validatedManagerComponents.Add(component);
            }

            return !hasErrors && _validatedManagerComponents.Count > 0;
        }

        /// <summary>
        /// 전체 사전 검증을 통과한 매니저를 순서대로 초기화합니다.
        ///
        /// 한 매니저라도 초기화에 실패하면
        /// 뒤쪽 매니저는 초기화하지 않고 즉시 중단합니다.
        /// </summary>
        private bool ProcessManagerInitialization()
        {
            foreach (MonoBehaviour component in _validatedManagerComponents)
            {
                // 사전 검증에서 IGameManager 구현을 확인했으므로 변환 가능
                IGameManager manager = (IGameManager)component;

                // 초기화 실패 시 나머지 초기화를 진행하지 않음
                if (!InitializeManager(manager, component))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 등록된 컴포넌트가 null이 아닌지 확인합니다.
        /// </summary>
        private bool ValidateComponent(MonoBehaviour component, int index)
        {
            if (component != null)
            {
                return true;
            }

            Debug.LogError($"[GameBootstrapper] 매니저 목록의 {index}번 항목이 비어 있습니다.", _context);

            return false;
        }

        /// <summary>
        /// 컴포넌트가 IGameManager 인터페이스를
        /// 구현했는지 확인합니다.
        /// </summary>
        private bool TryGetManagerInterface(MonoBehaviour component, out IGameManager manager)
        {
            if (component is IGameManager validManager)
            {
                manager = validManager;
                return true;
            }

            Debug.LogError(
                $"[GameBootstrapper] '{component.name}' 오브젝트의 " +
                $"{component.GetType().Name} 컴포넌트가 IGameManager를 구현하지 않았습니다.", component);

            manager = null;
            return false;
        }

        /// <summary>
        /// 매니저가 중복 등록되었는지 확인한 후 등록합니다.
        /// 중복 등록된 경우 전체 사전 검증 실패로 처리합니다.
        /// </summary>
        private bool CheckAndRegisterManager(IGameManager manager, MonoBehaviour component)
        {
            if (_registeredManagers.Add(manager))
            {
                return true;
            }

            Debug.LogError(
                $"[GameBootstrapper] {component.GetType().Name}이(가) " +
                "초기화 대상 목록에 중복 등록되어 있습니다.", component);

            return false;
        }

        /// <summary>
        /// 매니저의 준비 상태를 확인하고 필요한 경우 초기화합니다.
        ///
        /// 이미 초기화된 매니저는 준비 완료로만 기록하고,
        /// GameBootstrapper가 직접 초기화한 매니저만 종료 대상에 등록합니다.
        /// </summary>
        private bool InitializeManager(IGameManager manager, MonoBehaviour component)
        {
            try
            {
                // 이미 초기화된 매니저의 중복 초기화 방지
                if (manager.IsInitialized)
                {
                    // 이미 초기화된 매니저는 준비 완료 수에만 포함
                    // 직접 초기화하지 않았으므로 종료 목록에는 추가하지 않음
                    _readyManagerCount++;

                    Debug.Log(
                        $"[GameBootstrapper] 매니저 준비 확인 | {component.GetType().Name} " +
                        $"(기존 초기화 상태, Initialize 생략)", component);

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

                // 부트스트래퍼가 직접 초기화한 매니저만 종료 대상에 등록
                _initializedManagers.Add(manager);

                // 정상적으로 사용할 수 있으므로 준비 완료 수 증가
                _readyManagerCount++;

                Debug.Log($"[GameBootstrapper] 매니저 초기화 완료 | {component.GetType().Name}", component);

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[GameBootstrapper] {component.GetType().Name} 초기화 중 예외가 발생했습니다.", component);

                Debug.LogException(exception, component);

                return false;
            }
        }

        /// <summary>
        /// 초기화 결과에 따라 최종 상태를 결정합니다.
        ///
        /// 준비가 확인된 모든 매니저가 정상 상태이고 오류가 없다면
        /// 전체 초기화 완료로 처리합니다.
        ///
        /// 하나라도 실패했다면 GameBootstrapper가 직접 초기화한
        /// 매니저만 초기화의 역순으로 정리합니다.
        /// </summary>
        private void DetermineFinalState(bool hasErrors)
        {
            bool areAllManagersReady =
                _readyManagerCount > 0 &&
                _readyManagerCount == _registeredManagers.Count;

            IsInitializationComplete =
                !hasErrors && areAllManagersReady;

            if (IsInitializationComplete)
            {
                Debug.Log(
                    $"[GameBootstrapper] 전체 매니저 준비 완료 | 등록: {_registeredManagers.Count}, " +
                    $"직접 초기화: {_initializedManagers.Count}", _context);

                return;
            }

            Debug.LogError(
                $"[GameBootstrapper] 초기화 실패 | 준비 완료: {_readyManagerCount}, " +
                $"직접 초기화: {_initializedManagers.Count}, 등록 확인: {_registeredManagers.Count}, " +
                $"오류 발생: {hasErrors}", _context);

            // 전체 초기화 실패 시GameBootstrapper가 직접 초기화한 매니저만 역순 정리
            ShutdownManagers();
        }

        // ==================== 3단계: 정리 ====================

        /// <summary>
        /// GameBootstrapper가 직접 초기화한 매니저를
        /// 초기화의 역순으로 종료합니다.
        /// </summary>
        public void ShutdownManagers()
        {
            for (int index = _initializedManagers.Count - 1; index >= 0; index--)
            {
                IGameManager manager = _initializedManagers[index];

                try
                {
                    // 이미 종료된 매니저는 다시 종료하지 않음
                    if (!manager.IsInitialized)
                    {
                        continue;
                    }

                    manager.Shutdown();

                    Debug.Log($"[GameBootstrapper] 매니저 종료 완료 | {manager.GetType().Name}", _context);
                }
                catch (Exception exception)
                {
                    // 한 매니저의 종료에 실패해도
                    // 나머지 매니저의 종료 작업은 계속 진행
                    Debug.LogError($"[GameBootstrapper] {manager.GetType().Name} 종료 중 예외가 발생했습니다.", _context);

                    Debug.LogException(exception, _context);
                }
            }

            // 종료 대상 및 내부 추적 상태 정리
            ResetBootstrapperState();
        }

    }
}
