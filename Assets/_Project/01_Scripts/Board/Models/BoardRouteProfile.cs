using UnityEngine;

namespace OzGameLab01.Board.Models
{
    /// <summary>
    /// 중간 보스와 최종 보스 목표를 선정하는 경로 디렉팅 규칙입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBoardRouteProfile", menuName = "OZGL/Board/Route Profile")]
    public sealed class BoardRouteProfile : ScriptableObject
    {
        [Header("Progress")]
        [Tooltip("최종 보스 전에 처치해야 하는 중간 보스 수입니다.")]
        [Min(0)] [SerializeField] private int requiredEliteCount = 3;

        [Header("Leg Distance (tile hops)")]
        [Tooltip("현재 위치에서 다음 중간 보스까지의 최소 그래프 거리입니다.")]
        [Min(1)] [SerializeField] private int minimumEliteLegDistance = 10;

        [Tooltip("현재 위치에서 다음 중간 보스까지의 최대 그래프 거리입니다.")]
        [Min(1)] [SerializeField] private int maximumEliteLegDistance = 18;

        [Tooltip("최종 보스가 직전 중간 보스에 너무 붙지 않도록 보장하는 최소 거리입니다.")]
        [Min(1)] [SerializeField] private int minimumBossLegDistance = 10;

        [Tooltip("남은 목표를 배치할 공간이 부족한 후보를 피하기 위한 여유 거리 비율입니다.")]
        [Range(0f, 1f)] [SerializeField] private float futureRouteReserveRatio = 0.6f;

        [Header("Route Scoring")]
        [Min(0f)] [SerializeField] private float forwardProgressWeight = 1.5f;
        [Min(0f)] [SerializeField] private float explorationOpportunityWeight = 1.2f;
        [Min(0f)] [SerializeField] private float sideAlternationWeight = 2f;
        [Min(1f)] [SerializeField] private float maximumDetourRatio = 1.6f;

        public int RequiredEliteCount => requiredEliteCount;

        public BoardRouteSettings CreateSettings()
        {
            return new BoardRouteSettings
            {
                requiredEliteCount = requiredEliteCount,
                minimumEliteLegDistance = minimumEliteLegDistance,
                maximumEliteLegDistance = Mathf.Max(minimumEliteLegDistance, maximumEliteLegDistance),
                minimumBossLegDistance = minimumBossLegDistance,
                futureRouteReserveRatio = futureRouteReserveRatio,
                forwardProgressWeight = forwardProgressWeight,
                explorationOpportunityWeight = explorationOpportunityWeight,
                sideAlternationWeight = sideAlternationWeight,
                maximumDetourRatio = maximumDetourRatio,
            };
        }
    }
}
