using System.Collections.Generic;
using NUnit.Framework;
using OzGameLab01.Board.Models;
using OzGameLab01.Map;

namespace OzGameLab01.Tests.EditMode
{
    public class BoardMovementRangeTests
    {
        private sealed class State : IBoardMovementState
        {
            public int RemainingDiceValue { get; private set; }
            public void SetRemainingDiceValue(int value) => RemainingDiceValue = value;
        }

        [Test]
        public void RangeMatchesPathfindingAcrossCyclesObstaclesAndDisconnectedNodes()
        {
            var start = new MapNode();
            var first = new MapNode();
            var second = new MapNode();
            var third = new MapNode();
            var blocked = new MapNode { Type = NodeType.Tree };
            var behindObstacle = new MapNode();
            var isolated = new MapNode();
            start.ConnectedNodes.AddRange(new[] { first, first, blocked, null });
            first.ConnectedNodes.AddRange(new[] { start, second });
            second.ConnectedNodes.AddRange(new[] { first, third });
            third.ConnectedNodes.Add(second);
            blocked.ConnectedNodes.Add(behindObstacle);
            var nodes = new[] { start, first, second, third, blocked, behindObstacle, isolated };

            for (int budget = 0; budget <= 4; budget++)
            {
                var range = BoardPathfinder.GetReachableNodes(start, budget);
                Assert.That(new HashSet<MapNode>(range).Count, Is.EqualTo(range.Count));
                foreach (MapNode node in nodes)
                {
                    bool canMove = BoardPathfinder.FindPath(start, node, budget) != null;
                    Assert.That(new HashSet<MapNode>(range).Contains(node), Is.EqualTo(canMove));
                }
            }
            Assert.That(BoardPathfinder.GetReachableNodes(null, 3), Is.Empty);
            Assert.That(BoardPathfinder.GetReachableNodes(start, -1), Is.Empty);
        }

        [Test]
        public void RangeTracksMovementCancellationAndTurnEndWithoutMapManager()
        {
            var start = new MapNode();
            var first = new MapNode();
            var second = new MapNode();
            start.ConnectedNodes.Add(first);
            first.ConnectedNodes.AddRange(new[] { start, second });
            second.ConnectedNodes.Add(first);
            var model = new BoardMovementModel(new State());
            model.SetRemainingDiceValue(2);
            Assert.That(model.GetReachableNodes(), Is.Empty);
            model.Setup(start);
            var snapshot = model.GetReachableNodes();
            Assert.That(snapshot, Is.EquivalentTo(new[] { first, second }));

            Assert.That(model.TryBeginMovement(second, out _), Is.True);
            Assert.That(model.GetReachableNodes(), Is.Empty);
            Assert.That(model.CompleteStep(first), Is.True);
            model.CancelMovement();
            Assert.That(model.GetReachableNodes(), Is.EquivalentTo(new[] { start, second }));
            Assert.That(snapshot, Is.EquivalentTo(new[] { first, second }));
            Assert.That(model.TryEndTurn(out int unused), Is.True);
            Assert.That(unused, Is.EqualTo(1));
            Assert.That(model.GetReachableNodes(), Is.Empty);
        }
    }
}
