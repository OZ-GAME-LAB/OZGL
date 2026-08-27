using Combat;
using OzGameLab01.UI;
using UnityEngine;

namespace OzGameLab01.Managers
{
    public class UnitPlaceManager : MonoBehaviour
    {
        [SerializeField]
        private FormationManager formationManager;
        [SerializeField]
        private FormationViewerManager formationViewerManager;


        #region 프로토타입
        [SerializeField]
        private UnitPlaceIconUI _unitIconPrefab_bow;
        [SerializeField]
        private UnitPlaceIconUI _unitIconPrefab_staff;
        [SerializeField]
        private UnitPlaceIconUI _unitIconPrefab_sword;
        #endregion

        [SerializeField]
        private Transform unitListRoot;

        private void Start()
        {
            CreateUnitIcons();
        }

        public void Open()//List<Unit> units)
        {
        }
        public void Close()
        {
            this.gameObject.SetActive(false);
        }
        private void CreateUnitIcons()//List<Unit> units)
        {
            ClearUnitIcons();

            for(int i=0; i<3; i++)
            {
                UnitPlaceIconUI unitIcon_bow = Instantiate(_unitIconPrefab_bow, unitListRoot);
                unitIcon_bow.Init(GetComponent<Unit>(), formationManager, formationViewerManager);
            }


            UnitPlaceIconUI unitIcon_staff = Instantiate(_unitIconPrefab_staff, unitListRoot);
            unitIcon_staff.Init(GetComponent<Unit>(), formationManager, formationViewerManager);
            UnitPlaceIconUI unitIcon_sword = Instantiate(_unitIconPrefab_sword, unitListRoot);
            unitIcon_sword.Init(GetComponent<Unit>(), formationManager, formationViewerManager);

            //foreach (Unit unit in units)
            //{
            //    UnitPlaceIconUI unitIcon = Instantiate(_unitPortraitPrefab, _unitListRoot);

            //    unitIcon.Init( unit, _formationManager);
            //}
        }

        private void ClearUnitIcons()
        {
            for (int i = unitListRoot.childCount - 1;
                 i >= 0;
                 i--)
            {
                Destroy(unitListRoot.GetChild(i).gameObject);
            }
        }

    }
}