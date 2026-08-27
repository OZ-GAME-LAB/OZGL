using Combat;
using OzGameLab01.Controllers;
using OzGameLab01.UI;
using UnityEngine;

namespace OzGameLab01.Managers
{
    public class FormationViewerManager : MonoBehaviour
    {
        [SerializeField]
        private Transform fieldSlotRoot;
        [SerializeField]
        private Transform unitListRoot;
        [SerializeField]
        private FormationManager formationManager;

        private int _slotCount = 9;        //3x3

        //unitIcon을 배치
        public bool PlaceUnitIcon(UnitPlaceIconUI unitIcon, Transform fieldSlot)
        {
            //예외처리
            if (unitIcon == null) return false;
            if (fieldSlot == null) return false;

            //직접적으로 배치하는 기능
            SetUnitIconPosition(unitIcon, fieldSlot);

            return true;
        }
        //두 슬롯의 unitIcon을 스왑
        public bool SwapUnitIcon(int fromSlot, int toSlot, UnitPlaceIconUI fromUnitIcon)
        {
            //예외처리
            //if (!IsValidSlot(fromSlot) || !IsValidSlot(toSlot)) return false;
            if (fromSlot == toSlot) return false;

            //시작한 유닛의 위치가 유닛리스트 일 때 
            if (fromSlot == -1)
            {
                ReturnToListUnitIcon(GetUnitIcon(toSlot));
                SetUnitIconPosition(fromUnitIcon, fieldSlotRoot.GetChild(toSlot));

                fromUnitIcon.SetSlotIndex(toSlot);

                return true;
            }
            //시작한 유닛의 위치가 필드슬롯 일 때
            
            UnitPlaceIconUI toSlotUnitIcon = GetUnitIcon(toSlot);

            fromUnitIcon.SetSlotIndex(toSlot);
            toSlotUnitIcon.SetSlotIndex(fromSlot);

            SetUnitIconPosition(fromUnitIcon, fieldSlotRoot.GetChild(toSlot));
            SetUnitIconPosition(toSlotUnitIcon, fieldSlotRoot.GetChild(fromSlot));

            return true;
        }
        //리스트로 unitIcon되돌리기
        public bool ReturnToListUnitIcon(UnitPlaceIconUI unitIcon)
        {
            if (unitIcon == null) return false;
           
            SetUnitIconPosition(unitIcon, unitListRoot);
            unitIcon.SetSlotIndex(-1);
            return true;
        }
        //직접적으로 unitIcon을 옮기기
        private void SetUnitIconPosition(UnitPlaceIconUI unitIcon, Transform targetParent, bool isReturnList = false)
        {
            unitIcon.transform.SetParent(targetParent, false);
            if (!isReturnList) unitIcon.transform.localPosition = Vector2.zero;

        }
        //슬롯의 유효성 검사
        private bool IsValidSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < _slotCount;
        }
        //필드 슬롯에 있는 unitIcon을 반환
        private UnitPlaceIconUI GetUnitIcon(int targetIndex)
        {
            return fieldSlotRoot.GetChild(targetIndex).GetChild(0).GetComponent<UnitPlaceIconUI>();
        }
    }
}