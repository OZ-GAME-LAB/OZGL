using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.Managers;

namespace Combat
{
    public class Unit : MonoBehaviour
    {
        public enum Team { Ally, Enemy }
        public enum SkillType { Warrior, Archer, Mage }

        [SerializeField] private Team team;
        [SerializeField] private float maxHP = 100f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackCooldown = 1.2f;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private HealthBar healthBar;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [SerializeField] private SkillType skillType;

        public static List<Unit> All = new List<Unit>();

        public float AttackRange => attackRange;
        public bool IsDead => _isDead;
        public SkillType Skill => skillType;
        public Team TeamValue => team;

        private int _level = 1;
        public int Level => _level;

        private float _currentHP;
        private bool _isDead;
        private Color _originalColor;

        private void Awake()
        {
            if (team == Team.Ally)
            {
                _level = SceneTransitioner.GetAllyLevel(skillType);
                float multiplier = 1f + 0.1f * (_level - 1);
                maxHP *= multiplier;
                attackDamage *= multiplier;
            }

            _currentHP = maxHP;
            healthBar.Init(maxHP);
            if (spriteRenderer != null)
            {
                _originalColor = spriteRenderer.color;
            }
            All.Add(this);
        }

        private void OnDestroy()
        {
            All.Remove(this);
        }

        private void OnDisable()
        {
            All.Remove(this);
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
