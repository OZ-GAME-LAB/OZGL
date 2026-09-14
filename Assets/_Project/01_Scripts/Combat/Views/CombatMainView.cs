using System.Collections.Generic;
using UnityEngine;

namespace OzGameLab01.UI.Battle
{
    [DisallowMultipleComponent]
    public sealed class CombatMainView : MonoBehaviour
    {
        [SerializeField] private CombatTimerView timerView;

        [Header("Sub Views")]
        [SerializeField] private CombatEnemyHeaderView enemyHeaderView;
        [SerializeField] private CombatControlView controlView;
        [SerializeField] private CombatUnitInfoView unitInfoView;
        [SerializeField] private BattleSynergyView synergyView;
        [SerializeField] private BattleArtifactView artifactView;

        [Header("Battlefield")]
        [SerializeField] private Transform allyCombatArea;
        [SerializeField] private Transform enemyCombatArea;
        [SerializeField] private List<PlayerSlotItemView> playerSlotViews = new ();


        #region Properties

        //제거 대상입니다, 컴파일 오류 때문에 임시로 남겨놓습니당
        public CombatTimerView TimerView => timerView;

        /// <summary>
        /// 적 이름, 상태이상, 체력 UI를 관리하는 View입니다.
        /// </summary>
        public CombatEnemyHeaderView EnemyHeaderView => enemyHeaderView;

        /// <summary>
        /// 전투 제어 UI View입니다.
        /// </summary>
        public CombatControlView ControlView => controlView;

        /// <summary>
        /// 유닛 정보 UI View입니다.
        /// </summary>
        public CombatUnitInfoView UnitInfoView => unitInfoView;

        /// <summary>
        /// 시너지 UI View입니다.
        /// </summary>
        public BattleSynergyView SynergyView => synergyView;

        /// <summary>
        /// 아티팩트 UI View입니다.
        /// </summary>
        public BattleArtifactView ArtifactView => artifactView;

        /// <summary>
        /// 아군 전투 배치 영역입니다.
        /// </summary>
        public Transform AllyCombatArea => allyCombatArea;

        /// <summary>
        /// 적 전투 배치 영역입니다.
        /// </summary>
        public Transform EnemyCombatArea => enemyCombatArea;

        /// <summary>
        /// 플레이어 유닛 배치 슬롯 목록입니다.
        /// </summary>
        public IReadOnlyList<PlayerSlotItemView> PlayerSlotViews => playerSlotViews;

        /// <summary>
        /// 현재 Main View의 활성 상태입니다.
        /// </summary>
        public bool IsVisible => gameObject.activeSelf;

        #endregion


        #region Public API

        /// <summary>
        /// Battle Main View를 표시합니다.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Battle Main View를 숨깁니다.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion
    }
}