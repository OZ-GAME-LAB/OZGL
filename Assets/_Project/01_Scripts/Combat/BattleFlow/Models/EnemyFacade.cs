using OzGameLab01.Data;
using OzGameLab01.Player;
using OzGameLab01.Common;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// EnemyManager가 노출하는 유일한 진입점입니다. 실제 계산은 EnemyPreparationCache가
    /// 전담하고, 이 클래스는 플레이어 보유 유닛 조회를 SystemBus 경유로 연결하는 얇은
    /// 어댑터 역할만 합니다.
    /// </summary>
    public sealed class EnemyFacade
    {
        private readonly EnemyPreparationCache _cache = new EnemyPreparationCache();

        public int PreparedCacheCount => _cache.Count;

        public MonsterData BuildCombatSpec(MonsterData baseData)
        {
            var ownedUnits = SystemBus.Get<PlayerFacade>()?.OwnedUnits;
            RuntimeContentService content = RuntimeContent.Service;
            return _cache.Prepare(content.Revision, baseData,
                BoardRunData.TurnCount, BoardRunData.DefeatedElitesCount,
                BoardRunData.MapSeed, ownedUnits);
        }

        public void ClearPreparedCache() => _cache.Clear();
    }
}
