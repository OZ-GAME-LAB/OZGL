using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.Data;
using OzGameLab01.Managers;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// CombatManager에서 분리된 전투 상태(Model)입니다. 스폰된 아군 배치, 유닛 로스터 조회,
    /// 적 유닛 참조를 보관하고 순수 조회 메서드만 제공합니다. Unity 생명주기나 View에
    /// 의존하지 않는 순수 C# 클래스입니다.
    /// </summary>
    public sealed class CombatState
    {
        private const int SlotColumns = 3;
        private const int SlotRows = 3;

        public Unit[,] SlotUnits { get; } = new Unit[SlotColumns, SlotRows];
        public Dictionary<int, UnitData> UnitDataById { get; } = new Dictionary<int, UnitData>();
        public Dictionary<CombatManager.SlotKey, int> SpawnedFormation { get; set; } = new Dictionary<CombatManager.SlotKey, int>();
        public Unit EnemyUnit { get; set; }

        public Unit ResolveAllyTarget()
        {
            List<Unit> exposed = new List<Unit>();

            for (int column = 0; column < SlotColumns; column++)
            {
                Unit front = SlotUnits[column, (int)CombatManager.SlotRow.Front];
                Unit mid = SlotUnits[column, (int)CombatManager.SlotRow.Mid];
                Unit back = SlotUnits[column, (int)CombatManager.SlotRow.Back];

                if (front != null && !front.IsDead)
                {
                    exposed.Add(front);
                }
                else if (mid != null && !mid.IsDead)
                {
                    exposed.Add(mid);
                }
                else if (back != null && !back.IsDead)
                {
                    exposed.Add(back);
                }
            }

            if (exposed.Count == 0)
            {
                return null;
            }

            return exposed[Random.Range(0, exposed.Count)];
        }

        public List<Unit> GetParticipatingAllyUnits()
        {
            List<Unit> units = new List<Unit>();
            foreach (Unit unit in SlotUnits)
            {
                if (unit != null)
                {
                    units.Add(unit);
                }
            }

            return units;
        }

        /// <summary>
        /// 특정 행(front/mid/back)에 살아있는 아군만 반환합니다. CombatEffectExecutor의
        /// EffectTarget.FrontRow/MidRow/BackRow 해석에 사용합니다.
        /// </summary>
        public List<Unit> GetAliveAlliesInRow(CombatManager.SlotRow row)
        {
            List<Unit> units = new List<Unit>();
            for (int column = 0; column < SlotColumns; column++)
            {
                Unit unit = SlotUnits[column, (int)row];
                if (unit != null && !unit.IsDead)
                {
                    units.Add(unit);
                }
            }

            return units;
        }

        public List<Unit> GetAlliesInRow(CombatManager.SlotRow row)
        {
            List<Unit> units = new List<Unit>();
            for (int column = 0; column < SlotColumns; column++)
            {
                Unit unit = SlotUnits[column, (int)row];
                if (unit != null) units.Add(unit);
            }
            return units;
        }

        /// <summary>
        /// 유닛 id(GameDB 기준)로 현재 전투에 스폰된 아군 Unit을 찾습니다. 패시브 효과의
        /// Self 타겟(효과를 보유한 유닛 자신)을 해석할 때 사용합니다 — 소유는 하고 있지만
        /// 이번 전투 편성에는 없는 유닛이면 null을 반환합니다.
        /// </summary>
        public Unit GetAllyUnitById(int unitId)
        {
            foreach (KeyValuePair<CombatManager.SlotKey, int> kvp in SpawnedFormation)
            {
                if (kvp.Value == unitId)
                {
                    return SlotUnits[kvp.Key.column, (int)kvp.Key.row];
                }
            }

            return null;
        }
    }
}
