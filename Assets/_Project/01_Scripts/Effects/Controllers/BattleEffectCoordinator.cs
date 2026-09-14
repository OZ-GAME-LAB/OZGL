using UnityEngine;
using OzGameLab01.Interfaces;
using System.Collections.Generic;

public class BattleEffectCoordinator : Singleton<BattleEffectCoordinator>
{
    // 전투 파이프라인 캐시
    private readonly List<IEnterBattleTrigger> _enterBattleListeners = new();
    private readonly List<IAttackTrigger> _attackListeners = new();
    private readonly List<IHitTrigger> _hitListeners = new();
    private readonly List<IEndBattleTrigger> _endBattleListeners = new();

    // 시너지 파이프라인 캐시
    private readonly List<ISynergyCountModifier> _synergyCountModifiers = new();
    private readonly List<ISynergyEvaluateTrigger> _synergyEvaluateListeners = new();

    // 정비 파이프라인 캐시
    private readonly List<ICalculateStatTrigger> _calculateStatListeners = new();
    private readonly List<IDiceRollTrigger> _diceListeners = new();

    #region 리스너 등록 및 해제

    /// <summary>
    /// 리스너 등록
    /// </summary>
    public void RegisterListener(object target)
    {
        if (target is IEnterBattleTrigger enterBattleTrigger && !_enterBattleListeners.Contains(enterBattleTrigger))
            _enterBattleListeners.Add(enterBattleTrigger);

        if (target is IAttackTrigger attackTrigger && !_attackListeners.Contains(attackTrigger))
            _attackListeners.Add(attackTrigger);

        if (target is IHitTrigger hitTrigger && !_hitListeners.Contains(hitTrigger))
            _hitListeners.Add(hitTrigger);

        if (target is IEndBattleTrigger endBattleTrigger && !_endBattleListeners.Contains(endBattleTrigger))
            _endBattleListeners.Add(endBattleTrigger);

        if (target is ISynergyCountModifier countModifier && !_synergyCountModifiers.Contains(countModifier))
            _synergyCountModifiers.Add(countModifier);

        if (target is ISynergyEvaluateTrigger synergyEvaluateTrigger && !_synergyEvaluateListeners.Contains(synergyEvaluateTrigger))
            _synergyEvaluateListeners.Add(synergyEvaluateTrigger);

        if (target is ICalculateStatTrigger calculateStatTrigger && !_calculateStatListeners.Contains(calculateStatTrigger))
            _calculateStatListeners.Add(calculateStatTrigger);

        if (target is IDiceRollTrigger diceTrigger && !_diceListeners.Contains(diceTrigger))
            _diceListeners.Add(diceTrigger);
    }

    /// <summary>
    /// 리스너 해제
    /// </summary>
    public void UnregisterListener(object target)
    {
        if (target is IEnterBattleTrigger enterBattleTrigger)
            _enterBattleListeners.Remove(enterBattleTrigger);

        if (target is IAttackTrigger attackTrigger)
            _attackListeners.Remove(attackTrigger);

        if (target is IHitTrigger hitTrigger)
            _hitListeners.Remove(hitTrigger);

        if (target is IEndBattleTrigger endBattleTrigger)
            _endBattleListeners.Remove(endBattleTrigger);

        if (target is ISynergyCountModifier countModifier)
            _synergyCountModifiers.Remove(countModifier);
        
        if (target is ISynergyEvaluateTrigger synergyEvaluateTrigger)
            _synergyEvaluateListeners.Remove(synergyEvaluateTrigger);

        if (target is ICalculateStatTrigger calculateStatTrigger)
            _calculateStatListeners.Remove(calculateStatTrigger);

        if (target is IDiceRollTrigger diceTrigger)
            _diceListeners.Remove(diceTrigger);
    }

    /// <summary>
    /// 모든 리스너 초기화
    /// </summary>
    public void ClearAllListeners()
    {
        _enterBattleListeners.Clear();
        _attackListeners.Clear();
        _hitListeners.Clear();
        _endBattleListeners.Clear();
        _synergyCountModifiers.Clear();
        _synergyEvaluateListeners.Clear();
        _calculateStatListeners.Clear();
        _diceListeners.Clear();
    }

    #endregion

    #region 디스패치 루프

    public void DispatchAttack()
    {
        int count = _attackListeners.Count;
        for (int i = 0; i < count; i++)
        {
            _attackListeners[i].OnAttack();
        }
    }

    public void DispatchDiceRoll()
    {
        int count = _diceListeners.Count;
        for (int i = 0; i < count; i++)
        {
            _diceListeners[i].OnDiceRolled();
        }
    }

    #endregion
}
