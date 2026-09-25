using System;
using System.Collections.Generic;
using UnityEngine;

namespace OzGameLab01.Combat
{
    public enum DebuffType
    {
        None,
        DamageOverTime,
        Stun,
        AttackDown,
        Silence
    }

    [Serializable]
    public struct DebuffProfile
    {
        public DebuffType type;
        public float duration;
        [Tooltip("DamageOverTime: 틱당 데미지 / AttackDown: 공격력 감소 비율(0~1). 나머지 타입은 사용하지 않음.")]
        public float magnitude;
        [Tooltip("DamageOverTime 전용 틱 간격(초).")]
        public float tickInterval;
    }

    /// <summary>
    /// Unit에서 분리된 디버프(도트/기절/능력치감소/침묵) 상태를 관리하는 순수 C# 클래스.
    /// UnitPresenter와 같은 패턴 — Unit이 매 프레임 Tick()을 호출해준다.
    /// 같은 타입이 재적용되면 중첩하지 않고 지속시간/수치만 갱신한다.
    /// </summary>
    public sealed class UnitStatusEffects
    {
        private sealed class ActiveDebuff
        {
            public DebuffType type;
            public float remaining;
            public float duration;
            public float magnitude;
            public float tickInterval;
            public float tickTimer;
        }

        private readonly List<ActiveDebuff> _active = new List<ActiveDebuff>();

        /// <summary>onExpired가 있으면 지우기 전에 남아있던 각 디버프 타입마다 호출한다(연출 정리용).</summary>
        public void Clear(Action<DebuffType> onExpired = null)
        {
            if (onExpired != null)
            {
                foreach (ActiveDebuff debuff in _active) onExpired(debuff.type);
            }
            _active.Clear();
        }

        public bool IsStunned => HasType(DebuffType.Stun);
        public bool IsSilenced => HasType(DebuffType.Silence);
        public bool HasAnyDebuff => _active.Count > 0;

        public bool HasDebuff(DebuffType type) => HasType(type);

        public float AttackMultiplier
        {
            get
            {
                float multiplier = 1f;
                foreach (ActiveDebuff debuff in _active)
                {
                    if (debuff.type == DebuffType.AttackDown)
                    {
                        multiplier *= Mathf.Clamp01(1f - debuff.magnitude);
                    }
                }

                return multiplier;
            }
        }

        /// <summary>
        /// 현재 활성 디버프를 대표하는 플레이스홀더 색상. 우선순위: 기절 > 침묵 > 도트 > 그을림.
        /// 활성 디버프가 없으면 null.
        /// </summary>
        public Color? IndicatorColor
        {
            get
            {
                if (HasType(DebuffType.Stun)) return new Color(0.55f, 0.6f, 1f);
                if (HasType(DebuffType.Silence)) return new Color(0.65f, 0.2f, 0.75f);
                if (HasType(DebuffType.DamageOverTime)) return new Color(1f, 0.45f, 0.1f);
                if (HasType(DebuffType.AttackDown)) return new Color(0.35f, 0.35f, 0.35f);
                return null;
            }
        }

        /// <summary>새로 적용된 디버프면 true(연출 트리거용), 기존 디버프의 지속시간만 갱신했으면 false.</summary>
        public bool Apply(DebuffProfile profile)
        {
            if (profile.type == DebuffType.None || profile.duration <= 0f)
            {
                return false;
            }

            foreach (ActiveDebuff debuff in _active)
            {
                if (debuff.type == profile.type)
                {
                    debuff.remaining = profile.duration;
                    debuff.duration = profile.duration;
                    debuff.magnitude = profile.magnitude;
                    debuff.tickInterval = profile.tickInterval;
                    return false;
                }
            }

            _active.Add(new ActiveDebuff
            {
                type = profile.type,
                remaining = profile.duration,
                duration = profile.duration,
                magnitude = profile.magnitude,
                tickInterval = profile.tickInterval,
                tickTimer = profile.tickInterval
            });
            return true;
        }

        /// <summary>
        /// 전투 UI(상태이상 아이콘+지속시간 게이지)에 표시할 대표 디버프 하나를 반환합니다.
        /// 우선순위는 IndicatorColor와 동일(기절 > 침묵 > 도트 > 그을림). 활성 디버프가
        /// 여러 개 겹쳐도 UI 슬롯이 하나뿐이라(StatusEffectItemView) 가장 급한 것만 보여줍니다.
        /// </summary>
        public bool TryGetPrimaryDebuff(out DebuffType type, out float remaining, out float duration)
        {
            foreach (DebuffType priority in new[]
                     {
                         DebuffType.Stun, DebuffType.Silence, DebuffType.DamageOverTime, DebuffType.AttackDown
                     })
            {
                foreach (ActiveDebuff debuff in _active)
                {
                    if (debuff.type == priority)
                    {
                        type = debuff.type;
                        remaining = debuff.remaining;
                        duration = debuff.duration;
                        return true;
                    }
                }
            }

            type = DebuffType.None;
            remaining = 0f;
            duration = 0f;
            return false;
        }

        /// <summary>
        /// 대표 디버프(TryGetPrimaryDebuff 우선순위) 하나만 제거한다. 제거했으면 그 타입을 반환하고, 없으면 None.
        /// </summary>
        public DebuffType RemovePrimary()
        {
            if (!TryGetPrimaryDebuff(out DebuffType type, out _, out _)) return DebuffType.None;
            _active.RemoveAll(debuff => debuff.type == type);
            return type;
        }

        /// <summary>
        /// 지속시간을 감소시키고 도트 데미지 틱을 처리한 뒤, 만료된 디버프를 제거한다.
        /// 기절 여부와 무관하게 매 프레임 호출되어야 한다(도트는 기절 중에도 진행).
        /// onExpired는 방금 만료되어 제거된 디버프 타입마다 호출된다(연출 정리용).
        /// </summary>
        public void Tick(float deltaTime, Action<float> onDamageOverTime, Action<DebuffType> onExpired = null)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ActiveDebuff debuff = _active[i];
                debuff.remaining -= deltaTime;

                if (debuff.type == DebuffType.DamageOverTime)
                {
                    debuff.tickTimer -= deltaTime;
                    if (debuff.tickTimer <= 0f)
                    {
                        debuff.tickTimer += Mathf.Max(0.01f, debuff.tickInterval);
                        onDamageOverTime?.Invoke(debuff.magnitude);
                    }
                }

                if (debuff.remaining <= 0f)
                {
                    _active.RemoveAt(i);
                    onExpired?.Invoke(debuff.type);
                }
            }
        }

        private bool HasType(DebuffType type)
        {
            foreach (ActiveDebuff debuff in _active)
            {
                if (debuff.type == type)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
