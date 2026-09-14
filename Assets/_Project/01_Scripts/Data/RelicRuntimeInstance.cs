namespace OzGameLab01.Data
{
    /// <summary>
    /// RelicData의 정적 데이터를 게임 실행 중 실제 적용을 위해 묶은 런타임 인스턴스 클래스.
    /// 실제 효과 적용은 RelicData.effects(EffectInstance)를 RuntimeEffectManager가 취합해
    /// 처리하며, 이 클래스는 보유 인스턴스 식별용 래퍼입니다.
    /// </summary>
    public class RelicRuntimeInstance
    {
        public RelicData Data { get; }

        public RelicRuntimeInstance(RelicData data)
        {
            Data = data;
        }
    }
}