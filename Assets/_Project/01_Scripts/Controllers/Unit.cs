using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.Managers;
using OzGameLab01.UI;

namespace OzGameLab01.Controllers
{
    public class Unit : MonoBehaviour
    {
        public enum Team { Ally, Enemy }
        public enum UnitRole { Melee, Ranged }
        public enum SkillType { Warrior, Archer, Mage }

        [SerializeField] private Team team;
        [SerializeField] private UnitRole role;
        [SerializeField] private float maxHP = 100f;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackCooldown = 1.2f;
        [SerializeField] private HealthBar healthBar;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private GameObject aoeVfxPrefab;

        [SerializeField] private SkillType skillType;
        [SerializeField] private bool isLeader;

        [Header("Warrior Skill - Whirlwind")]
        [SerializeField] private float whirlwindRadius = 2.5f;
        [SerializeField] private float whirlwindTickDamage = 8f;
        [SerializeField] private float whirlwindTickInterval = 0.2f;
        [SerializeField] private float whirlwindDuration = 1.0f;
        [SerializeField] private float whirlwindMoveDistance = 1.5f;

        [Header("Archer Skill - Burst")]
        [SerializeField] private int burstShotCount = 5;
        [SerializeField] private float burstShotInterval = 0.1f;

        [Header("Mage Skill - Meteor")]
        [SerializeField] private float meteorRadius = 1.5f;
        [SerializeField] private float meteorDamage = 20f;
        [SerializeField] private float meteorTelegraphDelay = 0.5f;

        public static List<Unit> All = new List<Unit>();
        public static Unit Leader;

        public float AttackRange => attackRange;
        public bool IsDead => _isDead;
        public SkillType Skill => skillType;
        public Team TeamValue => team;

        private int _level = 1;
        public int Level => _level;

        private float _currentHP;
        private bool _isDead;
        private bool _isUsingSkill;
        private Unit _target;
        private float _cooldownTimer;
        private Color _originalColor;

        private void Awake()
        {
            if (team == Team.Ally)
            {
                _level = SceneTransitioner.GetAllyLevel(skillType);
                float multiplier = 1f + 0.1f * (_level - 1);
                maxHP *= multiplier;
                attackDamage *= multiplier;
                whirlwindTickDamage *= multiplier;
                meteorDamage *= multiplier;
            }

            _currentHP = maxHP;
            healthBar.Init(maxHP);
            if (spriteRenderer != null)
            {
                _originalColor = spriteRenderer.color;
            }
            All.Add(this);

            if (isLeader)
            {
                Leader = this;
            }
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
            if (_isDead || _isUsingSkill)
            {
                return;
            }

            _target = ResolveTarget();

            if (_target == null)
            {
                if (team == Team.Ally && !isLeader && Leader != null && !Leader.IsDead)
                {
                    Vector3 direction = (Leader.transform.position - transform.position).normalized;
                    direction = ApplySeparation(direction);
                    Move(direction);
                }
                return;
            }

            float distance = Vector3.Distance(transform.position, _target.transform.position);
            bool tooClose = role == UnitRole.Ranged && distance < attackRange * 0.5f;

            if (tooClose)
            {
                Vector3 direction = (transform.position - _target.transform.position).normalized;
                direction = ApplySeparation(direction);
                Move(direction);

                _cooldownTimer -= Time.deltaTime;
                if (_cooldownTimer <= 0f)
                {
                    Attack();
                    _cooldownTimer = attackCooldown;
                }
                return;
            }

            if (distance > attackRange)
            {
                if (team == Team.Ally && !isLeader && Leader != null && !Leader.IsDead)
                {
                    Vector3 direction = (Leader.transform.position - transform.position).normalized;
                    direction = ApplySeparation(direction);
                    Move(direction);
                }
                else
                {
                    Vector3 direction = (_target.transform.position - transform.position).normalized;
                    direction = ApplySeparation(direction);
                    Move(direction);
                }
                return;
            }

            if (role == UnitRole.Ranged && IsLineBlockedByAlly(_target))
            {
                Vector3 toTarget = (_target.transform.position - transform.position).normalized;
                Vector3 sidestepDir = new Vector3(toTarget.y, -toTarget.x, 0f);
                sidestepDir = ApplySeparation(sidestepDir);
                Move(sidestepDir);
                return;
            }

            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer <= 0f)
            {
                Attack();
                _cooldownTimer = attackCooldown;
            }
        }

        private static readonly Vector2 ArenaHalfExtents = new Vector2(6f, 5f);

        private void Move(Vector3 direction)
        {
            MoveByDistance(direction, moveSpeed * Time.deltaTime);
        }

        private void MoveByDistance(Vector3 direction, float distance)
        {
            Vector3 newPos = transform.position + direction * distance;
            newPos.x = Mathf.Clamp(newPos.x, -ArenaHalfExtents.x, ArenaHalfExtents.x);
            newPos.y = Mathf.Clamp(newPos.y, -ArenaHalfExtents.y, ArenaHalfExtents.y);
            transform.position = newPos;
        }

        private Vector3 ApplySeparation(Vector3 primaryDir)
        {
            Vector3 separation = Vector3.zero;

            foreach (Unit unit in All)
            {
                if (unit == null || unit == this || unit._isDead)
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, unit.transform.position);
                if (distance < 1.0f && distance > 0.0001f)
                {
                    separation += (transform.position - unit.transform.position).normalized / distance;
                }
            }

            if (separation == Vector3.zero)
            {
                return primaryDir;
            }

            return (primaryDir + separation).normalized;
        }

        private bool IsLineBlockedByAlly(Unit lineTarget)
        {
            Vector3 a = transform.position;
            Vector3 b = lineTarget.transform.position;
            Vector3 ab = b - a;
            float abLengthSq = ab.sqrMagnitude;

            if (abLengthSq < 0.0001f)
            {
                return false;
            }

            foreach (Unit unit in All)
            {
                if (unit == null || unit == this || unit._isDead || unit.team != team)
                {
                    continue;
                }

                Vector3 ap = unit.transform.position - a;
                float t = Vector3.Dot(ap, ab) / abLengthSq;
                if (t <= 0f || t >= 1f)
                {
                    continue;
                }

                Vector3 closestPoint = a + ab * t;
                float perpDistance = Vector3.Distance(unit.transform.position, closestPoint);
                if (perpDistance <= 0.5f)
                {
                    return true;
                }
            }

            return false;
        }

        private Unit ResolveTarget()
        {
            if (role == UnitRole.Melee)
            {
                Unit meleeThreat = FindClosestEnemy(UnitRole.Melee);
                if (meleeThreat != null && Vector3.Distance(transform.position, meleeThreat.transform.position) <= attackRange)
                {
                    return meleeThreat;
                }

                Unit rangedTarget = FindClosestEnemy(UnitRole.Ranged);
                if (rangedTarget != null)
                {
                    return rangedTarget;
                }
            }

            return FindClosestEnemy(null);
        }

        private Unit FindClosestEnemy(UnitRole? requiredRole)
        {
            Unit closest = null;
            float closestDistance = float.MaxValue;

            foreach (Unit unit in All)
            {
                if (unit == null || unit._isDead || unit.team == team)
                {
                    continue;
                }

                if (requiredRole.HasValue && unit.role != requiredRole.Value)
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, unit.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = unit;
                }
            }

            return closest;
        }

        private void Attack()
        {
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }

            if (role == UnitRole.Ranged)
            {
                if (projectilePrefab != null)
                {
                    GameObject projectileObj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
                    Projectile projectile = projectileObj.GetComponent<Projectile>();
                    if (projectile != null)
                    {
                        projectile.Init(_target, attackDamage);
                    }
                }
            }
            else
            {
                StartCoroutine(DealDamageAfterDelay(_target, attackDamage, 0.15f));
            }
        }

        private IEnumerator DealDamageAfterDelay(Unit victim, float damage, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (victim != null && !victim._isDead)
            {
                victim.TakeDamage(damage);
            }
        }

        public void UseSkill()
        {
            if (_isDead || _isUsingSkill)
            {
                return;
            }

            _isUsingSkill = true;

            switch (skillType)
            {
                case SkillType.Warrior:
                    StartCoroutine(Warrior_Whirlwind());
                    break;
                case SkillType.Archer:
                    StartCoroutine(Archer_Burst());
                    break;
                case SkillType.Mage:
                    StartCoroutine(Mage_Meteor());
                    break;
                default:
                    _isUsingSkill = false;
                    break;
            }
        }

        private IEnumerator Warrior_Whirlwind()
        {
            Unit closestEnemy = FindClosestEnemy(null);
            if (closestEnemy == null)
            {
                _isUsingSkill = false;
                yield break;
            }

            Vector3 direction = (closestEnemy.transform.position - transform.position).normalized;
            int tickCount = Mathf.Max(1, Mathf.RoundToInt(whirlwindDuration / whirlwindTickInterval));
            float moveStepDistance = whirlwindMoveDistance / tickCount;

            GameObject vfx = null;
            if (aoeVfxPrefab != null)
            {
                vfx = Instantiate(aoeVfxPrefab, transform.position, Quaternion.identity, transform);
                AoeCircleVFX aoeVfx = vfx.GetComponent<AoeCircleVFX>();
                if (aoeVfx != null)
                {
                    aoeVfx.Configure(whirlwindRadius, new Color(1f, 0.9f, 0.2f, 0.35f));
                }
            }

            for (int i = 0; i < tickCount; i++)
            {
                Vector3 moveDir = ApplySeparation(direction);
                MoveByDistance(moveDir, moveStepDistance);

                Unit[] snapshot = All.ToArray();
                foreach (Unit unit in snapshot)
                {
                    if (unit == null || unit._isDead || unit.team == team)
                    {
                        continue;
                    }

                    float distance = Vector3.Distance(transform.position, unit.transform.position);
                    if (distance <= whirlwindRadius)
                    {
                        unit.TakeDamage(whirlwindTickDamage);
                    }
                }

                yield return new WaitForSeconds(whirlwindTickInterval);
            }

            if (vfx != null)
            {
                Destroy(vfx);
            }

            _isUsingSkill = false;
        }

        private IEnumerator Archer_Burst()
        {
            Unit burstTarget = FindClosestEnemy(null);
            if (burstTarget == null)
            {
                _isUsingSkill = false;
                yield break;
            }

            for (int i = 0; i < burstShotCount; i++)
            {
                if (burstTarget == null || burstTarget._isDead)
                {
                    break;
                }

                if (projectilePrefab != null)
                {
                    GameObject projectileObj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
                    Projectile projectile = projectileObj.GetComponent<Projectile>();
                    if (projectile != null)
                    {
                        projectile.Init(burstTarget, attackDamage);
                    }
                }

                yield return new WaitForSeconds(burstShotInterval);
            }

            _isUsingSkill = false;
        }

        private IEnumerator Mage_Meteor()
        {
            Unit[] allSnapshot = All.ToArray();
            List<Unit> candidatesInRange = new List<Unit>();

            foreach (Unit unit in allSnapshot)
            {
                if (unit == null || unit._isDead || unit.team == team)
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, unit.transform.position);
                if (distance <= attackRange)
                {
                    candidatesInRange.Add(unit);
                }
            }

            if (candidatesInRange.Count == 0)
            {
                _isUsingSkill = false;
                yield break;
            }

            Vector3 bestPosition = candidatesInRange[0].transform.position;
            int bestCount = -1;

            foreach (Unit candidate in candidatesInRange)
            {
                Vector3 candidatePos = candidate.transform.position;
                int count = 0;

                foreach (Unit unit in allSnapshot)
                {
                    if (unit == null || unit._isDead || unit.team == team)
                    {
                        continue;
                    }

                    if (Vector3.Distance(candidatePos, unit.transform.position) <= meteorRadius)
                    {
                        count++;
                    }
                }

                if (count > bestCount)
                {
                    bestCount = count;
                    bestPosition = candidatePos;
                }
            }

            GameObject telegraph = null;
            if (aoeVfxPrefab != null)
            {
                telegraph = Instantiate(aoeVfxPrefab, bestPosition, Quaternion.identity);
                AoeCircleVFX telegraphVfx = telegraph.GetComponent<AoeCircleVFX>();
                if (telegraphVfx != null)
                {
                    telegraphVfx.Configure(meteorRadius, new Color(1f, 0.2f, 0.1f, 0.35f));
                }
            }

            yield return new WaitForSeconds(meteorTelegraphDelay);

            if (telegraph != null)
            {
                Destroy(telegraph);
            }

            if (aoeVfxPrefab != null)
            {
                GameObject impact = Instantiate(aoeVfxPrefab, bestPosition, Quaternion.identity);
                AoeCircleVFX impactVfx = impact.GetComponent<AoeCircleVFX>();
                if (impactVfx != null)
                {
                    impactVfx.Configure(meteorRadius * 1.3f, new Color(1f, 0.6f, 0.1f, 0.8f));
                    impactVfx.FadeOutAndDestroy(0.3f);
                }
            }

            Unit[] finalSnapshot = All.ToArray();
            foreach (Unit unit in finalSnapshot)
            {
                if (unit == null || unit._isDead || unit.team == team)
                {
                    continue;
                }

                float distance = Vector3.Distance(bestPosition, unit.transform.position);
                if (distance <= meteorRadius)
                {
                    unit.TakeDamage(meteorDamage);
                }
            }

            _isUsingSkill = false;
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
