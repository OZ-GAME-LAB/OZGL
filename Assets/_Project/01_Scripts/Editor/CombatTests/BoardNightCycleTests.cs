using System.Collections.Generic;
using NUnit.Framework;
using OzGameLab01.Board.Models;
using OzGameLab01.Map;
using UnityEngine;

namespace OzGameLab01.Tests.EditMode
{
    public sealed class BoardNightCycleTests
    {
        [Test]
        public void ActiveMidBossKeepsNightUntilDefeated()
        {
            Assert.That(GetTime(0, 0, false), Is.EqualTo(BoardTimeOfDay.Day));
            Assert.That(GetTime(8, 0, false), Is.EqualTo(BoardTimeOfDay.Noon));
            Assert.That(GetTime(10, 0, false), Is.EqualTo(BoardTimeOfDay.Night));
            Assert.That(GetTime(15, 0, false), Is.EqualTo(BoardTimeOfDay.Day));
            Assert.That(GetTime(15, 0, true), Is.EqualTo(BoardTimeOfDay.Night));
            Assert.That(BoardTurnRules.TurnsUntilRunPhase(10, 0, true, 8, 2, 5), Is.EqualTo(5));
            Assert.That(BoardTurnRules.TurnsUntilRunPhase(15, 0, true, 8, 2, 5), Is.Zero);
        }

        [Test]
        public void MidBossDefeatStartsANewMorningCycleAndPersistsState()
        {
            var state = new BoardRunState();
            state.BeginNewRun(1234);
            for (int i = 0; i < 10; i++)
            {
                state.AdvanceTurn();
            }

            Vector2Int objective = new Vector2Int(3, 7);
            state.SaveObjectivePosition(objective);
            state.ActivateMidBoss();

            var restored = new BoardRunState();
            Assert.That(restored.Restore(state.Capture()), Is.True);
            Assert.That(restored.IsMidBossActive, Is.True);
            Assert.That(restored.HasObjective, Is.True);
            Assert.That(restored.ObjectivePosition, Is.EqualTo(objective));

            restored.BeginBattle(objective, isBossBattle: false, isEliteBattle: true);
            restored.CompleteCurrentBattle();

            Assert.That(restored.IsMidBossActive, Is.False);
            Assert.That(restored.TimeCycleStartTurn, Is.EqualTo(10));
            Assert.That(restored.DefeatedElitesCount, Is.EqualTo(1));
            Assert.That(GetTime(restored.TurnCount, restored.TimeCycleStartTurn, false),
                Is.EqualTo(BoardTimeOfDay.Day));
            Assert.That(GetTime(20, restored.TimeCycleStartTurn, false),
                Is.EqualTo(BoardTimeOfDay.Night));
        }

        [Test]
        public void PlayerVisitHistoryPersistsAcrossRunRestore()
        {
            var state = new BoardRunState();
            state.BeginNewRun(1234);
            Vector2Int first = new Vector2Int(1, 2);
            Vector2Int second = new Vector2Int(2, 2);

            state.SavePlayerPosition(first);
            state.SavePlayerPosition(second);

            var restored = new BoardRunState();
            Assert.That(restored.Restore(state.Capture()), Is.True);
            Assert.That(restored.VisitedPositions, Does.Contain(first));
            Assert.That(restored.VisitedPositions, Does.Contain(second));
        }

        [Test]
        public void EliteScoringPrefersCandidateSurroundedByUnvisitedTiles()
        {
            MapNode start = CreateNode(0, 0, NodeType.Start);
            MapNode leftPath = CreateNode(-1, 0);
            MapNode visitedCandidate = CreateNode(-2, 0);
            MapNode rightPath = CreateNode(1, 0);
            MapNode unvisitedCandidate = CreateNode(2, 0);
            MapNode boss = CreateNode(0, 1);

            Connect(start, leftPath);
            Connect(leftPath, visitedCandidate);
            Connect(visitedCandidate, boss);
            Connect(start, rightPath);
            Connect(rightPath, unvisitedCandidate);
            Connect(unvisitedCandidate, boss);

            var nodes = new Dictionary<Vector2Int, MapNode>
            {
                [start.Position] = start,
                [leftPath.Position] = leftPath,
                [visitedCandidate.Position] = visitedCandidate,
                [rightPath.Position] = rightPath,
                [unvisitedCandidate.Position] = unvisitedCandidate,
                [boss.Position] = boss
            };
            var settings = new BoardRouteSettings
            {
                requiredEliteCount = 3,
                minimumEliteLegDistance = 2,
                maximumEliteLegDistance = 2,
                minimumBossLegDistance = 1,
                futureRouteReserveRatio = 0f,
                forwardProgressWeight = 0f,
                explorationOpportunityWeight = 0f,
                unvisitedRegionWeight = 1f,
                unvisitedRegionRadius = 1,
                sideAlternationWeight = 0f,
                maximumDetourRatio = 2f
            };
            Vector2Int[] visited =
            {
                start.Position,
                leftPath.Position,
                visitedCandidate.Position,
                boss.Position
            };
            var model = new BoardRouteModel(nodes, new Vector2Int[0], visited, 0, settings);

            MapNode selected = model.SelectEliteNode(
                start,
                start,
                boss,
                model.BuildDistanceMap(start));

            Assert.That(selected, Is.SameAs(unvisitedCandidate));
        }

        private static BoardTimeOfDay GetTime(int turn, int cycleStart, bool locked)
        {
            return BoardTurnRules.GetRunTimeOfDay(
                turn,
                cycleStart,
                locked,
                morningTurns: 8,
                lunchTurns: 2,
                eveningTurns: 5);
        }

        private static MapNode CreateNode(int x, int y, NodeType type = NodeType.Normal)
        {
            return new MapNode
            {
                Position = new Vector2Int(x, y),
                Type = type
            };
        }

        private static void Connect(MapNode first, MapNode second)
        {
            first.ConnectedNodes.Add(second);
            second.ConnectedNodes.Add(first);
        }
    }
}
