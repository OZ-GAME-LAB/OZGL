using System.Collections.Generic;
using OzGameLab01.Board.Models;
using System.Linq;
using OzGameLab01.Data;
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
            MapNode startNode = AllNodes.FirstOrDefault(node => node.Type == NodeType.Start);
            if (startNode == null)
            {
                Debug.LogWarning("[MapCoverageGenerator] Start 노드가 없어 커버리지 보정을 건너뜁니다.", this);
                return;
            }

            List<Vector2Int> consumed = new List<Vector2Int>();
            foreach (MapNode node in AllNodes)
            {
                if (BoardRunData.IsSpecialTileConsumed(node.Position)) { consumed.Add(node.Position); }
            }
            var model = new BoardCoverageModel(maximumStepsWithoutInteraction, minimumBranchDepth, maximumBranchPaths, eventTileRatio, consumed);
            model.Repair(startNode, out int repairedBands, out int repairedBranches);

            Debug.Log(
                $"[MapCoverageGenerator] 콘텐츠 커버리지 보정 완료 | " +
                $"거리 구간: {repairedBands}, 분기 경로: {repairedBranches}",
                this);
        }

    }
}
