using System;
using System.Collections.Generic;
using OzGameLab01.Map;

namespace OzGameLab01.Board.Models
{
    /// <summary>
    /// 화면과 싱글톤 참조 없이 보드 이동 상태와 경로를 관리합니다.
    /// </summary>
    public sealed class BoardMovementModel
    {
        private readonly IBoardMovementState _state;
        private List<MapNode> _path;
        private int _stepIndex;

        public MapNode CurrentNode { get; private set; }
        public bool IsMoving { get; private set; }
        public int RemainingDiceValue => _state.RemainingDiceValue;

        public BoardMovementModel(IBoardMovementState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public void Setup(MapNode node)
        {
            CurrentNode = node;
            CancelMovement();
        }

        public void SetRemainingDiceValue(int value)
        {
            _state.SetRemainingDiceValue(Math.Max(0, value));
        }

        public List<MapNode> FindPath(MapNode target)
        {
            if (IsMoving)
            {
                return null;
            }
            return BoardPathfinder.FindPath(CurrentNode, target, RemainingDiceValue);
        }

        public IReadOnlyList<MapNode> GetReachableNodes()
        {
            return IsMoving ? Array.Empty<MapNode>() : BoardPathfinder.GetReachableNodes(CurrentNode, RemainingDiceValue);
        }

        public bool TryBeginMovement(MapNode target, out IReadOnlyList<MapNode> path)
        {
            path = null;
            List<MapNode> candidate = FindPath(target);
            if (candidate == null || candidate.Count == 0)
            {
                return false;
            }
            _path = candidate;
            _stepIndex = 0;
            IsMoving = true;
            path = candidate.AsReadOnly();
            return true;
        }

        public bool CompleteStep(MapNode node)
        {
            if (!IsMoving || _path == null || _stepIndex >= _path.Count || _path[_stepIndex] != node)
            {
                return false;
            }
            CurrentNode = node;
            _stepIndex++;
            SetRemainingDiceValue(RemainingDiceValue - 1);
            return true;
        }

        public void CancelMovement()
        {
            IsMoving = false;
            _path = null;
            _stepIndex = 0;
        }

        public bool TryEndTurn(out int unusedActionPoints)
        {
            unusedActionPoints = 0;
            if (IsMoving)
            {
                return false;
            }
            unusedActionPoints = RemainingDiceValue;
            SetRemainingDiceValue(0);
            return true;
        }
    }
}
