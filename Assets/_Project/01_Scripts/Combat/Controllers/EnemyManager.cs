using OzGameLab01.Combat;

namespace OzGameLab01.Managers
{
    /// <summary>
    /// 전투 씬 진입 시 적 유닛의 최종 스펙(턴/낮밤/중간보스 상태로 스케일링한 체력 +
    /// 플레이어 보유 유닛에서 무작위로 훔친 액티브 스킬 2개)을 확정해 Unit에 넘깁니다.
    /// 실제 쿨타임 진행·현재 체력 같은 전투 런타임 상태는 계속 Unit/CombatManager가
    /// 소유합니다 — 이 매니저는 전투 시작 전 스펙 확정만 담당합니다. 계산 규칙 자체는
    /// EnemyCombatSpecModel이, 조회 라우팅은 EnemyFacade가 갖고 있고, 이 클래스는 부팅
    /// 시부터 계속 살아있는 Facade 진입점 노출 외에는 아무 일도 하지 않습니다.
    /// </summary>
    public sealed class EnemyManager : Singleton<EnemyManager>
    {
        private EnemyFacade _facade;
        public EnemyFacade Facade => _facade ??= new EnemyFacade();
    }
}
