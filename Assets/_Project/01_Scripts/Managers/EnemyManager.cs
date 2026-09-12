using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.Data;

namespace OzGameLab01.Managers
{
    /// <summary>
    /// 전투 씬 진입 시 적 유닛의 최종 스펙(턴/낮밤/중간보스 상태로 스케일링한 체력 +
    /// 플레이어 보유 유닛에서 무작위로 훔친 액티브 스킬 2개)을 확정해 Unit에 넘깁니다.
    /// 실제 쿨타임 진행·현재 체력 같은 전투 런타임 상태는 계속 Unit/CombatManager가
    /// 소유합니다 — 이 매니저는 전투 시작 전 스펙 확정만 담당합니다.
    /// </summary>
    public sealed class EnemyManager : Singleton<EnemyManager>
    {
        // BoardSceneController._nightInterval(02_MainGame.unity)과 반드시 같은 값이어야 합니다.
        private const int NightInterval = 3;

        // 한 유닛당 액티브 스킬은 skillIds[1]입니다(0번째는 공용 기본공격). TempRosterSeed 참고.
        private const int ActiveSkillIndex = 1;
        private const int StealCount = 2;

        /// <summary>
        /// baseData(로스터/JSON 원본, 여러 스폰에서 공유하는 캐시)는 수정하지 않고 복제해,
        /// 현재 턴/낮밤/중간보스 상태로 스케일링한 체력과 훔친 액티브 스킬을 채운 전투용
        /// 스펙을 반환합니다. 훔칠 액티브 스킬은 서로 다른 유닛에서 1개씩 최대 2개까지 뽑고,
        /// 후보가 부족하면 그만큼만(0~1개) 채우고 나머지는 기본공격만 남습니다.
        /// </summary>
        public MonsterData BuildCombatSpec(MonsterData baseData)
        {
            if (baseData == null)
            {
                return null;
            }

            float multiplier = EnemyValueResolver.GetHealthMultiplier(
                BoardRunData.TurnCount, NightInterval, BoardRunData.DefeatedElitesCount);

            return new MonsterData
            {
                id = baseData.id,
                name = baseData.name,
                spriteAddress = baseData.spriteAddress,
                healthPoint = Mathf.RoundToInt(baseData.healthPoint * multiplier),
                attackPoint = baseData.attackPoint,
                criticalRate = baseData.criticalRate,
                dodgeRate = baseData.dodgeRate,
                attackSpeed = baseData.attackSpeed,
                skillCooldown = baseData.skillCooldown,
                type = baseData.type,
                skillIds = BuildSkillIds(baseData.skillIds),
            };
        }

        private List<int> BuildSkillIds(List<int> baseSkillIds)
        {
            List<int> result = new List<int>();
            if (baseSkillIds != null && baseSkillIds.Count > 0)
            {
                result.Add(baseSkillIds[0]); // 공용 기본공격
            }

            result.AddRange(StealActiveSkillIds());
            return result;
        }

        /// <summary>
        /// 플레이어 보유 유닛 중 액티브 스킬(skillIds[1])을 가진 유닛을 무작위로 섞어
        /// 서로 다른 유닛에서 최대 StealCount개까지 뽑습니다.
        /// </summary>
        private List<int> StealActiveSkillIds()
        {
            List<int> stolen = new List<int>();

            PlayerInventoryManager inventory = FindAnyObjectByType<PlayerInventoryManager>();
            if (inventory == null)
            {
                return stolen;
            }

            List<int> candidates = new List<int>();
            foreach (UnitData unit in inventory.OwnedUnits)
            {
                if (unit?.skillIds != null && unit.skillIds.Count > ActiveSkillIndex)
                {
                    candidates.Add(unit.skillIds[ActiveSkillIndex]);
                }
            }

            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }

            int count = Mathf.Min(StealCount, candidates.Count);
            for (int i = 0; i < count; i++)
            {
                stolen.Add(candidates[i]);
            }

            return stolen;
        }
    }
}
