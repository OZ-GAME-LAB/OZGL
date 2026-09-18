using OzGameLab01.Combat;
using OzGameLab01.Effects.Models;
using OzGameLab01.Interfaces;
using OzGameLab01.Common;

namespace OzGameLab01.Managers
{
    /// <summary>
    /// 플레이어가 현재 보유한 유닛 패시브와 유물 효과를 한 곳에 모아 트리거별로
    /// 인덱싱하고 상시 스탯 효과를 캐싱하는 EffectsFacade를 노출하는, 게임 부팅 후
    /// 계속 살아있는 매니저입니다. 실제 캐시 재구성/조회 로직은 EffectsFacade가
    /// 전담합니다. 슬롯 타입 정의(EffectSource 등, 외부에서 여전히 많이 참조함)만
    /// 컴파일타임 편의를 위해 이 클래스에 유지합니다.
    /// </summary>
    public sealed class RuntimeEffectManager : Singleton<RuntimeEffectManager>, IGameManager
    {
        public EffectsFacade Facade { get; private set; }
        public bool IsInitialized => Facade != null;

        protected override void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            base.Awake();
            // 씬 직접 실행 호환. 부팅 경로에서도 Initialize는 중복 호출에 안전하다.
            Initialize();
        }

        public void Initialize()
        {
            if (IsInitialized) return;
            try
            {
                Facade = new EffectsFacade();
                SystemBus.Register(Facade);
            }
            catch { Shutdown(); throw; }
        }

        public void Shutdown()
        {
            if (Facade != null)
            {
                SystemBus.Unregister(Facade);
                Facade.ClearSubscriptions();
            }
            Facade = null;
        }

        private void OnDestroy() => Shutdown();

        public enum EffectSourceKind
        {
            UnitPassive,
            Relic
        }

        public readonly struct EffectSource
        {
            public EffectSourceKind Kind { get; }
            public int SourceId { get; }
            public EffectInstance Definition { get; }
            public int DeclarationIndex { get; }

            public EffectSource(
                EffectSourceKind kind,
                int sourceId,
                EffectInstance definition,
                int declarationIndex)
            {
                Kind = kind;
                SourceId = sourceId;
                Definition = definition;
                DeclarationIndex = declarationIndex;
            }
        }

        public readonly struct StatModifierCache
        {
            public float Additive { get; }
            public float Multiplicative { get; }
            public int SourceCount { get; }

            public StatModifierCache(float additive, float multiplicative, int sourceCount)
            {
                Additive = additive;
                Multiplicative = multiplicative;
                SourceCount = sourceCount;
            }

            public float Apply(float baseValue)
            {
                return (baseValue + Additive) * Multiplicative;
            }
        }

    }
}
