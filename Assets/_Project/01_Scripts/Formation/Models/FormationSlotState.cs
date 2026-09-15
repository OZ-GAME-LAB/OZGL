namespace OzGameLab01.Formation
{
    public enum FormationSlotKind { Battle, Support }

    public enum FormationDropOutcome
    {
        Rejected,
        Repositioned,
        PlacedInEmpty,
        MovedToEmpty,
        Replaced,
        Swapped
    }

    public readonly struct FormationDropResult
    {
        public FormationDropOutcome Outcome { get; }
        public int SourceIndex { get; }
        public int TargetIndex { get; }

        /// <summary>
        /// Replaced일 때는 목록으로 돌아간 기존 슬롯 유닛, Swapped일 때는
        /// 원래 슬롯으로 옮겨간 유닛의 핸들입니다. 그 외에는 null입니다.
        /// </summary>
        public int? DisplacedHandle { get; }

        public bool Success => Outcome != FormationDropOutcome.Rejected;

        public FormationDropResult(FormationDropOutcome outcome, int sourceIndex, int targetIndex, int? displacedHandle)
        {
            Outcome = outcome;
            SourceIndex = sourceIndex;
            TargetIndex = targetIndex;
            DisplacedHandle = displacedHandle;
        }

        internal static FormationDropResult Rejected() =>
            new FormationDropResult(FormationDropOutcome.Rejected, -1, -1, null);
    }

    /// <summary>
    /// 전투(9칸)/서브(2칸) 슬롯 배치 상태와 배치 규칙을 보관하는 순수 데이터 클래스입니다.
    /// 실제로 배치되는 유닛은 정수 핸들(보유 목록 인덱스 등 호출자가 정한 안정적인 ID)로만
    /// 식별하고, View(UnitItemView 등) 타입은 전혀 알지 못합니다.
    /// </summary>
    public sealed class FormationSlotState
    {
        public const int BattleSlotCount = 9;
        public const int MaxBattleUnitCount = 4;
        public const int SupportSlotCount = 2;

        private readonly int?[] _battleSlots = new int?[BattleSlotCount];
        private readonly int?[] _supportSlots = new int?[SupportSlotCount];

        public int BattleUnitCount { get; private set; }
        public int SupportUnitCount { get; private set; }

        public static bool IsValidBattleSlot(int index) => index >= 0 && index < BattleSlotCount;
        public static bool IsValidSupportSlot(int index) => index >= 0 && index < SupportSlotCount;

        public int? BattleAt(int index) => IsValidBattleSlot(index) ? _battleSlots[index] : null;
        public int? SupportAt(int index) => IsValidSupportSlot(index) ? _supportSlots[index] : null;

        public int FindBattleSlot(int handle) => FindSlot(_battleSlots, handle);
        public int FindSupportSlot(int handle) => FindSlot(_supportSlots, handle);

        private static int FindSlot(int?[] slots, int handle)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == handle)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 지정한 슬롯 종류의 targetIndex에 handle을 드롭한 결과를 계산하고 상태에 반영합니다.
        /// 이미 반대 종류(전투/서브) 슬롯에 배치되어 있는 유닛은 거부합니다 — 반대쪽에서 먼저
        /// 빼야 합니다.
        /// </summary>
        public FormationDropResult TryDrop(FormationSlotKind kind, int handle, int targetIndex)
        {
            int?[] slots = kind == FormationSlotKind.Battle ? _battleSlots : _supportSlots;

            if (targetIndex < 0 || targetIndex >= slots.Length)
            {
                return FormationDropResult.Rejected();
            }

            int otherKindIndex = kind == FormationSlotKind.Battle
                ? FindSupportSlot(handle)
                : FindBattleSlot(handle);
            if (otherKindIndex >= 0)
            {
                return FormationDropResult.Rejected();
            }

            int sourceIndex = FindSlot(slots, handle);
            int? targetHandle = slots[targetIndex];

            if (sourceIndex == targetIndex)
            {
                return new FormationDropResult(FormationDropOutcome.Repositioned, sourceIndex, targetIndex, null);
            }

            if (sourceIndex < 0 && targetHandle == null)
            {
                int count = kind == FormationSlotKind.Battle ? BattleUnitCount : SupportUnitCount;
                int max = kind == FormationSlotKind.Battle ? MaxBattleUnitCount : SupportSlotCount;
                if (count >= max)
                {
                    return FormationDropResult.Rejected();
                }

                slots[targetIndex] = handle;
                SetCount(kind, count + 1);
                return new FormationDropResult(FormationDropOutcome.PlacedInEmpty, sourceIndex, targetIndex, null);
            }

            if (sourceIndex >= 0 && targetHandle == null)
            {
                slots[sourceIndex] = null;
                slots[targetIndex] = handle;
                return new FormationDropResult(FormationDropOutcome.MovedToEmpty, sourceIndex, targetIndex, null);
            }

            if (sourceIndex < 0 && targetHandle != null)
            {
                slots[targetIndex] = handle;
                return new FormationDropResult(FormationDropOutcome.Replaced, sourceIndex, targetIndex, targetHandle);
            }

            // sourceIndex >= 0 && targetHandle != null
            slots[sourceIndex] = targetHandle;
            slots[targetIndex] = handle;
            return new FormationDropResult(FormationDropOutcome.Swapped, sourceIndex, targetIndex, targetHandle);
        }

        /// <summary>
        /// 비어 있는 전투 슬롯을 앞에서부터 찾아 handle을 배치합니다.
        /// </summary>
        public bool TryPlaceFirstEmptyBattle(int handle, out int placedIndex)
        {
            if (BattleUnitCount >= MaxBattleUnitCount)
            {
                placedIndex = -1;
                return false;
            }

            for (int i = 0; i < _battleSlots.Length; i++)
            {
                if (_battleSlots[i] == null)
                {
                    _battleSlots[i] = handle;
                    SetCount(FormationSlotKind.Battle, BattleUnitCount + 1);
                    placedIndex = i;
                    return true;
                }
            }

            placedIndex = -1;
            return false;
        }

        public int? RemoveBattle(int index)
        {
            if (!IsValidBattleSlot(index) || _battleSlots[index] == null)
            {
                return null;
            }

            int handle = _battleSlots[index].Value;
            _battleSlots[index] = null;
            SetCount(FormationSlotKind.Battle, BattleUnitCount - 1);
            return handle;
        }

        public int? RemoveSupport(int index)
        {
            if (!IsValidSupportSlot(index) || _supportSlots[index] == null)
            {
                return null;
            }

            int handle = _supportSlots[index].Value;
            _supportSlots[index] = null;
            SetCount(FormationSlotKind.Support, SupportUnitCount - 1);
            return handle;
        }

        private void SetCount(FormationSlotKind kind, int value)
        {
            if (kind == FormationSlotKind.Battle)
            {
                BattleUnitCount = value;
            }
            else
            {
                SupportUnitCount = value;
            }
        }
    }
}
