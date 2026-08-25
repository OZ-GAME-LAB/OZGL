using OzGameLab01.Interfaces;
using UnityEngine;

namespace OzGameLab01.Managers
{
    /// <summary>
    /// 게임 전체에서 공통으로 사용하는 기능을 관리하는 매니저입니다.
    ///
    /// 현재 단계에서는 GameBootstrapper의 동작을 검증하기 위해
    /// 초기화와 종료 기능만 최소한으로 구현합니다.
    /// </summary>
    public class CommonManager : MonoBehaviour, IGameManager
    {
        /// <summary>
        /// CommonManager의 초기화 완료 여부입니다.
        ///
        /// 외부에서는 값을 확인할 수 있지만,
        /// 값의 변경은 CommonManager 내부에서만 가능합니다.
        /// </summary>
        public bool IsInitialized { get; private set; }

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

            // 모든 준비 작업 완료 후 초기화 상태로 변경
            IsInitialized = true;

            //Debug.Log("[CommonManager] 초기화가 완료되었습니다.", this);
        }

        public void Shutdown()
        {
            // 초기화되지 않은 경우 종료 작업 생략
            if (!IsInitialized)
            {
                return;
            }

            // 이벤트 구독 해제 등 정리 작업 추가 예정

            // 초기화 상태 해제
            IsInitialized = false;

            //Debug.Log("[CommonManager] 종료 작업이 완료되었습니다.", this);
        }
    }
}