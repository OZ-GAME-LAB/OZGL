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
            // 클래스별 레벨업 기능 폐지로 더 이상 지급하지 않습니다. BattleRewardKind는
            // 기존 보상 asset의 직렬화된 값을 밀리지 않도록 값 자체는 유지합니다.
            Debug.LogWarning("[BattleRewardService] 경험치 보상은 더 이상 지원하지 않습니다.", context);
            return false;
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
