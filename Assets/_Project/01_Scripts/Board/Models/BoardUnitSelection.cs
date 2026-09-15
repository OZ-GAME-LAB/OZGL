using System;
using System.Collections.Generic;
using OzGameLab01.Data;
namespace OzGameLab01.Board.Models
{
    // 표시 리소스 우선 및 전체 후보 대체 규칙
    public static class BoardUnitSelection
    {
        public static UnitData Select(IReadOnlyList<UnitData> units, Func<int, int> chooseIndex)
        {
            if (units == null) { return null; }
            var candidates = new List<UnitData>();
            foreach (UnitData unit in units)
            {
                if (unit != null && !string.IsNullOrEmpty(unit.spriteAddress)) { candidates.Add(unit); }
            }
            if (candidates.Count == 0)
            {
                foreach (UnitData unit in units) { if (unit != null) { candidates.Add(unit); } }
            }
            return candidates.Count > 0 ? candidates[chooseIndex(candidates.Count)] : null;
        }
    }
}
