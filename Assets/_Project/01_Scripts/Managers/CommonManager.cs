using System;
using OzGameLab01.Data;
using OzGameLab01.Interfaces;
using UnityEngine;

namespace OzGameLab01.Managers
{
    /// <summary>
    /// 게임 전체에서 공통으로 사용하는 기능을 관리하는 매니저입니다.
    ///
    /// #10 현재 단계에서는 GameBootstrapper의 동작을 검증하기 위해
    /// 초기화와 종료 기능만 최소한으로 구현합니다.
    /// 
    /// #19 현재 단계에서는 게임 상태 관리와 상태 변경 이벤트 기능을 추가합니다. 
    /// </summary>
    public class CommonManager : MonoBehaviour, IGameManager
    {
        [Header("게임 상태")]
        [Tooltip("현재 게임의 진행 상태입니다.")]
        [SerializeField] private GameState _currentState = GameState.Boot;

        /// <summary>
        /// CommonManager의 초기화 완료 여부입니다.
        ///
        /// 외부에서는 값을 확인할 수 있지만,
        /// 값의 변경은 CommonManager 내부에서만 가능합니다.
        /// </summary>
        public bool IsInitialized { get; private set; }

        // ==================== 게임 상태 관리 ====================

        /// <summary>
        /// 현재 게임 상태입니다.
        ///
        /// 외부에서는 현재 상태를 확인할 수 있지만,
        /// 상태 변경은 ChangeState()를 통해서만 가능합니다.
        /// </summary>
        public GameState CurrentState => _currentState;

        /// <summary>
        /// 게임 상태가 변경될 때 호출되는 이벤트입니다.
        ///
        /// 첫 번째 값으로 이전 상태를 전달하고,
        /// 두 번째 값으로 새로운 상태를 전달합니다.
        /// </summary>
        public event Action<GameState, GameState> GameStateChanged;

        /// <summary>
        /// CommonManager를 사용할 수 있도록 준비합니다.
        /// GameBootstrapper가 Inspector 등록 순서에 따라 호출합니다.
        /// </summary>
        public void Initialize()
        {
            // 중복 초기화 방지
            if (IsInitialized)
            {
                Debug.LogWarning("[CommonManager] 이미 초기화되어 있어 Initialize() 호출을 건너뜁니다.", this);

                return;
            }

            // 공용 기능 준비 작업 추가 예정

            // 초기 게임 상태 설정
            _currentState = GameState.Boot;

            // 모든 준비 작업 완료 후 초기화 상태로 변경
            IsInitialized = true;

            Debug.Log("[CommonManager] 초기화가 완료되었습니다.", this);
        }


        /// <summary>
        /// 현재 게임 상태를 새로운 상태로 변경합니다. 
        /// 상태 변경에 성공하면 true를 반환하고, 실패하면 false를 반환합니다.
        /// </summary>
        public bool ChangeState(GameState newState)
        {
            // 초기화 전 상태 변경 방지
            if (!IsInitialized)
            {
                Debug.LogError("[CommonManager] 초기화되지 않은 상태에서 ChangeState() 호출이 발생했습니다.", this);
                return false;
            }

            // 동일 상태 중복 변경 방지
            if (_currentState == newState)
            {
                Debug.LogWarning($"[CommonManager] 현재 상태가 이미 {newState}이므로 상태 변경을 건너뜁니다.", this);
                return false;
            }

            // 이벤트 전달을 위한 이전 상태 보관
            GameState previousState = _currentState;

            // 현재 게임 상태 변경
            _currentState = newState;

            // 이전 상태와 새로운 상태 전달
            GameStateChanged?.Invoke(previousState, _currentState);

            return true;
        }

        /// <summary>
        /// CommonManager가 사용한 상태와 이벤트 연결을 정리합니다.
        /// </summary>
        public void Shutdown()
        {
            // 초기화되지 않은 경우 종료 작업 생략
            if (!IsInitialized)
            {
                return;
            }

            // 이벤트 구독 해제 등 정리 작업 추가 예정

            // 상태 변경 이벤트 구독 정보 정리
            GameStateChanged = null;

            // 게임 상태 초기화
            _currentState = GameState.Boot;

            // 초기화 상태 해제
            IsInitialized = false;

            Debug.Log("[CommonManager] 종료 작업이 완료되었습니다.", this);
        }
    }
}