using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.Managers;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// Combat 시스템 외부(Unit/CombatUIController 등)와 내부 협력 클래스
    /// (CombatEffectExecutor 등)가 공통으로 호출하는 유일한 진입점입니다.
    /// 이 클래스는 게임 부팅 후 계속 살아있는 CombatManager가 들고 있는 반면, 실제
    /// 상태는 씬에 배치된 CombatSession이 갖고 있으므로 필요할 때마다 찾아 사용합니다.
    /// </summary>
    public class CombatFacade
    {
        private CombatSession _session;

        public Unit EnemyUnit => GetSession()?.State.EnemyUnit;

        private CombatSession GetSession()
        {
            if (_session == null)
            {
                _session = Object.FindFirstObjectByType<CombatSession>(FindObjectsInactive.Include);
            }

            return _session;
        }

        public void ReportFeedback(CombatFeedback feedback)
        {
            GetSession()?.ReportFeedback(feedback);
        }

        public Unit ResolveAllyTarget() => GetSession()?.State.ResolveAllyTarget();

        public List<Unit> GetParticipatingAllyUnits() => GetSession()?.State.GetParticipatingAllyUnits() ?? new List<Unit>();

        /// <summary>
        /// 특정 행(front/mid/back)에 살아있는 아군만 반환합니다. CombatEffectExecutor의
        /// EffectTarget.FrontRow/MidRow/BackRow 해석에 사용합니다.
        /// </summary>
        public List<Unit> GetAliveAlliesInRow(CombatManager.SlotRow row) => GetSession()?.State.GetAliveAlliesInRow(row) ?? new List<Unit>();

        /// <summary>
        /// 유닛 id(GameDB 기준)로 현재 전투에 스폰된 아군 Unit을 찾습니다. 패시브 효과의
        /// Self 타겟(효과를 보유한 유닛 자신)을 해석할 때 사용합니다 — 소유는 하고 있지만
        /// 이번 전투 편성에는 없는 유닛이면 null을 반환합니다.
        /// </summary>
        public Unit GetAllyUnitById(int unitId) => GetSession()?.State.GetAllyUnitById(unitId);
    }
}
