using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

        public static List<Unit> All = new List<Unit>();

        public bool IsDead => _isDead;
        public SkillType Skill => skillType;
        public Team TeamValue => team;

        private int _level = 1;
        public int Level => _level;

        private float _currentHP;
        private bool _isDead;
        private Color _originalColor;
        private float _attackTimer;
        private float _skillTimer;

        private void Awake()
        {
            if (team == Team.Ally)
            {
                _level = SceneTransitioner.GetAllyLevel(skillType);
                float multiplier = 1f + 0.1f * (_level - 1);
                maxHP *= multiplier;
                basicAttack.damage *= multiplier;
                skillAttack.damage *= multiplier;
            }

            _currentHP = maxHP;
            healthBar.Init(maxHP);
            if (spriteRenderer != null)
            {
                _originalColor = spriteRenderer.color;
            }
            All.Add(this);

            _attackTimer = basicAttack.cooldown;
            _skillTimer = skillAttack.cooldown;
        }

        private void OnDestroy()
        {
            All.Remove(this);
        }

        private void OnDisable()
        {
            All.Remove(this);
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

        public void ApplySynergyBonus(float hpMultiplier, float attackMultiplier)
        {
            maxHP *= hpMultiplier;
            _currentHP = maxHP;
            healthBar.Init(maxHP);

            basicAttack.damage *= attackMultiplier;
            skillAttack.damage *= attackMultiplier;
        }

        private Unit ResolveTarget()
        {
            return team == Team.Ally ? CombatManager.Instance.EnemyUnit : CombatManager.Instance.ResolveAllyTarget();
        }

        private void FireProjectile(Unit target, float damage)
        {
            if (projectilePrefab == null)
            {
                return;
            }

            GameObject projectileObj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            Projectile projectile = projectileObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Init(target, damage);
            }
        }

        private IEnumerator SkillAttack(Unit target)
        {
            if (spriteRenderer != null)
            {
                float half = skillGlowDuration / 2f;
                float t = 0f;

                while (t < half)
                {
                    t += Time.deltaTime;
                    spriteRenderer.color = Color.Lerp(_originalColor, skillGlowColor, t / half);
                    yield return null;
                }

                t = 0f;
                while (t < half)
                {
                    t += Time.deltaTime;
                    spriteRenderer.color = Color.Lerp(skillGlowColor, _originalColor, t / half);
                    yield return null;
                }

                spriteRenderer.color = _originalColor;
            }

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
            healthBar.SetHP(_currentHP);

            if (_currentHP <= 0f)
            {
                Die();
            }
            else
            {
                StartCoroutine(HitFlash());
            }
        }

        private IEnumerator HitFlash()
        {
            if (spriteRenderer == null)
            {
                yield break;
            }

            for (int i = 0; i < 3; i++)
            {
                spriteRenderer.color = Color.white;
                yield return new WaitForSeconds(0.05f);
                spriteRenderer.color = _originalColor;
                yield return new WaitForSeconds(0.05f);
            }
        }

        private void Die()
        {
            _isDead = true;
            All.Remove(this);
            gameObject.SetActive(false);
        }
    }
}
