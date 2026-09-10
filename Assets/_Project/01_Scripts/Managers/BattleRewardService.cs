using OzGameLab01.Data;
using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.Combat;

namespace OzGameLab01.Managers
{
    public static class BattleRewardService
    {
        public static bool Apply(
            BattleRewardData reward,
            IEnumerable<Unit> participatingUnits,
            UnitRosterData rosterData,
            Object context = null)
        {
            switch (reward.kind)
            {
                case BattleRewardKind.Experience:
                    return ApplyExperience(reward.amount, participatingUnits, context);
                case BattleRewardKind.Relic:
                    return ApplyRelic(reward.targetId, context);
                case BattleRewardKind.Unit:
                    return ApplyUnit(reward.targetId, rosterData, context);
                default:
                    Debug.LogError($"[BattleRewardService] 지원하지 않는 보상 유형입니다: {reward.kind}", context);
                    return false;
            }
        }

        private static bool ApplyExperience(float amount, IEnumerable<Unit> units, Object context)
        {
            if (amount <= 0f || units == null)
            {
                Debug.LogError("[BattleRewardService] 경험치 보상 값 또는 대상 유닛이 올바르지 않습니다.", context);
                return false;
            }

            HashSet<Unit.SkillType> rewardedTypes = new HashSet<Unit.SkillType>();
            foreach (Unit unit in units)
            {
                if (unit != null && rewardedTypes.Add(unit.Skill))
                {
                    SceneTransitioner.AddExp(unit.Skill, amount);
                }
            }

            return rewardedTypes.Count > 0;
        }

        private static bool ApplyRelic(int relicId, Object context)
        {
            if (RelicManager.Instance == null || relicId < 0)
            {
                Debug.LogError($"[BattleRewardService] 유물 보상을 적용할 수 없습니다: {relicId}", context);
                return false;
            }

            RelicManager.Instance.AcquireRelic(relicId);
            return true;
        }

        private static bool ApplyUnit(int unitId, UnitRosterData rosterData, Object context)
        {
            if (rosterData == null || PlayerInventoryManager.Instance == null)
            {
                Debug.LogError("[BattleRewardService] 유닛 보상을 적용할 로스터 또는 인벤토리가 없습니다.", context);
                return false;
            }

            foreach (UnitData unit in rosterData.UnitStats)
            {
                if (unit != null && unit.id == unitId)
                {
                    PlayerInventoryManager.Instance.AddUnit(PlayerInventoryManager.CloneUnitData(unit));
                    return true;
                }
            }

            Debug.LogError($"[BattleRewardService] 유닛 보상 ID를 찾을 수 없습니다: {unitId}", context);
            return false;
        }
    }
}
