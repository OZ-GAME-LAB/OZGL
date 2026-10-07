using NUnit.Framework;
using UnityEngine;
using OzGameLab01.Board.Models;

namespace OzGameLab01.Tests.EditMode
{
    public class BoardRunStateGrowthTests
    {
        private static BoardRunState CreateActive()
        {
            var state = new BoardRunState();
            state.BeginNewRun(1);
            return state;
        }

        [Test]
        public void AdvanceTurnAccumulatesDayAndNightIncrements()
        {
            var state = CreateActive();
            Assert.That(state.EnemyGrowthValue, Is.EqualTo(0.7f).Within(0.0001f));

            state.AdvanceTurn(wasNightTurn: false);
            Assert.That(state.EnemyGrowthValue, Is.EqualTo(0.77f).Within(0.0001f));

            state.AdvanceTurn(wasNightTurn: true);
            Assert.That(state.EnemyGrowthValue, Is.EqualTo(0.89f).Within(0.0001f));
            Assert.That(state.EnemyOverturnValue, Is.EqualTo(0f));
            Assert.That(state.IsInEnemyOverturn, Is.False);
        }

        [Test]
        public void CycleBoundaryWithoutEliteKillEntersOverturnAndFreezesGrowth()
        {
            var state = CreateActive();
            state.AdvanceTurn(wasNightTurn: false); // 0.77

            // 중간보스를 못 잡은 채 사이클(웨이브)이 끝났다고 알린다.
            state.RegisterEnemyGrowthCycleBoundary();
            Assert.That(state.IsInEnemyOverturn, Is.True);

            float frozenValue = state.EnemyGrowthValue;
            state.AdvanceTurn(wasNightTurn: false); // 오버턴 중 — growth는 멈추고 overturn만 오른다.
            Assert.That(state.EnemyGrowthValue, Is.EqualTo(frozenValue));
            Assert.That(state.EnemyOverturnValue, Is.EqualTo(0.3f).Within(0.0001f));

            state.AdvanceTurn(wasNightTurn: true); // 여전히 오버턴 — 낮/밤 무관하게 0.3만 누적.
            Assert.That(state.EnemyGrowthValue, Is.EqualTo(frozenValue));
            Assert.That(state.EnemyOverturnValue, Is.EqualTo(0.6f).Within(0.0001f));
        }

        [Test]
        public void CycleBoundaryWithEliteKillStaysOutOfOverturn()
        {
            var state = CreateActive();
            state.AdvanceTurn(wasNightTurn: false); // 0.77

            state.BeginBattle(Vector2Int.zero, isBossBattle: false, isEliteBattle: true);
            state.CompleteCurrentBattle();

            state.RegisterEnemyGrowthCycleBoundary();
            Assert.That(state.IsInEnemyOverturn, Is.False, "사이클 안에서 중간보스를 잡았으면 오버턴에 들어가지 않아야 한다.");
        }

        [Test]
        public void EliteKillClearsOverturnAndJumpsGrowthValue()
        {
            var state = CreateActive();
            state.AdvanceTurn(wasNightTurn: false); // 0.77
            state.RegisterEnemyGrowthCycleBoundary(); // 오버턴 진입
            state.AdvanceTurn(wasNightTurn: false); // overturn 0.3 누적
            Assert.That(state.IsInEnemyOverturn, Is.True);

            float growthBeforeKill = state.EnemyGrowthValue;
            state.BeginBattle(Vector2Int.zero, isBossBattle: false, isEliteBattle: true);
            state.CompleteCurrentBattle();

            Assert.That(state.IsInEnemyOverturn, Is.False);
            Assert.That(state.EnemyOverturnValue, Is.EqualTo(0f));
            Assert.That(state.EnemyGrowthValue, Is.EqualTo(growthBeforeKill + 1f).Within(0.0001f));
        }

        [Test]
        public void EliteKillWithoutOverturnStillGrantsGrowthJump()
        {
            // 오버턴에 들어가기 전(제때 처치)에도 중간보스 처치는 +1을 준다.
            var state = CreateActive();
            state.AdvanceTurn(wasNightTurn: false); // 0.77

            state.BeginBattle(Vector2Int.zero, isBossBattle: false, isEliteBattle: true);
            state.CompleteCurrentBattle();

            Assert.That(state.EnemyGrowthValue, Is.EqualTo(1.77f).Within(0.0001f));
        }

        [Test]
        public void CaptureRestoreRoundTripPreservesGrowthState()
        {
            var state = CreateActive();
            state.AdvanceTurn(wasNightTurn: false);
            state.RegisterEnemyGrowthCycleBoundary();
            state.AdvanceTurn(wasNightTurn: true);

            BoardRunSnapshot snapshot = state.Capture();
            var restored = new BoardRunState();
            bool success = restored.Restore(snapshot);

            Assert.That(success, Is.True);
            Assert.That(restored.EnemyGrowthValue, Is.EqualTo(state.EnemyGrowthValue).Within(0.0001f));
            Assert.That(restored.EnemyOverturnValue, Is.EqualTo(state.EnemyOverturnValue).Within(0.0001f));
            Assert.That(restored.IsInEnemyOverturn, Is.EqualTo(state.IsInEnemyOverturn));
        }

        [Test]
        public void RestoreMigratesPreExistingSavesLackingGrowthFields()
        {
            // enemyGrowthValue가 0인 건 이 필드가 생기기 전(구버전) 세이브를 뜻한다 —
            // turnCount/defeatedElitesCount만으로 근사치를 다시 계산해야 한다.
            var snapshot = new BoardRunSnapshot
            {
                hasActiveRun = true,
                mapSeed = 1,
                turnCount = 10,
                defeatedElitesCount = 0,
                enemyGrowthValue = 0f
            };

            var state = new BoardRunState();
            bool success = state.Restore(snapshot);

            Assert.That(success, Is.True);
            Assert.That(state.EnemyGrowthValue, Is.EqualTo(EnemyGrowthSaveMigration.EstimateValue(10, 0)).Within(0.0001f));
            Assert.That(state.EnemyOverturnValue, Is.EqualTo(0f));
            Assert.That(state.IsInEnemyOverturn, Is.False);
        }
    }
}
