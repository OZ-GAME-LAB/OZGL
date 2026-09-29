using OzGameLab01.Data;

namespace OzGameLab01.Effects.Models
{
    /// <summary>효과 발동 조건 판정에 필요한 전투 상황값입니다.</summary>
    public readonly struct EffectConditionContext
    {
        public int AllyCount { get; }
        public int FrontRowCount { get; }
        public int ActiveSynergyCount { get; }
        public bool IsNightBattle { get; }

        public EffectConditionContext(int allyCount, int frontRowCount, int activeSynergyCount, bool isNightBattle)
        {
            AllyCount = allyCount;
            FrontRowCount = frontRowCount;
            ActiveSynergyCount = activeSynergyCount;
            IsNightBattle = isNightBattle;
        }
    }

    /// <summary>
    /// EffectInstance.condition을 판정합니다. 전투 실행기와 시너지 단계 계산이 같은 규칙을 공유합니다.
    /// </summary>
    public static class EffectConditionEvaluator
    {
        public static bool IsMet(EffectInstance effect, EffectConditionContext context)
        {
            int param = UnityEngine.Mathf.RoundToInt(effect.conditionParam);
            return effect.condition switch
            {
                EffectCondition.AllyCountAtMost => context.AllyCount <= param,
                EffectCondition.FrontRowCountEquals => context.FrontRowCount == param,
                EffectCondition.ActiveSynergyCountAtLeast => context.ActiveSynergyCount >= param,
                EffectCondition.ActiveSynergyCountEquals => context.ActiveSynergyCount == param,
                EffectCondition.NotNightBattle => !context.IsNightBattle,
                _ => true
            };
        }
    }
}
