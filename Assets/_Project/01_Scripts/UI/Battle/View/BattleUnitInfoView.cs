using System.Collections.Generic;
using UnityEngine;

namespace OzGameLab01.UI.Battle
{
    [DisallowMultipleComponent]
    public sealed class BattleUnitInfoView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform unitInfoContentRoot;
        [SerializeField] private BattleUnitInfoItemView battleUnitInfoItemPrefab;
        [SerializeField] private SupportUnitInfoItemView supportUnitInfoItemPrefab;

        private readonly List<BattleUnitInfoItemView> battleUnitInfoItems = new ();
        private readonly List<SupportUnitInfoItemView> supportUnitInfoItems = new ();

        #region Properties

        public Transform UnitInfoContentRoot => unitInfoContentRoot;

        public IReadOnlyList<BattleUnitInfoItemView> BattleUnitInfoItems =>
            battleUnitInfoItems;

        public IReadOnlyList<SupportUnitInfoItemView> SupportUnitInfoItems =>
            supportUnitInfoItems;

        public bool IsVisible => gameObject.activeSelf;

        #endregion

        #region API

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public BattleUnitInfoItemView CreateBattleUnitInfoItem()
        {
            if (battleUnitInfoItemPrefab == null || unitInfoContentRoot == null)
            {
                return null;
            }

            BattleUnitInfoItemView item = Instantiate(battleUnitInfoItemPrefab,unitInfoContentRoot);

            battleUnitInfoItems.Add(item);

            return item;
        }

        public SupportUnitInfoItemView CreateSupportUnitInfoItem()
        {
            if (supportUnitInfoItemPrefab == null || unitInfoContentRoot == null)
            {
                return null;
            }

            SupportUnitInfoItemView item = Instantiate(
                supportUnitInfoItemPrefab,
                unitInfoContentRoot);

            supportUnitInfoItems.Add(item);

            return item;
        }

        public void ClearUnitInfoItems()
        {
            foreach (BattleUnitInfoItemView item in battleUnitInfoItems)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }

            foreach (SupportUnitInfoItemView item in supportUnitInfoItems)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }

            battleUnitInfoItems.Clear();
            supportUnitInfoItems.Clear();
        }

        #endregion
    }
}