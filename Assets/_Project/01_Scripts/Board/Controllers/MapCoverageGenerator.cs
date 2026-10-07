using OzGameLab01.Board.Models;
using System.Linq;
using UnityEngine;

namespace OzGameLab01.Map
{
    /// <summary>
    /// 기본 맵 생성 이후 상호작용 공백 보정의 실행 조율
    /// 거리 구간과 시작점에서 멀리 뻗은 분기 경로에 전투 또는 이벤트를 추가 배치합니다.
    /// </summary>
    [AddComponentMenu("OZGL/Map/Map Coverage Generator")]
    public sealed class MapCoverageGenerator : MapGenerator
    {
        [Header("Interaction Coverage")]
        [Tooltip("비활성화하면 기본 맵 생성 프로필의 배치 결과를 그대로 사용하고 상호작용 타일을 추가하지 않습니다.")]
        [SerializeField] private bool enableInteractionCoverage = true;

        [Tooltip("상호작용 타일 없이 허용하는 최대 이동 칸 수입니다.")]
        [Min(1)] [SerializeField] private int maximumStepsWithoutInteraction = 8;

        [Tooltip("이 거리 이상 떨어진 주요 분기 경로만 보정 대상으로 사용합니다.")]
        [Min(1)] [SerializeField] private int minimumBranchDepth = 10;

        [Tooltip("한 맵에서 검사할 가장 먼 분기 경로의 최대 수입니다.")]
        [Min(1)] [SerializeField] private int maximumBranchPaths = 16;

        [Tooltip("보정으로 추가되는 상호작용 타일 중 Event가 될 비율입니다. 나머지는 Battle입니다.")]
        [Range(0f, 1f)] [SerializeField] private float eventTileRatio = 0.6f;

        protected override void ApplyPostGenerationRules()
        {
            if (!enableInteractionCoverage)
            {
                return;
            }

            MapNode startNode = AllNodes.FirstOrDefault(node => node.Type == NodeType.Start);
            if (startNode == null)
            {
                Debug.LogWarning("[MapCoverageGenerator] Start 노드가 없어 커버리지 보정을 건너뜁니다.", this);
                return;
            }

            // 커버리지 배치는 소비 이력과 무관하게 MapSeed만으로 최초 맵과 동일하게
            // 재현합니다. 소비된 타일을 여기서 제외하면 전투 복귀 시 빈 구간을 채우기
            // 위해 다른 Normal 타일이 Event/Battle로 새로 승격됩니다.
            // 보정이 끝난 뒤 MapGenerator.ApplyConsumedSpecialTiles()가 기존 소비 타일만
            // Normal로 복원합니다.
            var model = new BoardCoverageModel(
                maximumStepsWithoutInteraction,
                minimumBranchDepth,
                maximumBranchPaths,
                eventTileRatio,
                System.Array.Empty<Vector2Int>());
            model.Repair(startNode, out int repairedBands, out int repairedBranches);

            Debug.Log(
                $"[MapCoverageGenerator] 콘텐츠 커버리지 보정 완료 | " +
                $"거리 구간: {repairedBands}, 분기 경로: {repairedBranches}",
                this);
        }

    }
}
