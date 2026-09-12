using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OzGameLab01.Managers;
using OzGameLab01.Data;

namespace OzGameLab01.Combat
{
    public class Unit : MonoBehaviour
    {
        public enum Team { Ally, Enemy }

        /// <summary>
        /// SkillData(공용 정의) + 이 유닛 인스턴스만의 쿨다운 타이머/레벨 배율을 묶은 런타임 상태.
        /// SkillData는 로스터에 공유되는 데이터라 직접 변경하지 않고, 배율은 여기 별도로 둡니다.
        /// </summary>
        private sealed class RuntimeSkill
        {
            public SkillData data;
            public float timer;
            public float damageMultiplier = 1f;

            // 기본공격(0번째)은 유닛마다 다른 공격속도를 가지므로, 여러 유닛이 공유하는
            // SkillData.cooldown 대신 이 값을 우선 사용합니다. 그 외 스킬은 null로 두어
            // SkillData.cooldown을 그대로 씁니다.
            public float? cooldownOverride;
        }

        [SerializeField] private Team team;
        [SerializeField] private float maxHP = 100f;
        private float attackPoint;
        private float defensePoint;
        private float criticalRate;
        private float criticalMult = 150f;
        private float dodgeRate;
        [SerializeField] private HealthBar healthBar;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private GameObject projectilePrefab;

        [Header("Skill (0번째 = 기본공격, 침묵 면역. 각자 쿨다운마다 자동 발동)")]
        [SerializeField] private TMPro.TextMeshPro skillNameLabel;
        [SerializeField] private float skillNameDisplayDuration = 0.35f;

        private readonly List<RuntimeSkill> _skills = new List<RuntimeSkill>();

        public bool IsDead => _isDead;
        public Team TeamValue => team;
        public float CurrentHp => _currentHP;
        public float MaxHp => maxHP;

        private float _currentHP;
        private bool _isDead;
        // 활성 프리팹이 Instantiate될 때 Configure보다 Awake가 먼저 실행된 경우를 구분합니다.
        private bool _awakeInitialized;
        // 체력바/색상 연출/투사체 UI·월드 표시 등 시각 표현은 UnitPresenter에 위임합니다.
        private UnitPresenter _presenter;
        // 도트/기절/그을림/침묵 디버프 상태는 UnitStatusEffects에 위임합니다.
        private UnitStatusEffects _status;

        private void Awake()
        {
            _presenter = new UnitPresenter(healthBar, spriteRenderer, projectilePrefab, skillNameLabel, skillNameDisplayDuration, team);
            _status = new UnitStatusEffects();
            InitializeRuntimeState();
            BattleUnitRegistry.Register(this);
            _awakeInitialized = true;
        }

        private void OnDestroy()
        {
            BattleUnitRegistry.Unregister(this);
        }

        private void OnDisable()
        {
            BattleUnitRegistry.Unregister(this);
        }

        private void Update()
        {
            if (_isDead || CombatManager.Instance == null)
            {
                return;
            }

            // 도트 데미지는 기절 중에도 계속 진행되어야 하므로 가장 먼저 처리합니다.
            _status.Tick(Time.deltaTime, dmg => TakeDamage(dmg));
            _presenter.SetDebuffTint(_status.IndicatorColor);
            if (_isDead)
            {
                return;
            }

            if (_status.IsStunned)
            {
                return;
            }

            Unit target = ResolveTarget();
            if (target == null)
            {
                return;
            }

            for (int i = 0; i < _skills.Count; i++)
            {
                // 쿨다운은 침묵 중에도 계속 흐르고, 발동(캐스트)만 막습니다.
                // 0번째 항목(기본공격)은 예외로 침묵 중에도 발동합니다.
                bool isBasicAttack = i == 0;
                RuntimeSkill skill = _skills[i];
                skill.timer -= Time.deltaTime;
                if (skill.timer <= 0f && (isBasicAttack || !_status.IsSilenced))
                {
                    StartCoroutine(CastSkill(target, skill, isBasicAttack));
                    skill.timer = GetSkillCooldown(skill);
                }
            }
        }

        /// <summary>
        /// 공용 아군 프리팹을 UnitData 기준으로 설정합니다.
        /// Awake()가 이 값들을 읽어 초기화하므로, 프리팹을 비활성 상태로 Instantiate한 뒤
        /// 활성화하기 전에 호출해야 합니다.
        /// </summary>
        public void Configure(UnitData data)
        {
            // [추가] 잘못된 전달 데이터로 프리팹 기본값을 덮어쓰지 않도록 방어
            if (data == null)
            {
                Debug.LogError("[Unit] Configure에 전달된 UnitData가 null입니다.", this);
                return;
            }

            maxHP = data.healthPoint;
            attackPoint = data.attackPoint;
            defensePoint = data.defensePoint;
            criticalRate = data.criticalRate;
            criticalMult = data.criticalMult;
            dodgeRate = data.dodgeRate;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = data.color;
            }

            ResolveSkills(data.skillIds, RuntimeDataManager.Instance.GetSkill);
            SetBasicAttackCooldown(data.attackSpeed);

            if (_awakeInitialized)
            {
                InitializeRuntimeState();
            }
        }

        /// <summary>
        /// 적 프리팹을 MonsterData 기준으로 설정합니다. Configure(UnitData)의 적 버전으로,
        /// MonsterData에 없는 필드(스킬 타입, 색상)는 프리팹 기본값을 그대로 둡니다.
        /// </summary>
        public void ConfigureEnemy(MonsterData data)
        {
            if (data == null)
            {
                Debug.LogError("[Unit] ConfigureEnemy에 전달된 MonsterData가 null입니다.", this);
                return;
            }

            // data는 EnemyManager.BuildCombatSpec()이 턴 성장 배율과 훔친 액티브 스킬까지 반영해
            // 이미 확정한 전투용 스펙입니다. Unit은 그 값을 그대로 받아 런타임 상태(쿨타임/체력
            // 진행)만 관리합니다. 상세: Docs/COMBAT_REFACTOR_TASKS.md 21번.
            maxHP = data.healthPoint;
            attackPoint = data.attackPoint;
            defensePoint = data.defensePoint;
            criticalRate = data.criticalRate;
            criticalMult = data.criticalMult;
            dodgeRate = data.dodgeRate;

            // 원본 몬스터 스킬(201~203)과 EnemyManager가 훔쳐온 유닛 액티브 스킬 모두
            // RuntimeDataManager.GetSkill()이 두 로스터를 순서대로 조회해 풀어줍니다.
            ResolveSkills(data.skillIds, RuntimeDataManager.Instance.GetSkill);
            SetBasicAttackCooldown(data.attackSpeed);

            if (_awakeInitialized)
            {
                InitializeRuntimeState();
            }
        }

        /// <summary>
        /// skillIds(0번째 = 기본공격)를 SkillData로 풀어 런타임 스킬 목록을 새로 구성합니다.
        /// resolver가 없거나 id를 못 찾으면 해당 스킬은 건너뜁니다.
        /// </summary>
        private void ResolveSkills(List<int> skillIds, System.Func<int, SkillData> resolver)
        {
            _skills.Clear();

            if (skillIds == null || resolver == null)
            {
                return;
            }

            foreach (int id in skillIds)
            {
                SkillData data = resolver(id);
                if (data == null)
                {
                    Debug.LogWarning($"[Unit] skill id {id}에 해당하는 SkillData를 찾을 수 없습니다.", this);
                    continue;
                }

                _skills.Add(new RuntimeSkill { data = data, timer = data.cooldown, damageMultiplier = 1f });
            }
        }

        private void InitializeRuntimeState()
        {
            _isDead = false;
            _currentHP = maxHP;
            _presenter.InitHealthBar(maxHP);
            _presenter.CaptureOriginalColor();

            foreach (RuntimeSkill skill in _skills)
            {
                skill.timer = GetSkillCooldown(skill);
            }
        }

        /// <summary>
        /// 0번째 스킬(기본공격)의 쿨다운을 유닛별 공격속도로 덮어씁니다. 기본공격은 여러 유닛이
        /// 같은 SkillData(공용 기본공격 정의)를 공유하므로, 그 데이터 자체를 고치는 대신
        /// RuntimeSkill.cooldownOverride로 유닛별 값을 별도로 둡니다.
        /// </summary>
        private void SetBasicAttackCooldown(float attackSpeed)
        {
            if (_skills.Count > 0)
            {
                _skills[0].cooldownOverride = attackSpeed;
            }
        }

        private static float GetSkillCooldown(RuntimeSkill skill)
        {
            return skill.cooldownOverride ?? skill.data.cooldown;
        }

        /// <summary>
        /// 시너지 등 퍼센트 기반 스탯 보너스를 적용합니다. value는 "+20"이면 20%를 뜻하며,
        /// 항상 곱연산(1 + value/100)으로 누적됩니다 — 여러 시너지가 겹치면 중첩 적용됩니다.
        /// 공격력은 유닛에 별도 공격력 스탯이 없어 스킬 데미지 배율(damageMultiplier)에 적용하고,
        /// 공격속도/쿨타임 감소는 기본공격 쿨다운에만 적용합니다(스킬별 쿨다운은 아직 배율 개념이 없음).
        /// </summary>
        public void ApplyStatEffect(EffectStatType statType, float percentValue)
        {
            float multiplier = 1f + percentValue / 100f;

            switch (statType)
            {
                case EffectStatType.MaxHealth:
                    maxHP *= multiplier;
                    _currentHP = maxHP;
                    _presenter.InitHealthBar(maxHP);
                    break;

                case EffectStatType.Attack:
                    foreach (RuntimeSkill skill in _skills)
                    {
                        skill.damageMultiplier *= multiplier;
                    }
                    break;

                case EffectStatType.Defense:
                    defensePoint *= multiplier;
                    break;

                case EffectStatType.CriticalChance:
                    criticalRate *= multiplier;
                    break;

                case EffectStatType.CriticalMultiplier:
                    criticalMult *= multiplier;
                    break;

                case EffectStatType.DodgeChance:
                    dodgeRate *= multiplier;
                    break;

                case EffectStatType.AttackInterval:
                    if (_skills.Count > 0)
                    {
                        float cooldown = GetSkillCooldown(_skills[0]);
                        _skills[0].cooldownOverride = cooldown * (1f - percentValue / 100f);
                    }
                    break;
            }
        }

        /// <summary>
        /// 스프라이트, 이름표, 체력바 등 화면 표시를 전환합니다.
        /// 전투 로직(타겟팅, 데미지, 레지스트리)은 그대로 유지한 채 화면 표시만 끕니다.
        /// </summary>
        public void SetVisualsVisible(bool visible)
        {
            _presenter.SetVisualsVisible(visible, gameObject);
        }

        /// <summary>
        /// UnitAnchor의 UI 이미지와 투사체 풀을 실제 Unit에 바인딩합니다.
        /// </summary>
        public void BindCombatUI(RectTransform combatAnchor, Image combatImage, UIProjectilePool projectilePool)
        {
            _presenter.BindCombatUI(combatAnchor, combatImage, projectilePool);
        }

        private Unit ResolveTarget()
        {
            return team == Team.Ally ? CombatManager.Instance.EnemyUnit : CombatManager.Instance.ResolveAllyTarget();
        }

        private void FireProjectile(Unit target, float damage)
        {
            // 아군이 공격할 때만 유물의 공격 트리거를 발동시킨다(유물은 플레이어 소유 시스템).
            if (team == Team.Ally)
            {
                RelicManager.Instance?.DispatchAttack();
            }

            // 그을림(공격력 감소) 디버프는 데미지 계산 시점에 반영한다.
            float effectiveDamage = damage * _status.AttackMultiplier;

            if (target != null)
            {
                // 회피율은 최대 60%까지만 적용됨(UnitData.xlsx 규칙).
                float dodgeChance = Mathf.Min(target.dodgeRate, 60f) / 100f;
                if (Random.value < dodgeChance)
                {
                    effectiveDamage = 0f;
                }
                else
                {
                    if (Random.value < criticalRate / 100f)
                    {
                        effectiveDamage *= criticalMult / 100f;
                    }

                    // 최종 데미지 = 공격력 - 방어력, 소수 둘째자리 반올림, 최소 1.0(UnitData.xlsx 규칙).
                    effectiveDamage = Mathf.Max(1f, effectiveDamage - target.defensePoint);
                    effectiveDamage = Mathf.Round(effectiveDamage * 100f) / 100f;
                }
            }

            _presenter.FireProjectile(target, target != null ? target._presenter : null, transform.position, effectiveDamage);

            PassiveEventBus.RaiseAttackLanded(this, target);
        }

        private IEnumerator CastSkill(Unit target, RuntimeSkill skill, bool isBasicAttack)
        {
            yield return _presenter.ShowSkillCastText(skill.data.name);

            if (target != null && !target.IsDead)
            {
                FireProjectile(target, skill.data.damage * skill.damageMultiplier);
                target._status.Apply(skill.data.debuff);

                // 기본공격은 "스킬 사용" 트리거의 대상이 아닙니다(패시브 기획 기준).
                if (!isBasicAttack)
                {
                    Debug.Log($"[Unit] {name}({team}) 액티브 스킬 사용: {skill.data.name}");
                    PassiveEventBus.RaiseSkillUsed(this, skill.data);
                }
            }
        }

        public void TakeDamage(float dmg)
        {
            if (_isDead)
            {
                return;
            }

            _currentHP -= dmg;
            _presenter.SetHP(_currentHP);

            if (_currentHP <= 0f)
            {
                Die();
            }
            else
            {
                StartCoroutine(_presenter.HitFlash());
            }
        }

        private void Die()
        {
            _isDead = true;
            BattleUnitRegistry.Unregister(this);

            _presenter.HideCombatImage();
            gameObject.SetActive(false);

            PassiveEventBus.RaiseDeath(this);
        }
    }
}
