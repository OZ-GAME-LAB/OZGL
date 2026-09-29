using System.Collections.Generic;
using OzGameLab01.Data;

namespace OzGameLab01.Player
{
    /// <summary>
    /// 플레이어가 보유한 유닛 목록을 보관하는 순수 데이터 클래스입니다.
    /// </summary>
    public class PlayerState
    {
        private readonly List<UnitData> _ownedUnits = new List<UnitData>();

        public IReadOnlyList<UnitData> OwnedUnits => _ownedUnits;

        public bool TryAddUnit(UnitData unit)
        {
            if (unit == null)
            {
                return false;
            }

            for (int unitIndex = 0; unitIndex < _ownedUnits.Count; unitIndex++)
            {
                UnitData ownedUnit = _ownedUnits[unitIndex];
                if (ownedUnit != null && ownedUnit.id == unit.id)
                {
                    return false;
                }
            }

            _ownedUnits.Add(unit);
            return true;
        }

        public void Clear()
        {
            _ownedUnits.Clear();
        }
    }
}
