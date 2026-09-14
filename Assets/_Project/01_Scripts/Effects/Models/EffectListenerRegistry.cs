using System;
using System.Collections.Generic;
using OzGameLab01.Interfaces;

namespace OzGameLab01.Effects.Models
{
    /// <summary>
    /// Unity 객체 탐색 없이 리스너 등록과 공격 및 주사위 발동을 관리합니다.
    /// </summary>
    public sealed class EffectListenerRegistry
    {
        private sealed class Entry
        {
            public readonly object Target;
            public Entry(object target)
            {
                Target = target;
            }
        }

        private readonly List<Entry> _entries = new List<Entry>();

        public void RegisterListener(object target)
        {
            if (!(target is IEnterBattleTrigger || target is IAttackTrigger || target is IHitTrigger || target is IEndBattleTrigger || target is ISynergyCountModifier || target is ISynergyEvaluateTrigger || target is ICalculateStatTrigger || target is IDiceRollTrigger))
            {
                return;
            }
            foreach (Entry entry in _entries)
            {
                if (ReferenceEquals(entry.Target, target))
                {
                    return;
                }
            }
            _entries.Add(new Entry(target));
        }

        public void UnregisterListener(object target)
        {
            _entries.RemoveAll(entry => ReferenceEquals(entry.Target, target));
        }

        public void ClearAllListeners()
        {
            _entries.Clear();
        }

        public void DispatchAttack()
        {
            Dispatch<IAttackTrigger>(listener => listener.OnAttack());
        }

        public void DispatchDiceRoll()
        {
            Dispatch<IDiceRollTrigger>(listener => listener.OnDiceRolled());
        }

        private void Dispatch<T>(Action<T> invoke)
        {
            // 발동 중 신규 등록은 다음 호출부터 반영, 해제된 등록은 즉시 제외
            Entry[] snapshot = _entries.ToArray();
            foreach (Entry entry in snapshot)
            {
                if (_entries.Contains(entry) && entry.Target is T listener)
                {
                    invoke(listener);
                }
            }
        }
    }
}
