using System.Collections.Generic;
using OzGameLab01.UI.Battle;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// Combat 시스템 외부(Unit/CombatUIController 등)와 내부 협력 클래스
    /// (CombatEffectExecutor 등)가 공통으로 호출하는 유일한 진입점입니다.
    /// 실제 상태는 <see cref="CombatState"/>가 들고 있고, 이 클래스는 조회/명령을
    /// 위임만 합니다.
    /// </summary>
    public class CombatFacade
    {
        private readonly CombatState _state;
        private readonly CombatEffectFeedbackView _feedbackView;

        public Unit EnemyUnit => _state.EnemyUnit;

        public CombatFacade(CombatState state, CombatEffectFeedbackView feedbackView)
        {
            _state = state;
            _feedbackView = feedbackView;
        }

        public void ReportFeedback(CombatFeedback feedback)
        {
            if (_feedbackView != null) _feedbackView.Show(feedback);
        }

        public Unit ResolveAllyTarget() => _state.ResolveAllyTarget();

        public List<Unit> GetParticipatingAllyUnits() => _state.GetParticipatingAllyUnits();

        /// <summary>
        /// 특정 행(front/mid/back)에 살아있는 아군만 반환합니다. CombatEffectExecutor의
        /// EffectTarget.FrontRow/MidRow/BackRow 해석에 사용합니다.
        /// </summary>
        public List<Unit> GetAliveAlliesInRow(CombatManager.SlotRow row) => _state.GetAliveAlliesInRow(row);

        /// <summary>
        /// 유닛 id(GameDB 기준)로 현재 전투에 스폰된 아군 Unit을 찾습니다. 패시브 효과의
        /// Self 타겟(효과를 보유한 유닛 자신)을 해석할 때 사용합니다 — 소유는 하고 있지만
        /// 이번 전투 편성에는 없는 유닛이면 null을 반환합니다.
        /// </summary>
        public Unit GetAllyUnitById(int unitId) => _state.GetAllyUnitById(unitId);
    }
}
