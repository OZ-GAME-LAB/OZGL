using OzGameLab01.Effects.Models;

/// <summary>
/// 기존 싱글톤 API를 유지하고 리스너 관리를 독립 모델에 위임합니다.
/// </summary>
public class BattleEffectCoordinator : Singleton<BattleEffectCoordinator>
{
    private readonly EffectListenerRegistry _listeners = new EffectListenerRegistry();

    public void RegisterListener(object target)
    {
        _listeners.RegisterListener(target);
    }

    public void UnregisterListener(object target)
    {
        _listeners.UnregisterListener(target);
    }

    public void ClearAllListeners()
    {
        _listeners.ClearAllListeners();
    }

    public void DispatchAttack()
    {
        _listeners.DispatchAttack();
    }

    public void DispatchDiceRoll()
    {
        _listeners.DispatchDiceRoll();
    }
}
