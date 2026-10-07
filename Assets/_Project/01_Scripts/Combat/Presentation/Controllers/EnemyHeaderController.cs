using OzGameLab01.Combat;
using OzGameLab01.UI;
using OzGameLab01.UI.Battle;

namespace OzGameLab01.Controllers
{
    /// <summary>
    /// CombatManager에서 분리된 적 헤더 UI(체력/스킬 쿨다운/상태이상) 갱신 책임을 담당하는
    /// 출력 컨트롤러입니다. 매 프레임 실제 Unit 수치를 읽어 View에 반영합니다.
    /// 아이콘(스킬/상태이상)은 아직 원화 에셋이 없어 항상 비어 있고, 게이지·수치만 반영됩니다.
    /// </summary>
    public sealed class EnemyHeaderController
    {
        private readonly CombatEnemyHeaderView headerView;
        private readonly EnemySkillCooldownItemView skillCooldownView;
        private readonly StatusEffectItemView statusEffectView;

        public EnemyHeaderController(
            CombatEnemyHeaderView headerView,
            EnemySkillCooldownItemView skillCooldownView,
            StatusEffectItemView statusEffectView)
        {
            this.headerView = headerView;
            this.skillCooldownView = skillCooldownView;
            this.statusEffectView = statusEffectView;
        }

        public void SetEnemyName(string name)
        {
            headerView?.SetEnemyName(name ?? string.Empty);
        }

        public void Refresh(Unit enemyUnit)
        {
            if (enemyUnit == null)
            {
                return;
            }

            headerView?.SetHealth(enemyUnit.CurrentHp, enemyUnit.MaxHp);

            if (skillCooldownView != null)
            {
                if (enemyUnit.TryGetActiveSkillCooldown(out float remaining, out float duration))
                {
                    skillCooldownView.SetCooldown(remaining, duration);
                }
                else
                {
                    skillCooldownView.SetVisible(false);
                }
            }

            if (statusEffectView != null)
            {
                if (enemyUnit.TryGetPrimaryDebuff(out _, out float debuffRemaining, out float debuffDuration))
                {
                    statusEffectView.SetVisible(true);
                    statusEffectView.SetDuration(debuffRemaining, debuffDuration);
                }
                else
                {
                    statusEffectView.SetVisible(false);
                }
            }
        }
    }
}
