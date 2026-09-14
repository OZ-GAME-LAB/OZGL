using System;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// 패시브 스킬/유물이 구독할 전투 이벤트 발행처. TriggerType과 1:1로 대응하되,
    /// 지금 실제로 발행하는 것은 기존 Unit 메커니즘(사망/스킬 사용/공격 명중)에
    /// 자연스럽게 연결되는 이벤트뿐입니다. 회복/버프/보호막/부활처럼 아직 존재하지 않는
    /// 메커니즘에 대응하는 이벤트는 그 메커니즘이 실제로 생기기 전까지 추가하지 않습니다
    /// (Docs/PASSIVE_TRIGGER_EFFECT_SCHEMA.md 참고).
    /// </summary>
    public static class PassiveEventBus
    {
        public static event Action OnBattleStart;
        public static event Action<Unit> OnSelfDeath;
        public static event Action<Unit> OnAllyDeath;
        public static event Action<Unit, SkillData> OnAllySkillUsed;
        public static event Action<Unit, SkillData> OnEnemySkillUsed;
        public static event Action<Unit, Unit> OnAttackLanded;

        public static void RaiseBattleStart()
        {
            OnBattleStart?.Invoke();
        }

        public static void RaiseDeath(Unit unit)
        {
            if (unit == null)
            {
                return;
            }

            OnSelfDeath?.Invoke(unit);

            if (unit.TeamValue == Unit.Team.Ally)
            {
                OnAllyDeath?.Invoke(unit);
            }
        }

        public static void RaiseSkillUsed(Unit caster, SkillData skill)
        {
            if (caster == null)
            {
                return;
            }

            if (caster.TeamValue == Unit.Team.Ally)
            {
                OnAllySkillUsed?.Invoke(caster, skill);
            }
            else
            {
                OnEnemySkillUsed?.Invoke(caster, skill);
            }
        }

        public static void RaiseAttackLanded(Unit attacker, Unit target)
        {
            OnAttackLanded?.Invoke(attacker, target);
        }

        /// <summary>
        /// 전투 세션 종료 시 이전 구독자가 남아있지 않도록 비웁니다.
        /// </summary>
        public static void ResetRunState()
        {
            OnBattleStart = null;
            OnSelfDeath = null;
            OnAllyDeath = null;
            OnAllySkillUsed = null;
            OnEnemySkillUsed = null;
            OnAttackLanded = null;
        }
    }
}
