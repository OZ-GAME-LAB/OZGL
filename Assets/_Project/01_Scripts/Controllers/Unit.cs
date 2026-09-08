using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using OzGameLab01.Managers;

namespace OzGameLab01.Combat
{
    public class Unit : MonoBehaviour
    {
        public enum Team { Ally, Enemy }
        public enum SkillType { Warrior, Archer, Mage }

        [System.Serializable]
        private struct AttackProfile
        {
            public float damage;
            public float cooldown;
        }

        [SerializeField] private Team team;
        [SerializeField] private float maxHP = 100f;
        [SerializeField] private AttackProfile basicAttack = new AttackProfile { damage = 10f, cooldown = 1.2f };
        [SerializeField] private HealthBar healthBar;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private GameObject projectilePrefab;

        [SerializeField] private SkillType skillType;

        [Header("Skill (별도 쿨다운, 발광 후 강한 투사체)")]
        [SerializeField] private AttackProfile skillAttack = new AttackProfile { damage = 30f, cooldown = 4f };
        [SerializeField] private Color skillGlowColor = new Color(1f, 0.95f, 0.3f, 1f);
        [SerializeField] private float skillGlowDuration = 0.35f;

        public bool IsDead => _isDead;
        public SkillType Skill => skillType;
        public Team TeamValue => team;

        private int _level = 1;
        public int Level => _level;

        private float _currentHP;
        private bool _isDead;
        private float _attackTimer;
        private float _skillTimer;
        // 활성 프리팹이 Instantiate될 때 Configure보다 Awake가 먼저 실행된 경우를 구분합니다.
        private bool _awakeInitialized;
        // 체력바/색상 연출/투사체 UI·월드 표시 등 시각 표현은 UnitPresenter에 위임합니다.
        private UnitPresenter _presenter;

        private void Awake()
        {
            _presenter = new UnitPresenter(healthBar, spriteRenderer, projectilePrefab, skillGlowColor, skillGlowDuration, team);
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

            Unit target = ResolveTarget();
            if (target == null)
            {
                return;
            }

            _attackTimer -= Time.deltaTime;
            if (_attackTimer <= 0f)
            {
                FireProjectile(target, basicAttack.damage);
                _attackTimer = basicAttack.cooldown;
            }

            _skillTimer -= Time.deltaTime;
            if (_skillTimer <= 0f)
            {
                StartCoroutine(SkillAttack(target));
                _skillTimer = skillAttack.cooldown;
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

            skillType = data.skillType;
            maxHP = data.healthPoint;
            basicAttack.damage = data.attackPoint;
            basicAttack.cooldown = data.basicAttackCooldown;
            skillAttack.cooldown = data.skillCooldown;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = data.color;
            }

            if (_awakeInitialized)
            {
                InitializeRuntimeState();
            }
        }

        private void InitializeRuntimeState()
        {
            _level = 1;
            if (team == Team.Ally)
            {
                _level = SceneTransitioner.GetAllyLevel(skillType);
                float multiplier = 1f + 0.1f * (_level - 1);
                maxHP *= multiplier;
                basicAttack.damage *= multiplier;
                skillAttack.damage *= multiplier;
            }

            _isDead = false;
            _currentHP = maxHP;
            _presenter.InitHealthBar(maxHP);
            _presenter.CaptureOriginalColor();

            _attackTimer = basicAttack.cooldown;
            _skillTimer = skillAttack.cooldown;
        }

        public void ApplySynergyBonus(float hpMultiplier, float attackMultiplier)
        {
            maxHP *= hpMultiplier;
            _currentHP = maxHP;
            _presenter.InitHealthBar(maxHP);

            basicAttack.damage *= attackMultiplier;
            skillAttack.damage *= attackMultiplier;
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

            _presenter.FireProjectile(target, target != null ? target._presenter : null, transform.position, damage);
        }

        private IEnumerator SkillAttack(Unit target)
        {
            yield return _presenter.SkillGlow();

            if (target != null && !target.IsDead)
            {
                FireProjectile(target, skillAttack.damage);
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
        }
    }
}
