using System.Collections.Generic;
using UnityEngine;

namespace OzGameLab01.UI.Battle
{
    [DisallowMultipleComponent]
    public sealed class BattleMainView : MonoBehaviour
    {
        [Header("Sub Views")]
        [SerializeField] private BattleTimerView timerView;
        [SerializeField] private BattleControlView controlView;
        [SerializeField] private BattleUnitInfoView unitInfoView;
        [SerializeField] private BattleSynergyView synergyView;
        [SerializeField] private BattleArtifactView artifactView;

        [Header("Battlefield")]
        [SerializeField] private Transform allyCombatArea;
        [SerializeField] private Transform enemyCombatArea;
        [SerializeField]
        private List<PlayerSlotItemView> playerSlotViews =
            new List<PlayerSlotItemView>();

        #region Properties

        public BattleTimerView TimerView => timerView;
        public BattleControlView ControlView => controlView;
        public BattleUnitInfoView UnitInfoView => unitInfoView;
        public BattleSynergyView SynergyView => synergyView;
        public BattleArtifactView ArtifactView => artifactView;

        public Transform AllyCombatArea => allyCombatArea;
        public Transform EnemyCombatArea => enemyCombatArea;

        public IReadOnlyList<PlayerSlotItemView> PlayerSlotViews => playerSlotViews;

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

        #endregion
    }
}