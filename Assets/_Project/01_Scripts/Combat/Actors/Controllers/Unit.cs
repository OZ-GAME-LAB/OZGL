using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OzGameLab01.Managers;
using OzGameLab01.Data;
using OzGameLab01.UI.Battle;

namespace OzGameLab01.Combat
{
    public class Unit : MonoBehaviour
    {
        public enum Team { Ally, Enemy }

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

        private readonly List<UnitSkillRuntime> _skills = new List<UnitSkillRuntime>();

        public bool IsDead => _isDead;
        public Team TeamValue => team;
        public float CurrentHp => _currentHP;
        public float MaxHp => maxHP;
        public RectTransform CombatAnchor => _presenter?.CombatAnchor;
        public string DisplayName { get; private set; }

        private float _currentHP;
        private bool _isDead;
        private UnitCombatStats _stats;
        private readonly UnitShieldPool _shields = new UnitShieldPool();
        private IRandomProvider _random = new CombatRandom();
        public float Shield => _shields.Total;
        public void SetRandomProvider(IRandomProvider random)
            => _random = random ?? throw new System.ArgumentNullException(nameof(random));
        // 활성 프리팹이 Instantiate될 때 Configure보다 Awake가 먼저 실행된 경우를 구분합니다.
        private bool _awakeInitialized;
        // 체력바/색상 연출/투사체 UI·월드 표시 등 시각 표현은 UnitPresenter에 위임합니다.
        private UnitPresenter _presenter;
        // 도트/기절/그을림/침묵 디버프 상태는 UnitStatusEffects에 위임합니다.
        private UnitStatusEffects _status;
        // -1 means that no fixed-damage synergy is active.
        private float _fixedDamage = -1f;
        private float _extraDamageOnStatusPercent;
        private float _statusEffectChance;
        private bool _useSkillTwice;
        private bool _activeSkillsDisabled;
        private bool _noSkillStatBuffApplied;
        private float _damageReductionDefenseStep;
        private float _damageReductionPercent;
        private float _shieldBonusDamagePerDefensePercent;
        private float _shieldDamageReductionPerDefensePercent;

        public bool HasAnyDebuff => _status != null && _status.HasAnyDebuff;
        public bool AreActiveSkillsDisabled => _activeSkillsDisabled;

        private void EnsureRuntimeComponents()
        {
            if (_presenter == null)
            {
                _presenter = new UnitPresenter(
                    healthBar, spriteRenderer, projectilePrefab,
                    skillNameLabel, skillNameDisplayDuration, team);
            }

            if (_status == null)
            {
                _status = new UnitStatusEffects();
            }
        }

        private void Awake()
        {
            EnsureRuntimeComponents();
            InitializeRuntimeState();
            CombatUnitRegistry.Register(this);
            _awakeInitialized = true;
        }

        private void OnDestroy()
        {
            CombatUnitRegistry.Unregister(this);
        }

        private void OnDisable()
        {
            CombatUnitRegistry.Unregister(this);
        }

        private void Update()
        {
            if (_isDead || CombatManager.Instance == null)
            {
                return;
            }

            // 도트 데미지는 기절 중에도 계속 진행되어야 하므로 가장 먼저 처리합니다.
            if (_stats != null && _stats.Tick(Time.deltaTime)) RefreshStats();
            _shields.Tick(Time.deltaTime);
            _status.Tick(Time.deltaTime, dmg => TakeDamage(dmg));
            _presenter.SetDebuffTint(_status.IndicatorColor);
            bool hasCooldown = TryGetActiveSkillCooldown(out float cdRemaining, out float cdDuration);
            _presenter.UpdateHud(_currentHP, maxHP, hasCooldown, cdRemaining, cdDuration);
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
                UnitSkillRuntime skill = _skills[i];
                skill.timer -= Time.deltaTime;
                if (skill.timer <= 0f && (isBasicAttack || (!_activeSkillsDisabled && !_status.IsSilenced)))
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

            DisplayName = data.name;
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

            ResolveSkills(data.skillIds, OzGameLab01.Data.RuntimeContent.Catalog.GetSkill);
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

            DisplayName = data.name;
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
            // ContentCatalog가 적/아군 스킬 정의를 통합 조회합니다.
            ResolveSkills(data.skillIds, RuntimeContent.Catalog.GetSkill);
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

                _skills.Add(new UnitSkillRuntime { data = data, timer = data.cooldown, damageMultiplier = 1f });
            }
        }

        private void InitializeRuntimeState()
        {
            CaptureBaseStats();
            _isDead = false;
            _shields.Clear();
            _status.Clear();
            _fixedDamage = -1f;
            _extraDamageOnStatusPercent = 0f;
            _statusEffectChance = 0f;
            _useSkillTwice = false;
            _activeSkillsDisabled = false;
            _noSkillStatBuffApplied = false;
            _damageReductionDefenseStep = 0f;
            _damageReductionPercent = 0f;
            _shieldBonusDamagePerDefensePercent = 0f;
            _shieldDamageReductionPerDefensePercent = 0f;
            _currentHP = maxHP;
            _presenter.InitHealthBar(maxHP);
            _presenter.CaptureOriginalColor();

            foreach (UnitSkillRuntime skill in _skills)
            {
                skill.timer = GetSkillCooldown(skill);
            }
        }

        /// <summary>
        /// 0번째 스킬(기본공격)의 쿨다운을 유닛별 공격속도로 덮어씁니다. 기본공격은 여러 유닛이
        /// 같은 SkillData(공용 기본공격 정의)를 공유하므로, 그 데이터 자체를 고치는 대신
        /// UnitSkillRuntime.cooldownOverride로 유닛별 값을 별도로 둡니다.
        /// </summary>
        private void SetBasicAttackCooldown(float attackSpeed)
        {
            if (_skills.Count > 0)
            {
                _skills[0].cooldownOverride = attackSpeed;
            }
        }

        private static float GetSkillCooldown(UnitSkillRuntime skill)
        {
            return skill.cooldownOverride ?? skill.data.cooldown;
        }

        /// <summary>
        /// 1번째 스킬(액티브, 0번째는 기본공격)의 남은/전체 쿨다운. 전투 HUD 게이지 표시용.
        /// 액티브 스킬이 없으면 false.
        /// </summary>
        public bool TryGetActiveSkillCooldown(out float remaining, out float duration)
        {
            if (_skills.Count > 1)
            {
                UnitSkillRuntime skill = _skills[1];
                remaining = Mathf.Max(0f, skill.timer);
                duration = GetSkillCooldown(skill);
                return true;
            }

            remaining = 0f;
            duration = 0f;
            return false;
        }

        /// <summary>
        /// 전투 HUD 상태이상 아이콘 표시용. UnitStatusEffects.TryGetPrimaryDebuff 그대로 위임.
        /// </summary>
        public bool TryGetPrimaryDebuff(out DebuffType type, out float remaining, out float duration)
        {
            return _status.TryGetPrimaryDebuff(out type, out remaining, out duration);
        }

        /// <summary>
        /// 시너지 등 퍼센트 기반 스탯 보너스를 적용합니다. value는 "+20"이면 20%를 뜻하며,
        /// Add/Multiply 그룹의 합계를 원본 스탯에 적용하며 만료 시 원본에서 재계산합니다.
        /// 공격력 보너스는 기존 스킬 데미지 배율에도 반영하고,
        /// 공격속도/쿨타임 감소는 기본공격 쿨다운에만 적용합니다 — 액티브 스킬 쿨다운은
        /// 의도적으로 제외한 확정 기획입니다(2026-09-21 확정, 배율 개념 없음).
        /// </summary>
        public bool ApplyStatEffect(EffectStatType statType, float percentValue)
            => ApplyStatEffect(statType, percentValue, EffectOperation.Add, 0, true);

        public bool ApplyStatEffect(EffectStatType statType, float percentValue, EffectOperation operation,
            float durationSeconds, bool untilBattleEnd)
        {
            if (_stats == null) CaptureBaseStats();
            // 확정 기획(2026-09-21): 공격속도는 기본공격 쿨다운에만 적용하고,
            // untilBattleEnd가 아닌 시간제 효과는 지원하지 않는다.
            if (statType == EffectStatType.AttackInterval)
            {
                if (_skills.Count == 0 || !untilBattleEnd) return false;
                _skills[0].cooldownOverride = Mathf.Max(0.01f, GetSkillCooldown(_skills[0]) * (1 - percentValue / 100f));
                PassiveEventBus.RaiseBuffed(this);
                return true;
            }
            if (!_stats.Add(statType, percentValue, operation, durationSeconds, untilBattleEnd)) return false;
            RefreshStats();
            PassiveEventBus.RaiseBuffed(this);
            return true;
        }

        private void CaptureBaseStats()
        {
            _stats = new UnitCombatStats();
            _stats.SetBase(EffectStatType.MaxHealth, maxHP);
            _stats.SetBase(EffectStatType.Attack, attackPoint);
            _stats.SetBase(EffectStatType.Defense, defensePoint);
            _stats.SetBase(EffectStatType.CriticalChance, criticalRate);
            _stats.SetBase(EffectStatType.CriticalMultiplier, criticalMult);
            _stats.SetBase(EffectStatType.DodgeChance, dodgeRate);
            // RecoveryAmount is a percentage scale: base 100 means normal healing.
            _stats.SetBase(EffectStatType.RecoveryAmount, 100f);
        }

        private void RefreshStats()
        {
            float previousMax = maxHP;
            maxHP = _stats.Get(EffectStatType.MaxHealth);
            // Gain adds the increase; loss preserves HP subject to the new maximum.
            _currentHP = Mathf.Min(maxHP, _currentHP + Mathf.Max(0, maxHP - previousMax));
            attackPoint = _stats.Get(EffectStatType.Attack);
            defensePoint = _stats.Get(EffectStatType.Defense);
            criticalRate = _stats.Get(EffectStatType.CriticalChance);
            criticalMult = _stats.Get(EffectStatType.CriticalMultiplier);
            dodgeRate = _stats.Get(EffectStatType.DodgeChance);
            foreach (UnitSkillRuntime skill in _skills) skill.damageMultiplier = _stats.GetMultiplier(EffectStatType.Attack);
            _presenter.InitHealthBar(maxHP);
            _presenter.SetHP(_currentHP);
        }

        /// <summary>
        /// 스프라이트, 이름표, 체력바 등 화면 표시를 전환합니다.
        /// 전투 로직(타겟팅, 데미지, 레지스트리)은 그대로 유지한 채 화면 표시만 끕니다.
        /// </summary>
        public void SetVisualsVisible(bool visible)
        {
            EnsureRuntimeComponents();
            _presenter.SetVisualsVisible(visible, gameObject);
        }

        /// <summary>
        /// UnitAnchor의 UI 이미지와 투사체 풀을 실제 Unit에 바인딩합니다.
        /// </summary>
        public void BindCombatUI(RectTransform combatAnchor, Image combatImage, UIProjectilePool projectilePool)
        {
            // Factory intentionally configures inactive instances before SetActive(true).
            // Unity may defer Awake for those instances, so prepare the presenter lazily.
            EnsureRuntimeComponents();
            _presenter.BindCombatUI(combatAnchor, combatImage, projectilePool);
        }

        /// <summary>
        /// UnitAnchor 아래에 스폰된 아군 전투 HUD(체력/액티브 스킬 쿨다운 게이지)를 바인딩합니다.
        /// </summary>
        public void BindHud(AllyUnitCombatHUDView hud)
        {
            EnsureRuntimeComponents();
            _presenter.BindHud(hud);
        }

        private Unit ResolveTarget()
        {
            return team == Team.Ally ? CombatManager.Instance.Facade.EnemyUnit : CombatManager.Instance.Facade.ResolveAllyTarget();
        }

        private void FireProjectile(Unit target, float damage, System.Action onImpact = null, bool applyDamage = true)
        {
            // 그을림(공격력 감소) 디버프는 데미지 계산 시점에 반영한다.
            float baseDamage = _fixedDamage >= 0f ? _fixedDamage : damage;
            if (target != null && target.HasAnyDebuff)
            {
                baseDamage *= 1f + _extraDamageOnStatusPercent / 100f;
            }
            float effectiveDamage = baseDamage * _status.AttackMultiplier;
            if (Shield > 0f)
            {
                effectiveDamage *= 1f + defensePoint * _shieldBonusDamagePerDefensePercent / 100f;
            }

            if (target != null && applyDamage)
            {
                // 회피율은 최대 60%까지만 적용됨(UnitData.xlsx 규칙).
                float dodgeChance = Mathf.Min(target.dodgeRate, 60f) / 100f;
                if (_random.NextDouble() < dodgeChance)
                {
                    effectiveDamage = 0f;
                }
                else
                {
                    if (_random.NextDouble() < criticalRate / 100f)
                    {
                        effectiveDamage *= criticalMult / 100f;
                    }

                    // 최종 데미지 = 공격력 - 방어력, 소수 둘째자리 반올림, 최소 1.0(UnitData.xlsx 규칙).
                    effectiveDamage = Mathf.Max(1f, effectiveDamage - target.defensePoint);
                    effectiveDamage = Mathf.Round(effectiveDamage * 100f) / 100f;
                }
            }

            _presenter.FireProjectile(target, target != null ? target._presenter : null, transform.position, effectiveDamage,
                applyDamage, onImpact);

            PassiveEventBus.RaiseAttackLanded(this, target);
            if (applyDamage) TryApplyRandomStatusEffect(target);
        }

        private IEnumerator CastSkill(Unit target, UnitSkillRuntime skill, bool isBasicAttack)
        {
            int useCount = !isBasicAttack && _useSkillTwice ? 2 : 1;
            for (int useIndex = 0; useIndex < useCount; useIndex++)
            {
            yield return _presenter.ShowSkillCastText(skill.data.name);

            if (target != null && !target.IsDead)
            {
                if (skill.data.effects != null && skill.data.effects.Count > 0)
                {
                    FireProjectile(target, 0f,
                        () => ExecuteSkillEffects(skill.data.effects, target, skill.damageMultiplier), false);
                }
                else
                {
                    FireProjectile(target, skill.data.damage * skill.damageMultiplier);
                    target._status.Apply(skill.data.debuff);
                }

                // 기본공격은 "스킬 사용" 트리거의 대상이 아닙니다(패시브 기획 기준).
                if (!isBasicAttack)
                {
                    CombatManager.Instance?.Facade.ReportFeedback(new CombatFeedback(
                        CombatFeedbackKind.Skill, skill.data.name,
                        $"{DisplayName ?? name} → {target.DisplayName ?? target.name}", this));
                    Debug.Log($"[Unit] {name}({team}) 액티브 스킬 사용: {skill.data.name}");
                    PassiveEventBus.RaiseSkillUsed(this, skill.data);
                }
            }
            }
        }

        private void ExecuteSkillEffects(IReadOnlyList<EffectInstance> effects, Unit defaultTarget, float multiplier)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                EffectInstance effect = effects[i];
                foreach (Unit target in ResolveSkillTargets(effect, defaultTarget))
                {
                    if (target == null || target.IsDead) continue;
                    switch (effect.effect)
                    {
                        case EffectType.DealDamage:
                            ApplySkillDamage(target, effect.effectParam * multiplier);
                            break;
                        case EffectType.Heal:
                            target.Heal(effect.effectParam);
                            break;
                        case EffectType.GrantShield:
                            target.GrantShield(effect.effectParam, effect.durationSeconds, effect.untilBattleEnd);
                            break;
                        case EffectType.StatModifier:
                            target.ApplyStatEffect(effect.statType, effect.effectParam, effect.operation,
                                effect.durationSeconds, effect.untilBattleEnd || effect.durationSeconds <= 0);
                            break;
                        case EffectType.CleanseDebuffs:
                            target.CleanseDebuffs();
                            break;
                    }
                }
            }
        }

        private IEnumerable<Unit> ResolveSkillTargets(EffectInstance effect, Unit defaultTarget)
        {
            EffectTarget target = effect.target;
            switch (target)
            {
                case EffectTarget.Self:
                    yield return this;
                    yield break;
                case EffectTarget.AllAllies:
                    foreach (Unit ally in CombatManager.Instance.Facade.GetParticipatingAllyUnits())
                        if (ally != null && !ally.IsDead) yield return ally;
                    yield break;
                case EffectTarget.FrontRow:
                case EffectTarget.MidRow:
                case EffectTarget.BackRow:
                    CombatManager.SlotRow row = target == EffectTarget.FrontRow
                        ? CombatManager.SlotRow.Front
                        : target == EffectTarget.MidRow ? CombatManager.SlotRow.Mid : CombatManager.SlotRow.Back;
                    foreach (Unit ally in CombatManager.Instance.Facade.GetAliveAlliesInRow(row))
                        if (ally != null && !ally.IsDead) yield return ally;
                    yield break;
                case EffectTarget.RandomAlly:
                    foreach (Unit ally in CombatTargetSelector.SelectRandom(
                        GetAliveAlliesForSkill(), effect.targetCount, _random))
                        yield return ally;
                    yield break;
                case EffectTarget.WorstHpAlly:
                    foreach (Unit ally in CombatTargetSelector.SelectWorstHp(
                        GetAliveAlliesForSkill(), effect.targetCount))
                        yield return ally;
                    yield break;
                default:
                    if (defaultTarget != null && !defaultTarget.IsDead) yield return defaultTarget;
                    yield break;
            }
        }

        private List<Unit> GetAliveAlliesForSkill()
        {
            var result = new List<Unit>();
            foreach (Unit ally in CombatManager.Instance.Facade.GetParticipatingAllyUnits())
                if (ally != null && !ally.IsDead) result.Add(ally);
            return result;
        }

        private void ApplySkillDamage(Unit target, float damage)
        {
            if (target == null || target.IsDead) return;
            float baseDamage = _fixedDamage >= 0f ? _fixedDamage : damage;
            if (target.HasAnyDebuff)
            {
                baseDamage *= 1f + _extraDamageOnStatusPercent / 100f;
            }
            float effectiveDamage = baseDamage * _status.AttackMultiplier;
            if (Shield > 0f)
            {
                effectiveDamage *= 1f + defensePoint * _shieldBonusDamagePerDefensePercent / 100f;
            }
            if (_random.NextDouble() < Mathf.Min(target.dodgeRate, 60f) / 100f) return;
            if (_random.NextDouble() < criticalRate / 100f) effectiveDamage *= criticalMult / 100f;
            effectiveDamage = Mathf.Max(1f, effectiveDamage - target.defensePoint);
            target.TakeDamage(Mathf.Round(effectiveDamage * 100f) / 100f);
            PassiveEventBus.RaiseAttackLanded(this, target);
            TryApplyRandomStatusEffect(target);
        }

        public bool Heal(float amount)
        {
            if (_isDead || amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return false;
            if (_stats == null) CaptureBaseStats();
            float previous = _currentHP;
            float recoveryMultiplier = _stats.Get(EffectStatType.RecoveryAmount) / 100f;
            _currentHP = Mathf.Min(maxHP, _currentHP + amount * Mathf.Max(0f, recoveryMultiplier));
            if (_currentHP <= previous) return false;
            _presenter.SetHP(_currentHP);
            PassiveEventBus.RaiseHealed(this);
            return true;
        }

        public bool GrantShield(float amount, float durationSeconds, bool untilBattleEnd)
        {
            if (_isDead) return false;
            float previous = _shields.Total;
            bool acquired = _shields.Add(amount, durationSeconds, untilBattleEnd);
            if (acquired) PassiveEventBus.RaiseShielded(this);
            return _shields.Total > previous;
        }

        public bool SetFixedDamage(float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f) return false;
            _fixedDamage = amount;
            return true;
        }

        public bool SetExtraDamageOnStatus(float percent)
        {
            if (float.IsNaN(percent) || float.IsInfinity(percent) || percent < 0f) return false;
            _extraDamageOnStatusPercent = percent;
            return true;
        }

        public bool SetStatusEffectChance(float percent)
        {
            if (float.IsNaN(percent) || float.IsInfinity(percent) || percent < 0f) return false;
            _statusEffectChance = Mathf.Clamp(percent, 0f, 100f);
            return true;
        }

        public bool SetUseSkillTwice(bool enabled)
        {
            _useSkillTwice = enabled;
            return true;
        }

        public bool SetNoSkillStatBuff(float percent)
        {
            if (float.IsNaN(percent) || float.IsInfinity(percent) || percent < 0f) return false;
            _activeSkillsDisabled = true;
            if (_noSkillStatBuffApplied) return true;
            _noSkillStatBuffApplied = true;

            EffectStatType[] stats =
            {
                EffectStatType.MaxHealth,
                EffectStatType.Attack,
                EffectStatType.Defense,
                EffectStatType.CriticalMultiplier,
                EffectStatType.CriticalChance,
                EffectStatType.DodgeChance,
                EffectStatType.RecoveryAmount
            };
            bool applied = true;
            for (int i = 0; i < stats.Length; i++)
            {
                applied &= ApplyStatEffect(stats[i], percent);
            }
            return applied;
        }

        public bool SetDefenseBasedDamageReduction(float defenseStep, float reductionPercent)
        {
            if (float.IsNaN(defenseStep) || float.IsInfinity(defenseStep) || defenseStep <= 0f ||
                float.IsNaN(reductionPercent) || float.IsInfinity(reductionPercent) || reductionPercent < 0f)
            {
                return false;
            }

            _damageReductionDefenseStep = defenseStep;
            _damageReductionPercent = Mathf.Clamp(reductionPercent, 0f, 100f);
            return true;
        }

        public bool SetShieldBonusDamage(float bonusPerDefensePercent, float reductionPerDefensePercent)
        {
            if (float.IsNaN(bonusPerDefensePercent) || float.IsInfinity(bonusPerDefensePercent) || bonusPerDefensePercent < 0f ||
                float.IsNaN(reductionPerDefensePercent) || float.IsInfinity(reductionPerDefensePercent) || reductionPerDefensePercent < 0f)
            {
                return false;
            }

            _shieldBonusDamagePerDefensePercent = bonusPerDefensePercent;
            _shieldDamageReductionPerDefensePercent = reductionPerDefensePercent;
            return true;
        }

        private void TryApplyRandomStatusEffect(Unit target)
        {
            if (target == null || target.IsDead || _statusEffectChance <= 0f ||
                _random.NextDouble() * 100f >= _statusEffectChance)
            {
                return;
            }

            DebuffType type = (DebuffType)(_random.Next(4) + 1);
            DebuffProfile profile;
            switch (type)
            {
                case DebuffType.DamageOverTime:
                    profile = new DebuffProfile { type = type, duration = 4f, magnitude = 5f, tickInterval = 1f };
                    break;
                case DebuffType.Stun:
                    profile = new DebuffProfile { type = type, duration = 2f };
                    break;
                case DebuffType.AttackDown:
                    profile = new DebuffProfile { type = type, duration = 4f, magnitude = 0.3f };
                    break;
                default:
                    profile = new DebuffProfile { type = DebuffType.Silence, duration = 3f };
                    break;
            }

            target.ApplyDebuff(profile);
        }

        public void ApplyDebuff(DebuffProfile profile) => _status.Apply(profile);

        public bool Revive(float healthPercent)
        {
            if (!_isDead || float.IsNaN(healthPercent) || float.IsInfinity(healthPercent)) return false;
            _isDead = false;
            _currentHP = Mathf.Clamp(maxHP * Mathf.Clamp(healthPercent, 0f, 100f) / 100f, 1f, maxHP);
            _shields.Clear();
            _status.Clear();
            gameObject.SetActive(true);
            CombatUnitRegistry.Register(this);
            _presenter.SetVisualsVisible(true, gameObject);
            _presenter.SetHP(_currentHP);
            return true;
        }

        public void CleanseDebuffs() => _status.Clear();

        public void TakeDamage(float dmg)
        {
            if (_isDead)
            {
                return;
            }

            if (_damageReductionDefenseStep > 0f && dmg > 0f)
            {
                float reduction = defensePoint / _damageReductionDefenseStep * _damageReductionPercent / 100f;
                dmg *= Mathf.Clamp01(1f - reduction);
            }
            if (Shield > 0f && _shieldDamageReductionPerDefensePercent > 0f && dmg > 0f)
            {
                float reduction = defensePoint * _shieldDamageReductionPerDefensePercent / 100f;
                dmg *= Mathf.Clamp01(1f - reduction);
            }
            dmg = _shields.Absorb(dmg);
            if (dmg <= 0f) return;
            float previousRatio = maxHP > 0f ? _currentHP / maxHP : 0f;
            _currentHP = Mathf.Max(0f, _currentHP - dmg);
            _presenter.SetHP(_currentHP);
            PassiveEventBus.RaiseHpChanged(this, previousRatio);

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
            CombatUnitRegistry.Unregister(this);

            _presenter.HideCombatImage();
            gameObject.SetActive(false);

            PassiveEventBus.RaiseDeath(this);
        }
    }
}
