using UnityEngine;

namespace OzGameLab01.Controllers
{
    public class FieldSlot : MonoBehaviour
    {
        [SerializeField]
        private int _slotIndex;

        public int SlotIndex => _slotIndex;

        public void OnUnitDrag(bool onUnit)
        {
            //ToDo : 슬롯 하이라이트 등 시각적 표시.
        }
    }
}