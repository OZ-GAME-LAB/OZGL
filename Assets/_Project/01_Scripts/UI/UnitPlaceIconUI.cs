using UnityEngine;
using UnityEngine.UI;
using OzGameLab01.Controllers;
using UnityEngine.EventSystems;
using OzGameLab01.Managers;
using Combat; 

namespace OzGameLab01.UI
{
    public class UnitPlaceIconUI : MonoBehaviour , IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerClickHandler
    {
        #region 프로토타입 용 속성
        [SerializeField]
        private GameObject prototypeUnitPrefab;
        private Color _prototypeColor;
        #endregion

        private Unit _unit;

        private Image _unitPlaceIcon;
        private FormationManager _formationManager;
        private FormationViewerManager _formationViewerManager;
        private CanvasGroup _canvasGroup;
        private Transform _originalParent;
        private Transform _prevParent;
        //private Vector2 _originalPosition;
        private Vector2 _prevPosition;

        [SerializeField]
        private int prevSlotIndex;
        

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _unitPlaceIcon = GetComponent<Image>();
            _originalParent = transform.parent;
            #region 프로토타입용 코드
            _prototypeColor = Random.ColorHSV();
            _unitPlaceIcon.color = _prototypeColor;
            _unit = prototypeUnitPrefab.GetComponent<Unit>();
            prevSlotIndex = -1;
            #endregion

            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void Init(Unit unit, FormationManager formationManager, FormationViewerManager formationViewManager)
        {
            _formationManager = formationManager;
            _formationViewerManager = formationViewManager;
            //ToDo : 유닛의 정보 중에 필요한 것들 여기서 세팅
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_unit == null) return;

            if (prevSlotIndex != -1) _formationManager.RemoveUnit(prevSlotIndex);

            _prevParent = transform.parent;
            _prevPosition = transform.position;
            

            _canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            transform.position = eventData.position;

            FieldSlot fieldSlot = FindFieldSlot(eventData);
            
            if (fieldSlot == null) 
            {
                return;
            } 

            if (fieldSlot != null)
            {
                fieldSlot.OnUnitDrag(true);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _canvasGroup.blocksRaycasts = true;

            FieldSlot fieldSlot = FindFieldSlot(eventData);

            if (fieldSlot == null)
            {
                _formationViewerManager.PlaceUnitIcon(this, _prevParent);
                transform.position = _prevPosition;
                return;
            }

            if(fieldSlot != null)
            {
                //해당 슬롯에 이미 다른 기물이 있을 경우 스왑.
                if (!_formationManager.IsEmpty(fieldSlot.SlotIndex)) 
                {
                    _formationManager.SwapUnit(prevSlotIndex, fieldSlot.SlotIndex, _unit);
                    _formationViewerManager.SwapUnitIcon( prevSlotIndex, fieldSlot.SlotIndex, this);
                    return;
                } 

                _formationManager.PlaceUnit( fieldSlot.SlotIndex, _unit); //
                _formationViewerManager.PlaceUnitIcon(this, fieldSlot.transform);

                //SetPosition(fieldSlot.transform, fieldSlot.transform.position);

                prevSlotIndex = fieldSlot.SlotIndex;
            }
        }
        public void SetSlotIndex(int slotIndex)
        {
            prevSlotIndex = slotIndex;
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            if(eventData.button == PointerEventData.InputButton.Right)
            {
                _formationManager.RemoveUnit(prevSlotIndex);
                _formationViewerManager.ReturnToListUnitIcon(this);
            }
        }

        private FieldSlot FindFieldSlot(PointerEventData eventData)
        {
            if (eventData.pointerCurrentRaycast.gameObject == null) return null;

            return eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<FieldSlot>();
        }
    }
}