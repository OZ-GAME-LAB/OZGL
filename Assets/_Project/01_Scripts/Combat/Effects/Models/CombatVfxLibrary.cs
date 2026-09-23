using UnityEngine;
using OzGameLab01.Data;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// 유닛(아군/적 공통)에게 재생되는 힐/상태이상/스탯 버프·디버프 VFX를 한 곳에 모아둔
    /// 공용 라이브러리입니다. 적/아군 프리팹마다 중복 배선하지 않고 Resources에서 한 번만
    /// 불러와 정적으로 캐시합니다. 상태이상(기절/침묵/도트)은 지속 재생(적용 시 생성, 해제
    /// 시 파괴)하고, 스탯 버프/디버프는 적용 순간 1회성으로만 재생합니다(UnitCombatStats에
    /// 개별 모디파이어 식별자가 없어 정확한 만료 시점을 추적할 수 없기 때문).
    /// </summary>
    [CreateAssetMenu(fileName = "CombatVfxLibrary", menuName = "OzGameLab01/Combat/VFX Library")]
    public sealed class CombatVfxLibrary : ScriptableObject
    {
        [Header("힐")]
        [SerializeField] private GameObject healEffect;

        [Header("상태이상 (지속 재생)")]
        [SerializeField] private GameObject stunEffect;
        [SerializeField] private GameObject silenceEffect;
        [SerializeField] private GameObject damageOverTimeEffect;

        [Header("스탯 버프 (1회성)")]
        [SerializeField] private GameObject attackUpEffect;
        [SerializeField] private GameObject defenseUpEffect;
        [SerializeField] private GameObject criticalChanceUpEffect;
        [SerializeField] private GameObject criticalDamageUpEffect;
        [SerializeField] private GameObject dodgeUpEffect;
        [SerializeField] private GameObject attackSpeedUpEffect;

        [Header("스탯 디버프 (1회성)")]
        [SerializeField] private GameObject attackDownEffect;
        [SerializeField] private GameObject defenseDownEffect;
        [SerializeField] private GameObject criticalChanceDownEffect;
        [SerializeField] private GameObject criticalDamageDownEffect;
        [SerializeField] private GameObject dodgeDownEffect;
        [SerializeField] private GameObject attackSpeedDownEffect;

        private const string ResourcePath = "CombatVfxLibrary";
        private static CombatVfxLibrary _instance;
        private static bool _loaded;

        public static CombatVfxLibrary Instance
        {
            get
            {
                if (!_loaded)
                {
                    _instance = Resources.Load<CombatVfxLibrary>(ResourcePath);
                    _loaded = true;
                }
                return _instance;
            }
        }

        public GameObject HealEffect => healEffect;

        public GameObject GetDebuffEffect(DebuffType type)
        {
            switch (type)
            {
                case DebuffType.Stun: return stunEffect;
                case DebuffType.Silence: return silenceEffect;
                case DebuffType.DamageOverTime: return damageOverTimeEffect;
                default: return null;
            }
        }

        public GameObject GetStatEffect(EffectStatType stat, bool isBuff)
        {
            switch (stat)
            {
                case EffectStatType.Attack: return isBuff ? attackUpEffect : attackDownEffect;
                case EffectStatType.Defense: return isBuff ? defenseUpEffect : defenseDownEffect;
                case EffectStatType.CriticalChance: return isBuff ? criticalChanceUpEffect : criticalChanceDownEffect;
                case EffectStatType.CriticalMultiplier: return isBuff ? criticalDamageUpEffect : criticalDamageDownEffect;
                case EffectStatType.DodgeChance: return isBuff ? dodgeUpEffect : dodgeDownEffect;
                case EffectStatType.AttackInterval: return isBuff ? attackSpeedUpEffect : attackSpeedDownEffect;
                default: return null;
            }
        }
    }
}
