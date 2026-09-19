using System.Collections.Generic;
using OzGameLab01.Data;
using OzGameLab01.Effects.Models;
using OzGameLab01.Managers;

namespace OzGameLab01.Combat
{
    /// <summary>Owns effect indexes, never player possession or battle-local trigger state.</summary>
    public sealed class CombatEffectCatalog
    {
        private RuntimeEffectCache<RuntimeEffectManager.EffectSource> _cache = CreateCache();
        private static RuntimeEffectCache<RuntimeEffectManager.EffectSource> CreateCache()
            => new RuntimeEffectCache<RuntimeEffectManager.EffectSource>(source => source.Definition,
                source => (int)source.Kind, source => source.SourceId, source => source.DeclarationIndex);
        public bool IsCacheReady => _cache.IsCacheReady;
        public int CachedEffectCount => _cache.CachedEffectCount;
        public IReadOnlyList<RuntimeEffectManager.EffectSource> AllEffects => _cache.AllEffects;
        public IReadOnlyList<RuntimeEffectManager.EffectSource> OrderedEffects => _cache.OrderedEffects;
        public long Revision { get; private set; }

        public void Rebuild(ContentCatalog content, IEnumerable<UnitData> units, IEnumerable<RelicData> relics,
            IEnumerable<RuntimeEffectManager.EffectSource> extraSources = null)
        {
            var sources = new List<RuntimeEffectManager.EffectSource>();
            if (units != null)
                foreach (UnitData owned in units)
                {
                    if (owned == null) continue;
                    var definition = content.GetUnit(owned.id);
                    Add(sources, RuntimeEffectManager.EffectSourceKind.UnitPassive, owned.id, definition?.passiveEffects);
                }
            if (relics != null)
                foreach (RelicData owned in relics)
                {
                    if (owned == null) continue;
                    var definition = content.GetRelic(owned.id);
                    Add(sources, RuntimeEffectManager.EffectSourceKind.Relic, owned.id, definition?.effects);
                }
            if (extraSources != null)
                sources.AddRange(extraSources);
            var next = CreateCache();
            next.Rebuild(sources);
            _cache = next;
            Revision++;
        }

        private static void Add(List<RuntimeEffectManager.EffectSource> sources,
            RuntimeEffectManager.EffectSourceKind kind, int id, IReadOnlyList<EffectInstance> effects)
        {
            if (effects == null) return;
            for (int i = 0; i < effects.Count; i++)
                sources.Add(new RuntimeEffectManager.EffectSource(kind, id, effects[i], i));
        }

        public IReadOnlyList<RuntimeEffectManager.EffectSource> GetEffects(TriggerType trigger) => _cache.GetEffects(trigger);
        public bool TryGetAlwaysStatModifier(EffectStatType stat, EffectTarget target,
            out RuntimeEffectCache<RuntimeEffectManager.EffectSource>.EffectStatModifier modifier)
            => _cache.TryGetAlwaysStatModifier(stat, target, out modifier);
    }
}
