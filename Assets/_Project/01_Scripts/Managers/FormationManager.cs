using UnityEngine;
using OzGameLab01.Controllers;
using System;
using Combat;

namespace OzGameLab01.Managers
{
    public class FormationManager : MonoBehaviour
    {
        private int _slotCount = 9;        //3x3

        private Unit[] _placedUnits;        //현재 배치된 유닛들

        public Unit[] PlaceUnits => _placedUnits; 

        public event Action OnFormationChagned;

        // ==================== 전투/서브 편성 제한 추가 ====================

        private const int MaxBattleUnitCount = 4;
        private const int SupportSlotCount = 2;
        private Unit[] _supportUnits;

        /// <summary>
        /// 현재 전투 슬롯에 배치된 유닛 수입니다.
        /// </summary>
        public int BattleUnitCount => CountUnits(_placedUnits);

        /// <summary>
        /// 현재 편성된 서브 유닛 수입니다.
        /// </summary>
        public int SupportUnitCount => CountUnits(_supportUnits);

        /// <summary>
        /// 현재 서브 유닛 배열입니다.
        ///
        /// 배열의 길이는 항상 2이며,
        /// 비어 있는 슬롯에는 null이 들어갑니다.
        /// </summary>
        public Unit[] SupportUnits => _supportUnits;

        /// <summary>
        /// 전투를 시작할 수 있는 최소 편성인지 확인합니다.
        ///
        /// 편집 중에는 전투 유닛이 0명일 수 있지만,
        /// 실제 전투 시작에는 최소 1명이 필요합니다.
        /// </summary>
        public bool CanStartBattle =>
            BattleUnitCount >= 1 && BattleUnitCount <= MaxBattleUnitCount;

        private void Awake()
        {
            Init();
        }

        private void Init()
        {
            _placedUnits = new Unit[_slotCount];

            // 서브 유닛 데이터 슬롯 2칸 초기화
            _supportUnits = new Unit[SupportSlotCount];
        }

        public bool IsEmpty(int slotIndex)
        {
            if (!IsValidSlot(slotIndex))
                return false;

            return _placedUnits[slotIndex] == null;
        }

        public Unit GetUnit(int slotIndex)
        {
            if (!IsValidSlot(slotIndex))
                return null;

            return _placedUnits[slotIndex];
        }

        public bool PlaceUnit(int slotIndex, Unit unit) //유닛의 대한 정보도 넘길예정
        {
            //if (unit == null)
            //    return false;
            if (!IsValidSlot(slotIndex))
                return false;

            // null 유닛 배치 방지
            if (unit == null)
            {
                Debug.LogWarning("[FormationManager] null 유닛은 전투 슬롯에 배치할 수 없습니다.", this);

                return false;
            }

            // 서브 편성에 들어 있는 유닛의 전투 슬롯 중복 배치 방지
            if (ContainsUnit(_supportUnits, unit))
            {
                Debug.LogWarning(
                    $"[FormationManager] 서브 편성에 포함된 유닛은 전투 슬롯에 중복 배치할 수 없습니다. " +
                    $"유닛: {unit.name}", this);

                return false;
            }

            // 빈 슬롯에 새로운 유닛을 추가할 때만 최대 인원 검사
            bool isEmptySlot = _placedUnits[slotIndex] == null;

            if (isEmptySlot && BattleUnitCount >= MaxBattleUnitCount)
            {
                Debug.LogWarning($"[FormationManager] 전투 유닛은 최대 {MaxBattleUnitCount}명까지 배치할 수 있습니다.", this);

                return false;
            }

            _placedUnits[slotIndex] = unit;
            NotifyFormationChanged();
            return true;
        }

        public bool RemoveUnit(int slotIndex)
        {
            if (!IsValidSlot(slotIndex))
                return false;

            if (_placedUnits[slotIndex] == null)
                return false;

            _placedUnits[slotIndex] = null;

            NotifyFormationChanged();

            return true;
        }

        public bool SwapUnit(int fromSlot, int toSlot, Unit fromSlotUnit)
        {
            if (!IsValidSlot(fromSlot) || !IsValidSlot(toSlot))
                return false;

            if (fromSlot == toSlot)
                return false;

            if(fromSlot == -1)
            {
                RemoveUnit(toSlot);

                _placedUnits[toSlot] = fromSlotUnit;

                return true;
            }

            Unit temp = fromSlotUnit;

            PlaceUnit(fromSlot, _placedUnits[toSlot]);
            PlaceUnit(toSlot, temp);
            //_placedUnits[fromSlot] = _placedUnits[toSlot];
            //_placedUnits[toSlot] = temp;

            NotifyFormationChanged();

            return true;
        }

        /// <summary>
        /// 지정한 서브 슬롯이 비어 있는지 확인합니다.
        /// </summary>
        public bool IsSupportEmpty(int slotIndex)
        {
            if (!IsValidSupportSlot(slotIndex))
            {
                return false;
            }

            return _supportUnits[slotIndex] == null;
        }

        /// <summary>
        /// 지정한 서브 슬롯에 편성된 유닛을 반환합니다.
        /// </summary>
        public Unit GetSupportUnit(int slotIndex)
        {
            if (!IsValidSupportSlot(slotIndex))
            {
                return null;
            }

            return _supportUnits[slotIndex];
        }

        /// <summary>
        /// 지정한 서브 슬롯에 유닛 데이터를 편성합니다.
        /// 서브 슬롯은 총 2칸이므로 최대 2명의 서브 유닛을 저장할 수 있습니다.
        /// 전투 슬롯 또는 다른 서브 슬롯에 이미 편성된 유닛은 중복으로 추가할 수 없습니다.
        /// </summary>
        public bool PlaceSupportUnit(int slotIndex, Unit unit)
        {
            if (!IsValidSupportSlot(slotIndex))
            {
                Debug.LogWarning($"[FormationManager] 서브 슬롯 인덱스가 올바르지 않습니다. 슬롯: {slotIndex}", this);

                return false;
            }

            if (unit == null)
            {
                Debug.LogWarning("[FormationManager] null 유닛은 서브 슬롯에 편성할 수 없습니다.", this);

                return false;
            }

            // 전투 편성에 들어 있는 유닛의 서브 슬롯 중복 배치 방지
            if (ContainsUnit(_placedUnits, unit))
            {
                Debug.LogWarning(
                    $"[FormationManager] 전투 편성에 포함된 유닛은 서브 슬롯에 중복 배치할 수 없습니다. " +
                    $"유닛: {unit.name}", this);

                return false;
            }

            // 다른 서브 슬롯에 들어 있는 동일 유닛 중복 방지
            if (ContainsUnit(_supportUnits, unit, slotIndex))
            {
                Debug.LogWarning($"[FormationManager] 이미 서브 편성에 포함된 유닛입니다. 유닛: {unit.name}", this);

                return false;
            }

            _supportUnits[slotIndex] = unit;

            NotifyFormationChanged();
            return true;
        }

        /// <summary>
        /// 지정한 서브 슬롯에서 유닛을 제거합니다
        /// </summary>
        public bool RemoveSupportUnit(int slotIndex)
        {
            if (!IsValidSupportSlot(slotIndex))
            {
                return false;
            }

            if (_supportUnits[slotIndex] == null)
            {
                return false;
            }

            _supportUnits[slotIndex] = null;

            NotifyFormationChanged();
            return true;
        }

        /// <summary>
        /// 서브 슬롯 인덱스가 0 ~ 1 범위인지 확인합니다.
        /// </summary>
        private bool IsValidSupportSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < SupportSlotCount;
        }

        /// <summary>
        /// 지정한 유닛이 배열에 이미 들어 있는지 확인합니다.
        /// ignoredIndex는 현재 교체할 슬롯을 중복 검사에서 제외할 때 사용합니다.
        /// </summary>
        private bool ContainsUnit(Unit[] units, Unit targetUnit, int ignoredIndex = -1)
        {
            if (units == null || targetUnit == null)
            {
                return false;
            }

            for (int index = 0; index < units.Length; index++)
            {
                if (index == ignoredIndex)
                {
                    continue;
                }

                if (units[index] == targetUnit)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 배열에 저장된 null이 아닌 유닛 수를 반환합니다.
        /// </summary>
        private int CountUnits(Unit[] units)
        {
            if (units == null)
            {
                return 0;
            }

            int unitCount = 0;

            foreach (Unit unit in units)
            {
                if (unit != null)
                {
                    unitCount++;
                }
            }

            return unitCount;
        }

        // ==================== 전투/서브 편성 제한 추가 끝 ====================

        private bool IsValidSlot(int slotIndex) 
        {
            return slotIndex >= 0 && slotIndex < _slotCount;
        }

        private void NotifyFormationChanged()
        {
            SceneTransitioner.AllyFormationSlots = _placedUnits;
            OnFormationChagned?.Invoke();
            //ToDo : 시너지 매니저에게 시너지 체크 요청
        }
    }
}