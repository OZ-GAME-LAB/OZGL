namespace OzGameLab01.Data
{
    public enum DataNotificationKind
    {
        /// <summary>역직렬화 결과의 캐시 교체 완료</summary>
        CacheReplaced,
        /// <summary>기존 저장값 대체 사용을 포함한 로스터 참조 연결 완료</summary>
        RosterBound,
        /// <summary>리소스 또는 목록 부재에 따른 기존 빈 결과 적용</summary>
        SourceUnavailable
    }

    /// <summary>데이터 적용 완료 시점의 값 스냅샷</summary>
    public readonly struct DataNotification
    {
        /// <summary>데이터 모델의 전체 타입명 기반 식별자</summary>
        public string Dataset { get; }
        public DataNotificationKind Kind { get; }
        public int Count { get; }
        /// <summary>발행처 인스턴스 내 적용 순번</summary>
        public long Revision { get; }

        public DataNotification(string dataset, DataNotificationKind kind, int count, long revision)
        {
            Dataset = dataset;
            Kind = kind;
            Count = count;
            Revision = revision;
        }
    }
}
