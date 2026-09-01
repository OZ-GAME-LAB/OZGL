using OzGameLab01.Combat;
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

        //unitIcon�� ��ġ
        public bool PlaceUnitIcon(UnitPlaceIconUI unitIcon, Transform fieldSlot)
        {
            //����ó��
            if (unitIcon == null) return false;
            if (fieldSlot == null) return false;

            //���������� ��ġ�ϴ� ���
            SetUnitIconPosition(unitIcon, fieldSlot);

            return true;
        }
        //�� ������ unitIcon�� ����
        public bool SwapUnitIcon(int fromSlot, int toSlot, UnitPlaceIconUI fromUnitIcon)
        {
            //����ó��
            //if (!IsValidSlot(fromSlot) || !IsValidSlot(toSlot)) return false;
            if (fromSlot == toSlot) return false;

            //������ ������ ��ġ�� ���ָ���Ʈ �� �� 
            if (fromSlot == -1)
            {
                ReturnToListUnitIcon(GetUnitIcon(toSlot));
                SetUnitIconPosition(fromUnitIcon, fieldSlotRoot.GetChild(toSlot));

                fromUnitIcon.SetSlotIndex(toSlot);

                return true;
            }
            //������ ������ ��ġ�� �ʵ彽�� �� ��
            
            UnitPlaceIconUI toSlotUnitIcon = GetUnitIcon(toSlot);

            fromUnitIcon.SetSlotIndex(toSlot);
            toSlotUnitIcon.SetSlotIndex(fromSlot);

            SetUnitIconPosition(fromUnitIcon, fieldSlotRoot.GetChild(toSlot));
            SetUnitIconPosition(toSlotUnitIcon, fieldSlotRoot.GetChild(fromSlot));

            return true;
        }
        //����Ʈ�� unitIcon�ǵ�����
        public bool ReturnToListUnitIcon(UnitPlaceIconUI unitIcon)
        {
            if (unitIcon == null) return false;
           
            SetUnitIconPosition(unitIcon, unitListRoot);
            unitIcon.SetSlotIndex(-1);
            return true;
        }
        //���������� unitIcon�� �ű��
        private void SetUnitIconPosition(UnitPlaceIconUI unitIcon, Transform targetParent, bool isReturnList = false)
        {
            unitIcon.transform.SetParent(targetParent, false);
            if (!isReturnList) unitIcon.transform.localPosition = Vector2.zero;

        }
        //������ ��ȿ�� �˻�
        private bool IsValidSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < _slotCount;
        }
        //�ʵ� ���Կ� �ִ� unitIcon�� ��ȯ
        private UnitPlaceIconUI GetUnitIcon(int targetIndex)
        {
            return fieldSlotRoot.GetChild(targetIndex).GetChild(0).GetComponent<UnitPlaceIconUI>();
        }
    }
}