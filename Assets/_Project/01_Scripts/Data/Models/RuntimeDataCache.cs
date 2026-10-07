using System.Collections.Generic;

namespace OzGameLab01.Data
{
    /// <summary>런타임 시너지·유물 정의 캐시와 ID 조회</summary>
    public sealed class RuntimeDataCache
    {
        private readonly IdDataCache<SynergyData> _synergies = new IdDataCache<SynergyData>(data => data.id);
        private readonly IdDataCache<RelicData> _relics = new IdDataCache<RelicData>(data => data.id);

        public IReadOnlyDictionary<int, SynergyData> Synergies => _synergies.Items;
        public IReadOnlyDictionary<int, RelicData> Relics => _relics.Items;

        public void ReplaceSynergies(List<SynergyData> items) => _synergies.Replace(items ?? new List<SynergyData>());
        public void ReplaceRelics(List<RelicData> items) => _relics.Replace(items ?? new List<RelicData>());
        public SynergyData GetSynergy(int id) => _synergies.Get(id);
        public RelicData GetRelic(int id) => _relics.Get(id);
    }
}