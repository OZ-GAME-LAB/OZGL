using UnityEngine;
using OzGameLab01.Data;

namespace OzGameLab01.Interfaces
{
    /// <summary>
    /// 유물 로직 추상 클래스
    /// 유물의 동작 원리 정의
    /// </summary>
    public abstract class RelicLogic
    {
        public RelicData Data { get; private set; }
        public RelicRuntimeInstance Runtime { get; private set; }

        // 
        public virtual void Initialize(RelicData data, RelicRuntimeInstance runtime)
        {
            Data = data;
            Runtime = runtime;
        }

        public virtual void OnEquip() { }           // 장착 시 적용 메서드
        public virtual void OnAttack() { }          // 전투 페이즈에서 공격 시 적용 메서드
        public virtual void OnDiceRolled() { }      // 주사위를 굴릴 때 적용 메서드
    }

    public interface IAttackTriggerRelic
    {
        void OnAttack();
    }

    public interface IDiceTriggerRelic
    {
        void OnDiceRolled();
    }

    #region 로직 모음
    // 모든 유닛 공격력 +1
    public class AllUnitAtkPlusOne : RelicLogic
    {
        public override void OnEquip()
        {
            
        }
    }

    public class EnemyHitOneDamage : RelicLogic, IAttackTriggerRelic
    {
        public override void OnAttack()
        {
            base.OnAttack();
        }
    }
    #endregion
}