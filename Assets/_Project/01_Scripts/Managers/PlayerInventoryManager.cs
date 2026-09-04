using System;
using System.Collections.Generic;
using UnityEngine;

namespace OzGameLab01.Managers
{
    /// <summary>
    /// 게임 중 플레이어가 획득한 유닛과 재화 등을 전역(Global)으로 관리하는 싱글톤 인벤토리 매니저입니다.
    /// 전투 씬으로 전환되어도 이 데이터는 절대 파괴되지 않습니다.
    /// </summary>
    public class PlayerInventoryManager : Singleton<PlayerInventoryManager>
    {
        private readonly List<UnitData> _ownedUnits = new List<UnitData>();

        /// <summary>
        /// 현재 플레이어가 보유한 전체 유닛 목록
        /// </summary>
        public IReadOnlyList<UnitData> OwnedUnits => _ownedUnits;

        public event Action<UnitData> OnUnitAdded;

        protected override void Awake()
        {
            base.Awake();

            // 임시: 매니저가 처음 생성될 때 기본 테스트 유닛 5개를 지급합니다.
            // 나중에 유닛 획득 타일을 밟아서 얻는 진짜 로직이 완성되면 이 if문을 삭제하시면 됩니다!
            if (_ownedUnits.Count == 0)
            {
                InitializeStartingUnits();
            }
        }

        /// <summary>
        /// 새 유닛을 인벤토리에 추가합니다. (나중에 맵 타일 이벤트에서 호출할 함수)
        /// </summary>
        public void AddUnit(UnitData unit)
        {
            if (unit == null) return;

            _ownedUnits.Add(unit);
            OnUnitAdded?.Invoke(unit);
            Debug.Log($"[PlayerInventoryManager] 유닛 획득 성공! : {unit.name} (현재 총 {_ownedUnits.Count}명 보유 중)");
        }

        /// <summary>
        /// 게임 오버 또는 타이틀로 돌아갈 때 인벤토리를 비우는 용도입니다.
        /// </summary>
        public void ClearInventory()
        {
            _ownedUnits.Clear();
            Debug.Log("[PlayerInventoryManager] 인벤토리가 초기화되었습니다.");
        }

        private void InitializeStartingUnits()
        {
            // UI에 임시로 띄워줄 테스트 유닛들
            AddUnit(new UnitData { id = 1001, name = "Test Unit 01", healthPoint = 100, attackPoint = 10, criticalRate = 5, dodgeRate = 3, bloodDrain = 0, attackSpeed = 10, skillCooldown = 5 });
            AddUnit(new UnitData { id = 1002, name = "Test Unit 02", healthPoint = 110, attackPoint = 12, criticalRate = 7, dodgeRate = 4, bloodDrain = 0, attackSpeed = 9, skillCooldown = 6 });
            AddUnit(new UnitData { id = 1003, name = "Test Unit 03", healthPoint = 90, attackPoint = 15, criticalRate = 10, dodgeRate = 5, bloodDrain = 2, attackSpeed = 12, skillCooldown = 7 });
            AddUnit(new UnitData { id = 1004, name = "Test Unit 04", healthPoint = 130, attackPoint = 8, criticalRate = 4, dodgeRate = 8, bloodDrain = 0, attackSpeed = 7, skillCooldown = 4 });
            AddUnit(new UnitData { id = 1005, name = "Test Unit 05", healthPoint = 95, attackPoint = 14, criticalRate = 8, dodgeRate = 6, bloodDrain = 3, attackSpeed = 11, skillCooldown = 8 });
        }
    }
}