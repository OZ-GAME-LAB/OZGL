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

        public void AddUnit(UnitData unit)
        {
            _ownedUnits.Add(unit);
        }

        public void Clear()
        {
            _ownedUnits.Clear();
        }
    }
}
