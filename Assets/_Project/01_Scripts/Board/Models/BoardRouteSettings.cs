namespace OzGameLab01.Board.Models
{
    /// <summary>
    /// Inspector에서 전달받은 목표 선정 설정을 보관합니다.
    /// </summary>
    public sealed class BoardRouteSettings
    {
        public int eliteCount;
        public int minimumEliteLegDistance;
        public int maximumEliteLegDistance;
        public int minimumBossLegDistance;
        public float futureRouteReserveRatio;
        public float forwardProgressWeight;
        public float explorationOpportunityWeight;
        public float sideAlternationWeight;
        public float maximumDetourRatio;
    }
}
