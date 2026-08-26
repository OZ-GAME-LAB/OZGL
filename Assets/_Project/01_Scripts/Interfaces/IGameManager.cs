namespace OzGameLab01.Interfaces
{
    /// <summary>
    /// 부트스트래퍼가 매니저의 초기화와 종료를
    /// 동일한 방식으로 관리하기 위한 공통 규칙입니다.
    ///
    /// GameBootstrapper 또는 씬 전용 부트스트래퍼가
    /// 생명주기를 관리해야 하는 매니저만 구현합니다.
    /// </summary>
    public interface IGameManager
    {
        /// <summary>
        /// 매니저의 초기화가 정상적으로 완료되었는지 반환합니다.
        ///
        /// false: 아직 초기화되지 않았거나 초기화에 실패한 상태
        /// true: 초기화가 정상적으로 완료된 상태
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// 매니저가 기능을 사용하기 위해 필요한 준비 작업을 실행합니다.
        ///
        /// 예시:
        /// - 데이터 불러오기
        /// - 이벤트 구독
        /// - 오브젝트 풀 생성
        /// - 다른 매니저와의 연결 준비
        /// </summary>
        void Initialize();

        /// <summary>
        /// 매니저가 사용한 자원과 연결을 정리합니다.
        ///
        /// 예시:
        /// - 이벤트 구독 해제
        /// - 코루틴 정지
        /// - 임시 데이터 제거
        /// - 초기화 상태 초기화
        /// </summary>
        void Shutdown();
    }
}