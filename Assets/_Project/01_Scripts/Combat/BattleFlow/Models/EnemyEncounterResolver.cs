using OzGameLab01.Controllers;
using OzGameLab01.Data;
using OzGameLab01.Managers;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// 보드 전투 종류에 따라 사용할 적 데이터를 한 곳에서 결정합니다.
    /// 보드의 전투 정보 화면과 실제 전투 씬이 같은 규칙을 공유합니다.
    /// </summary>
    public static class EnemyEncounterResolver
    {
        private const int NormalEnemyMonsterId = 1;
        private const int NightEnemyMonsterId = 2;
        private const int SemibossEnemyMonsterId = 3;
        private const int FinalBossEnemyMonsterId = 4;

        /// <summary>
        /// 현재 BoardRunData에 기록된 전투 조건으로 최종 전투 스펙을 반환합니다.
        /// </summary>
        public static MonsterData ResolveCurrentEncounter()
        {
            return ResolvePreparedEnemy(
                BoardRunData.IsBossBattle,
                BoardRunData.IsEliteBattle,
                BoardRunData.IsNightEncounter);
        }

        /// <summary>
        /// 아직 BoardRunData.BeginBattle을 호출하기 전인 미리보기 단계에서 사용할
        /// 최종 전투 스펙을 반환합니다.
        /// </summary>
        public static MonsterData ResolvePreparedEnemy(
            bool isBoss,
            bool isElite,
            bool isNightEncounter)
        {
            int monsterId = ResolveMonsterId(isBoss, isElite, isNightEncounter);
            MonsterData baseData = RuntimeContent.Catalog.GetEnemy(monsterId);

            return baseData != null
                ? EnemyManager.Instance.Facade.BuildCombatSpec(baseData)
                : null;
        }

        public static int ResolveMonsterId(
            bool isBoss,
            bool isElite,
            bool isNightEncounter)
        {
            // 최종 보스는 finalbossEnemy 시트에서 생성된 고정 데이터(ID 4)를,
            // 중간 보스는 semiboss 데이터(ID 3)를 사용합니다.
            if (isBoss)
            {
                return FinalBossEnemyMonsterId;
            }

            if (isElite)
            {
                return SemibossEnemyMonsterId;
            }

            return isNightEncounter
                ? NightEnemyMonsterId
                : NormalEnemyMonsterId;
        }
    }
}
