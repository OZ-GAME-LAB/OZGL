namespace OzGameLab01.Effects.Models
{
    public enum EffectsNotificationKind
    {
        CacheRebuilt,
        RelicAcquired,
        RelicsRestored,
        RelicsCleared,
        SynergiesEvaluated
    }

    /// <summary>처리 완료 시점의 ID·개수·발행처별 순번 스냅샷</summary>
    public readonly struct EffectsNotification
    {
        public EffectsNotificationKind Kind { get; }
        public int SourceId { get; }
        public int Count { get; }
        public long Revision { get; }

        public EffectsNotification(EffectsNotificationKind kind, int sourceId, int count, long revision)
        {
            Kind = kind;
            SourceId = sourceId;
            Count = count;
            Revision = revision;
        }
    }
}