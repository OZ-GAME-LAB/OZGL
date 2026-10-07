using UnityEngine;
using OzGameLab01.Data;
using System.Collections.Generic;

namespace OzGameLab01.Interfaces
{
    /// <summary>
    /// dev PR 전 임시 작성 enum (이후 삭제 필요) 
    /// </summary>
    public enum SynergyType
    {
        Knight,
        Hunter,
        Trickster,
        Human,
        Beast,
    }
    public interface ICalculateStatTrigger
    {
        void OnCalculate();
    }

    public interface IEnterBattleTrigger
    {
        void OnEnterBattle();
    }

    public interface IAttackTrigger
    {
        void OnAttack();
    }

    public interface IHitTrigger
    {
        void OnHit();
    }

    public interface IEndBattleTrigger
    {
        void OnEndBattle();
    }

    public interface IDiceRollTrigger
    {
        void OnDiceRolled();
    }

    public interface ISynergyCountModifier
    {
        void ModifySynergyCounts(Dictionary<SynergyType, int> synergyCounts);
    }

    public interface ISynergyEvaluateTrigger
    {
        void EvaluateSynergies();
    }
}
