using OzGameLab01.Data;
using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.Combat;
using OzGameLab01.Effects.Models;
using OzGameLab01.Player;
using OzGameLab01.Rewards;
using OzGameLab01.Common;

namespace OzGameLab01.Managers
{
    public static class BattleRewardService
    {
        /// <summary>
        /// 전투 승리 시 지급하는 기본 보상입니다.
        /// 현재 기획은 선택지 없이 dropWeight 기반 유물 1개를 자동 지급합니다.
        /// </summary>
        public static RelicData ApplyAutomaticVictoryReward(Object context = null)
        {
            RelicFacade relicFacade = SystemBus.Get<RelicFacade>();
            if (relicFacade == null)
            {
                Debug.LogError("[BattleRewardService] 자동 전투 보상을 적용할 유물 서비스가 없습니다.", context);
                return null;
            }

            RelicData grantedRelic = relicFacade.AcquireRandomRelic();
            if (grantedRelic == null)
            {
                Debug.LogWarning("[BattleRewardService] 지급 가능한 유물이 없습니다.", context);
            }

            return grantedRelic;
        }

        public static bool Apply(
            BattleRewardData reward,
            IEnumerable<Unit> participatingUnits,
            Object context = null)
        {
            switch (reward.kind)
            {
                case BattleRewardKind.Experience:
                    return ApplyExperience(reward.amount, participatingUnits, context);
                case BattleRewardKind.Relic:
                    return ApplyRelic(reward.targetId, context);
                case BattleRewardKind.Unit:
                    return ApplyUnit(reward.targetId, context);
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
            RelicFacade relicFacade = SystemBus.Get<RelicFacade>();
            if (relicFacade == null || relicId < 0)
            {
                Debug.LogError($"[BattleRewardService] 유물 보상을 적용할 수 없습니다: {relicId}", context);
                return false;
            }

            relicFacade.AcquireRelic(relicId);
            return true;
        }

        private static bool ApplyUnit(int unitId, Object context)
        {
            PlayerFacade playerFacade = SystemBus.Get<PlayerFacade>();
            if (playerFacade == null)
            {
                Debug.LogError("[BattleRewardService] 유닛 보상을 적용할 인벤토리가 없습니다.", context);
                return false;
            }

            UnitData unit = OzGameLab01.Data.RuntimeContent.Catalog.GetUnit(unitId);
            if (unit == null)
            {
                Debug.LogError($"[BattleRewardService] 유닛 보상 ID를 찾을 수 없습니다: {unitId}", context);
                return false;
            }

            playerFacade.AddUnit(PlayerFacade.CloneUnitData(unit));
            return true;
        }
    }
}
