using UnityEngine;

namespace OzGameLab01.Combat
{
    public enum CombatFeedbackKind { Synergy, Skill, Relic, Passive }

    /// <summary>Presentation payload emitted only after an effect was applied (or a skill fired).</summary>
    public readonly struct CombatFeedback
    {
        public readonly CombatFeedbackKind Kind;
        public readonly string Source;
        public readonly string Detail;
        public readonly Unit Target;

        public CombatFeedback(CombatFeedbackKind kind, string source, string detail, Unit target)
        {
            Kind = kind;
            Source = source;
            Detail = detail;
            Target = target;
        }

        public string Category => Kind switch
        {
            CombatFeedbackKind.Synergy => "시너지",
            CombatFeedbackKind.Skill => "스킬",
            CombatFeedbackKind.Relic => "유물",
            _ => "패시브"
        };

        public Color Color => Kind switch
        {
            CombatFeedbackKind.Synergy => new Color(0.35f, 1f, 0.78f),
            CombatFeedbackKind.Skill => new Color(0.45f, 0.76f, 1f),
            CombatFeedbackKind.Relic => new Color(1f, 0.8f, 0.3f),
            _ => new Color(0.86f, 0.62f, 1f)
        };

        public static string StatText(EffectStatType stat, float value)
        {
            string label = stat switch
            {
                EffectStatType.MaxHealth => "최대 체력",
                EffectStatType.Attack => "공격력",
                EffectStatType.Defense => "방어력",
                EffectStatType.AttackInterval => "기본공격 쿨다운 감소",
                EffectStatType.CriticalChance => "치명타 확률",
                EffectStatType.CriticalMultiplier => "치명타 배율",
                EffectStatType.DodgeChance => "회피율",
                _ => stat.ToString()
            };
            return $"{label} {value:+0.##;-0.##;0}%";
        }
    }
}
