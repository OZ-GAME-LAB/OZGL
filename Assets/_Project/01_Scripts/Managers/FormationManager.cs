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

        private void Awake()
        {
            Init();
        }

        private void Init()
        {
            _placedUnits = new Unit[_slotCount];
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