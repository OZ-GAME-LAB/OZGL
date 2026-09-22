using OzGameLab01.Data;
using UnityEngine;
using OzGameLab01.Effects.Models;
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
    }
}
