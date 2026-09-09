using System;
using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.Combat;
using OzGameLab01.Data;

namespace OzGameLab01.Managers
{
    /// <summary>
    /// 게임 중 플레이어가 획득한 유닛과 재화 등을 전역(Global)으로 관리하는 싱글톤 인벤토리 매니저입니다.
    /// 전투 씬으로 전환되어도 이 데이터는 절대 파괴되지 않습니다.
    /// </summary>
    public class PlayerInventoryManager : Singleton<PlayerInventoryManager>
    {
        [Tooltip("게임 시작 시 임시로 지급할 시작 유닛의 원본 데이터. CombatManager/UnitFormationController와 동일한 로스터(UnitRosterData)를 사용해야 id·트레이트가 어긋나지 않습니다.")]
        [SerializeField] private UnitRosterData startingRoster;

        private readonly List<UnitData> _ownedUnits = new List<UnitData>();

        /// <summary>
        /// 현재 플레이어가 보유한 전체 유닛 목록
        /// </summary>
        public IReadOnlyList<UnitData> OwnedUnits => _ownedUnits;

        public event Action<UnitData> OnUnitAdded;

        protected override void Awake()
        {
            base.Awake();
            // [추가] Continue에서 보류된 인벤토리를 로스터 참조가 준비된 시점에 복원
            SaveManager.Instance.RestorePendingInventory(this);
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

        /// <summary>
        /// 저장된 유닛 ID와 수량을 로스터 원본 데이터로 복원합니다.
        /// </summary>
        public void RestoreFromSave(IReadOnlyList<UnitStuff> savedUnits)
        {
            _ownedUnits.Clear();

            if (savedUnits == null || savedUnits.Count == 0)
            {
                return;
            }

            if (startingRoster == null)
            {
                Debug.LogError("[PlayerInventoryManager] 저장된 인벤토리를 복원할 UnitRosterData가 없습니다.", this);
                return;
            }

            foreach (UnitStuff savedUnit in savedUnits)
            {
                if (savedUnit == null || savedUnit.amount <= 0)
                {
                    continue;
                }

                UnitData rosterUnit = FindRosterUnit(savedUnit.unitId);
                if (rosterUnit == null)
                {
                    Debug.LogWarning($"[PlayerInventoryManager] 로스터에 없는 저장 유닛을 건너뜁니다. ID: {savedUnit.unitId}", this);
                    continue;
                }

                for (int amountIndex = 0; amountIndex < savedUnit.amount; amountIndex++)
                {
                    _ownedUnits.Add(CloneUnitData(rosterUnit));
                }
            }

            Debug.Log($"[PlayerInventoryManager] 저장된 인벤토리를 복원했습니다. 총 {_ownedUnits.Count}명", this);
        }

        /// <summary>
        /// 저장된 유닛 ID와 일치하는 로스터 원본을 찾습니다.
        /// </summary>
        private UnitData FindRosterUnit(int unitId)
        {
            foreach (UnitData rosterUnit in startingRoster.UnitStats)
            {
                if (rosterUnit != null && rosterUnit.id == unitId)
                {
                    return rosterUnit;
                }
            }

            return null;
        }

        /// <summary>
        /// UnitRosterData가 들고 있는 원본 UnitData를 그대로 참조하면 인벤토리 쪽에서
        /// 값을 바꿀 때 로스터 에셋까지 같이 바뀌므로, 별도 인스턴스로 복제해서 지급합니다.
        /// </summary>
        public static UnitData CloneUnitData(UnitData source)
        {
            return new UnitData
            {
                id = source.id,
                name = source.name,
                spriteAddress = source.spriteAddress,
                healthPoint = source.healthPoint,
                attackPoint = source.attackPoint,
                defensePoint = source.defensePoint,
                attackSpeed = source.attackSpeed,
                criticalMult = source.criticalMult,
                criticalRate = source.criticalRate,
                dodgeRate = source.dodgeRate,
                basicAttackCooldown = source.basicAttackCooldown,
                passiveSkillKey = source.passiveSkillKey,
                activeSkillKey = source.activeSkillKey,
                skillCooldown = source.skillCooldown,
                attackKey = source.attackKey,
                color = source.color,
                skillType = source.skillType,
                jobType = source.jobType,
                tribeType = source.tribeType
            };
        }
    }
}
