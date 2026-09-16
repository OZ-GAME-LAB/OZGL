using System.Collections.Generic;
using UnityEngine;

namespace OzGameLab01.UI.Battle
{
    [DisallowMultipleComponent]
    public sealed class CombatUnitInfoView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform unitInfoContentRoot;
        [SerializeField] private CombatUnitInfoItemView battleUnitInfoItemPrefab;
        [SerializeField] private CombatUnitInfoItemView supportUnitInfoItemPrefab;

        private readonly List<CombatUnitInfoItemView> battleUnitInfoItems = new ();
        private readonly List<CombatUnitInfoItemView> supportUnitInfoItems = new ();

        #region Properties

        public Transform UnitInfoContentRoot => unitInfoContentRoot;

        public IReadOnlyList<CombatUnitInfoItemView> BattleUnitInfoItems =>
            battleUnitInfoItems;

        public IReadOnlyList<CombatUnitInfoItemView> SupportUnitInfoItems =>
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

        public CombatUnitInfoItemView CreateBattleUnitInfoItem()
        {
            if (battleUnitInfoItemPrefab == null || unitInfoContentRoot == null)
            {
                return null;
            }

            CombatUnitInfoItemView item = Instantiate(battleUnitInfoItemPrefab,unitInfoContentRoot);

            battleUnitInfoItems.Add(item);

            return item;
        }

        public CombatUnitInfoItemView CreateSupportUnitInfoItem()
        {
            if (supportUnitInfoItemPrefab == null || unitInfoContentRoot == null)
            {
                return null;
            }

            CombatUnitInfoItemView item = Instantiate(
                supportUnitInfoItemPrefab,
                unitInfoContentRoot);

            supportUnitInfoItems.Add(item);

            return item;
        }

        public void ClearUnitInfoItems()
        {
            foreach (CombatUnitInfoItemView item in battleUnitInfoItems)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }

            foreach (CombatUnitInfoItemView item in supportUnitInfoItems)
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