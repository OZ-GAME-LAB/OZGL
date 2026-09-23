using UnityEngine;

namespace OzGameLab01.Board.Models
{
    public enum FirstEliteSpawnOrigin
    {
        CurrentPlayerPosition,
        StartNode
    }

    /// <summary>
    /// 중간 보스와 최종 보스 목표를 선정하는 경로 디렉팅 규칙입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBoardRouteProfile", menuName = "OZGL/Board/Route Profile")]
    public sealed class BoardRouteProfile : ScriptableObject
    {
        [Header("Progress")]
        [Tooltip("최종 보스 전에 처치해야 하는 중간 보스 수입니다.")]
        [Min(0)] [SerializeField] private int requiredEliteCount = 3;

        [Tooltip("첫 중간 보스의 거리 계산 기준을 현재 플레이어 위치 또는 시작 노드 중에서 선택합니다.")]
        [SerializeField] private FirstEliteSpawnOrigin firstEliteSpawnOrigin =
            FirstEliteSpawnOrigin.CurrentPlayerPosition;

        [Header("구간 거리 (연결 타일 수)")]
        [Tooltip("현재 기준 위치에서 다음 중간 보스 후보까지 필요한 최소 거리입니다. 연결된 타일 한 칸을 거리 1로 계산합니다.")]
        [Min(1)] [SerializeField] private int minimumEliteLegDistance = 10;

        [Tooltip("현재 기준 위치에서 다음 중간 보스 후보까지 허용할 최대 거리입니다. 연결된 타일 한 칸을 거리 1로 계산합니다.")]
        [Min(1)] [SerializeField] private int maximumEliteLegDistance = 18;

        [Tooltip("마지막 중간 보스 위치에서 최종 보스 후보까지 확보할 최소 거리입니다. 연결된 타일 한 칸을 거리 1로 계산합니다.")]
        [Min(1)] [SerializeField] private int minimumBossLegDistance = 10;

        [Tooltip("남은 중간 보스와 최종 보스를 배치할 구간이 부족한 후보를 피하기 위해 확보할 거리의 비율입니다.")]
        [Range(0f, 1f)] [SerializeField] private float futureRouteReserveRatio = 0.6f;

        [Header("Route Scoring")]
        [Tooltip("시작점에서 최종 보스 방향으로 전진한 후보에 부여하는 점수 가중치입니다.")]
        [Min(0f)] [SerializeField] private float forwardProgressWeight = 1.5f;
        [Tooltip("주변 이동 가능 타일과 갈림길이 많은 후보에 부여하는 탐험 점수 가중치입니다.")]
        [Min(0f)] [SerializeField] private float explorationOpportunityWeight = 1.2f;
        [Tooltip("후보 주변에 아직 방문하지 않은 타일이 많을수록 추가하는 점수의 가중치입니다. 0이면 미방문 지역 점수를 사용하지 않습니다.")]
        [Min(0f)] [SerializeField] private float unvisitedRegionWeight = 1f;
        [Tooltip("미방문 지역 점수를 계산할 때 후보 타일에서 몇 칸 거리까지 조사할지 정합니다.")]
        [Min(1)] [SerializeField] private int unvisitedRegionRadius = 3;
        [Tooltip("중간 보스가 주 경로의 좌우에 번갈아 배치되도록 부여하는 점수 가중치입니다.")]
        [Min(0f)] [SerializeField] private float sideAlternationWeight = 2f;
        [Tooltip("현재 위치에서 후보를 거쳐 최종 보스로 가는 경로가 허용할 최대 우회 비율입니다.")]
        [Min(1f)] [SerializeField] private float maximumDetourRatio = 1.6f;

        public int RequiredEliteCount => requiredEliteCount;
        public FirstEliteSpawnOrigin FirstEliteSpawnOrigin => firstEliteSpawnOrigin;

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
                unvisitedRegionWeight = unvisitedRegionWeight,
                unvisitedRegionRadius = Mathf.Max(1, unvisitedRegionRadius),
                sideAlternationWeight = sideAlternationWeight,
                maximumDetourRatio = maximumDetourRatio,
            };
        }
    }
}
