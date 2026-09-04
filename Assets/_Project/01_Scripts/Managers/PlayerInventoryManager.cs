using System;
using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.Combat;

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
            // 임시: UnitRosterData(CombatManager/UnitFormationController와 공유하는 id·트레이트 체계)에서
            // 시작 유닛을 그대로 지급합니다. 나중에 유닛 획득 타일을 밟아서 얻는 진짜 로직이 완성되면 이 메서드는 삭제하시면 됩니다!
            if (startingRoster == null)
            {
                Debug.LogWarning("[PlayerInventoryManager] startingRoster(UnitRosterData)가 연결되지 않아 시작 유닛을 지급할 수 없습니다.", this);
                return;
            }

            foreach (UnitData source in startingRoster.UnitStats)
            {
                if (source == null)
                {
                    continue;
                }

                AddUnit(CloneUnitData(source));
            }
        }

        /// <summary>
        /// UnitRosterData가 들고 있는 원본 UnitData를 그대로 참조하면 인벤토리 쪽에서
        /// 값을 바꿀 때 로스터 에셋까지 같이 바뀌므로, 별도 인스턴스로 복제해서 지급합니다.
        /// </summary>
        private static UnitData CloneUnitData(UnitData source)
        {
            return new UnitData
            {
                id = source.id,
                name = source.name,
                spriteAddress = source.spriteAddress,
                healthPoint = source.healthPoint,
                attackPoint = source.attackPoint,
                criticalRate = source.criticalRate,
                dodgeRate = source.dodgeRate,
                bloodDrain = source.bloodDrain,
                attackSpeed = source.attackSpeed,
                basicAttackCooldown = source.basicAttackCooldown,
                skillCooldown = source.skillCooldown,
                attackKey = source.attackKey,
                skillKey = source.skillKey,
                color = source.color,
                skillType = source.skillType
            };
        }
    }
}