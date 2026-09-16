using NUnit.Framework;
using OzGameLab01.Formation;

namespace OzGameLab01.Tests.EditMode
{
    public class FormationSlotStateTests
    {
        [Test]
        public void PlaceInEmptyBattleSlot_Succeeds_AndIncrementsCount()
        {
            var state = new FormationSlotState();
            var result = state.TryDrop(FormationSlotKind.Battle, handle: 1, targetIndex: 0);

            Assert.That(result.Outcome, Is.EqualTo(FormationDropOutcome.PlacedInEmpty));
            Assert.That(state.BattleAt(0), Is.EqualTo(1));
            Assert.That(state.BattleUnitCount, Is.EqualTo(1));
        }

        [Test]
        public void PlaceInEmptyBattleSlot_RejectsBeyondMaxCount()
        {
            var state = new FormationSlotState();
            for (int i = 0; i < FormationSlotState.MAX_BATTLE_UNIT_COUNT; i++)
            {
                Assert.That(state.TryDrop(FormationSlotKind.Battle, handle: i, targetIndex: i).Success, Is.True);
            }

            var overflow = state.TryDrop(FormationSlotKind.Battle, handle: 99, targetIndex: FormationSlotState.MAX_BATTLE_UNIT_COUNT);
            Assert.That(overflow.Outcome, Is.EqualTo(FormationDropOutcome.Rejected));
            Assert.That(state.BattleUnitCount, Is.EqualTo(FormationSlotState.MAX_BATTLE_UNIT_COUNT));
        }

        [Test]
        public void SupportSlot_BothFull_NewUnitReplacesRatherThanRejected()
        {
            // Support의 최대 인원(2)이 슬롯 수(2)와 같아서, 슬롯이 다 차 있으면
            // "빈 슬롯이 없다"는 이유로 항상 Replaced가 되고 count 초과 거부 분기는 타지 않는다.
            var state = new FormationSlotState();
            Assert.That(state.TryDrop(FormationSlotKind.Support, 1, 0).Success, Is.True);
            Assert.That(state.TryDrop(FormationSlotKind.Support, 2, 1).Success, Is.True);

            var result = state.TryDrop(FormationSlotKind.Support, 3, 0);
            Assert.That(result.Outcome, Is.EqualTo(FormationDropOutcome.Replaced));
            Assert.That(state.SupportUnitCount, Is.EqualTo(FormationSlotState.SUPPORT_SLOT_COUNT));
        }

        [Test]
        public void SameSlotDrop_ReturnsRepositionedWithoutStateChange()
        {
            var state = new FormationSlotState();
            state.TryDrop(FormationSlotKind.Battle, 1, 0);

            var result = state.TryDrop(FormationSlotKind.Battle, 1, 0);
            Assert.That(result.Outcome, Is.EqualTo(FormationDropOutcome.Repositioned));
            Assert.That(state.BattleAt(0), Is.EqualTo(1));
            Assert.That(state.BattleUnitCount, Is.EqualTo(1));
        }

        [Test]
        public void MoveToEmptySlot_ClearsSourceAndKeepsCount()
        {
            var state = new FormationSlotState();
            state.TryDrop(FormationSlotKind.Battle, 1, 0);

            var result = state.TryDrop(FormationSlotKind.Battle, 1, 3);
            Assert.That(result.Outcome, Is.EqualTo(FormationDropOutcome.MovedToEmpty));
            Assert.That(result.SourceIndex, Is.EqualTo(0));
            Assert.That(state.BattleAt(0), Is.Null);
            Assert.That(state.BattleAt(3), Is.EqualTo(1));
            Assert.That(state.BattleUnitCount, Is.EqualTo(1));
        }

        [Test]
        public void ReplaceOccupiedSlotFromList_DisplacesExistingAndKeepsCountSame()
        {
            var state = new FormationSlotState();
            state.TryDrop(FormationSlotKind.Battle, 1, 0);

            var result = state.TryDrop(FormationSlotKind.Battle, 2, 0);
            Assert.That(result.Outcome, Is.EqualTo(FormationDropOutcome.Replaced));
            Assert.That(result.DisplacedHandle, Is.EqualTo(1));
            Assert.That(state.BattleAt(0), Is.EqualTo(2));
            Assert.That(state.BattleUnitCount, Is.EqualTo(1));
        }

        [Test]
        public void SwapTwoOccupiedSlots_ExchangesHandlesAndKeepsCount()
        {
            var state = new FormationSlotState();
            state.TryDrop(FormationSlotKind.Battle, 1, 0);
            state.TryDrop(FormationSlotKind.Battle, 2, 1);

            var result = state.TryDrop(FormationSlotKind.Battle, 1, 1);
            Assert.That(result.Outcome, Is.EqualTo(FormationDropOutcome.Swapped));
            Assert.That(result.DisplacedHandle, Is.EqualTo(2));
            Assert.That(state.BattleAt(0), Is.EqualTo(2));
            Assert.That(state.BattleAt(1), Is.EqualTo(1));
            Assert.That(state.BattleUnitCount, Is.EqualTo(2));
        }

        [Test]
        public void UnitAlreadyInSupportSlot_CannotBeDroppedOnBattleSlot()
        {
            var state = new FormationSlotState();
            state.TryDrop(FormationSlotKind.Support, 1, 0);

            var result = state.TryDrop(FormationSlotKind.Battle, 1, 0);
            Assert.That(result.Outcome, Is.EqualTo(FormationDropOutcome.Rejected));
            Assert.That(state.FindSupportSlot(1), Is.EqualTo(0));
        }

        [Test]
        public void RemoveBattle_ClearsSlotAndDecrementsCount()
        {
            var state = new FormationSlotState();
            state.TryDrop(FormationSlotKind.Battle, 1, 0);

            int? removed = state.RemoveBattle(0);
            Assert.That(removed, Is.EqualTo(1));
            Assert.That(state.BattleAt(0), Is.Null);
            Assert.That(state.BattleUnitCount, Is.EqualTo(0));
        }

        [Test]
        public void RemoveBattle_OnEmptySlot_ReturnsNull()
        {
            var state = new FormationSlotState();
            Assert.That(state.RemoveBattle(0), Is.Null);
        }

        [Test]
        public void TryPlaceFirstEmptyBattle_FillsSlotsInOrder()
        {
            var state = new FormationSlotState();
            state.TryDrop(FormationSlotKind.Battle, 1, 0);

            bool placed = state.TryPlaceFirstEmptyBattle(2, out int index);
            Assert.That(placed, Is.True);
            Assert.That(index, Is.EqualTo(1));
            Assert.That(state.BattleAt(1), Is.EqualTo(2));
        }

        [Test]
        public void TryPlaceFirstEmptyBattle_RejectsWhenFull()
        {
            var state = new FormationSlotState();
            for (int i = 0; i < FormationSlotState.MAX_BATTLE_UNIT_COUNT; i++)
            {
                state.TryDrop(FormationSlotKind.Battle, i, i);
            }

            bool placed = state.TryPlaceFirstEmptyBattle(99, out int index);
            Assert.That(placed, Is.False);
            Assert.That(index, Is.EqualTo(-1));
        }

        [Test]
        public void InvalidTargetIndex_IsRejected()
        {
            var state = new FormationSlotState();
            Assert.That(state.TryDrop(FormationSlotKind.Battle, 1, -1).Success, Is.False);
            Assert.That(state.TryDrop(FormationSlotKind.Battle, 1, FormationSlotState.BATTLE_SLOT_COUNT).Success, Is.False);
        }
    }
}
