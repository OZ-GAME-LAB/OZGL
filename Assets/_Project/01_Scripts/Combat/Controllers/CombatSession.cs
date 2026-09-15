using UnityEngine;
using OzGameLab01.UI;
using OzGameLab01.UI.Battle;
using OzGameLab01.Managers;
using OzGameLab01.Controllers;
using OzGameLab01.Data;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// "이번 전투" 하나에 대한 씬 소속 데이터/배선을 담당합니다. 게임 부팅 후 계속
    /// 살아있는 <see cref="CombatManager"/>(Facade 보유)와 달리 이 오브젝트는 전투 씬이
    /// 떠 있는 동안만 존재합니다. <see cref="CombatFacade"/>가 필요할 때 이 컴포넌트를
    /// 찾아 <see cref="State"/>/<see cref="FeedbackView"/>를 읽어갑니다.
    /// </summary>
    public class CombatSession : MonoBehaviour
    {
        [SerializeField] private Transform unitsRoot;
        [SerializeField] private Vector3 enemyPosition = new Vector3(4.5f, 0f, 0f);
        [SerializeField] private string enemyPrefabResourceName = "Characters/Enemy_Melee";
        [SerializeField] private float enemyScale = 3f;

        [Tooltip("모든 아군이 공유하는 프리팹입니다. Instantiate 후 UnitData로 Configure()하여 실제 유닛으로 만듭니다. 프리팹 루트는 비활성 상태여야 합니다(Configure가 Awake보다 먼저 실행되어야 하므로).")]
        [SerializeField] private GameObject allyTemplatePrefab;

        [Tooltip("아군 유닛마다 UnitAnchor 아래에 생성되는 체력/스킬 쿨다운 HUD입니다. 비워두면 HUD 없이 진행됩니다.")]
        [SerializeField] private AllyUnitCombatHUDView allyHudPrefab;

        [Header("플레이어 전투 슬롯")]
        [SerializeField] private CombatMainView battleMainView;

        [Tooltip("유닛 id별 프리팹/트레이트, 시너지 발동 정의. 로스터 준비 화면과 공유하는 데이터입니다.")]
        [SerializeField] private UnitRosterData rosterData;

        [Tooltip("적 유닛 id별 스탯. 비워두면 enemyPrefabResourceName 프리팹의 기본값을 그대로 사용합니다.")]
        [SerializeField] private MonsterRosterData monsterRosterData;
        [SerializeField] private int enemyMonsterId = 1;

        [Header("시너지 UI")]
        [Tooltip("시너지 표시 아이템이 배치될 부모입니다.")]
        [SerializeField] private Transform synergyPanelRoot;
        [Tooltip("시너지 한 개를 표시하는 아이템 원본입니다.")]
        [SerializeField] private SynergyItemView synergyItemTemplate;
        [Tooltip("발동 중인 시너지 아이템 색상입니다.")]
        [SerializeField] private Color synergyActiveColor = Color.white;
        [Tooltip("보유 중이지만 아직 발동하지 않은 시너지 아이템 색상입니다.")]
        [SerializeField] private Color synergyInactiveColor = new Color(1f, 1f, 1f, 0.4f);

        private readonly CombatState _state = new CombatState();
        private UIProjectilePool _uiProjectilePool;
        private AllySpawner _allySpawner;
        private SynergyController _synergyController;
        private CombatEffectExecutor _combatEffectExecutor;
        private CombatEffectFeedbackView _feedbackView;
        private EnemyHeaderPresenter _enemyHeaderPresenter;

        public CombatState State => _state;
        public CombatEffectFeedbackView FeedbackView => _feedbackView;

        private void Awake()
        {
            // 정적 상태라 실기기 빌드에서는 씬 전환만으로 비워지지 않는다.
            // 이전 전투 세션에서 남아있을 수 있는 참조를 새 전투 시작 전에 비운다.
            CombatUnitRegistry.Clear();
            PassiveEventBus.ResetRunState();
            //  씬/프리팹에서 직접 연결하지 못한 경우 비활성 BattleUI까지 포함해 자동으로 찾기
            if (battleMainView == null)
            {
                battleMainView = FindFirstObjectByType<CombatMainView>(FindObjectsInactive.Include);
            }
            // CombatMainView 아래에 런타임 투사체 풀은 한번만 생성
            if (battleMainView != null)
            {
                _feedbackView = CombatEffectFeedbackView.Create(battleMainView);
                _uiProjectilePool = battleMainView.GetComponentInChildren<UIProjectilePool>(true);
                if (_uiProjectilePool == null)
                {
                    GameObject poolObject =
                        new GameObject("UIProjectilePool", typeof(RectTransform), typeof(UIProjectilePool));
                    poolObject.transform.SetParent(battleMainView.transform, false);
                    poolObject.transform.SetAsLastSibling();
                    _uiProjectilePool = poolObject.GetComponent<UIProjectilePool>();
                }
            }

            // 스폰/시너지 책임은 별도 클래스로 분리되어 있다. Inspector 참조는 CombatSession이
            // 그대로 들고 있고, 생성자로 넘겨주기만 한다(씬/프리팹 재배선 불필요).
            MonsterData enemyMonsterData = monsterRosterData != null
                ? monsterRosterData.GetById(enemyMonsterId)
                : null;

            // 턴/낮밤/중간보스 상태로 스케일링한 체력과 플레이어 보유 유닛에서 훔친 액티브
            // 스킬까지 반영한 전투용 스펙으로 교체합니다. 원본 로스터 캐시는 수정하지 않습니다.
            enemyMonsterData = EnemyManager.Instance.BuildCombatSpec(enemyMonsterData);

            _allySpawner = new AllySpawner(
                battleMainView, allyTemplatePrefab, unitsRoot,
                enemyPosition, enemyPrefabResourceName, enemyScale,
                _uiProjectilePool, enemyMonsterData, allyHudPrefab);

            // dev 브랜치 머지로 들어온 전투 UI(적 이름/체력/스킬쿨타임/상태이상)를 실제 수치와
            // 연동합니다. 아직 이 값들을 갱신하는 코드가 없어 화면에는 붙어 있어도 항상
            // 초기값(0)만 보이던 상태였습니다.
            if (battleMainView != null)
            {
                CombatEnemyHeaderView enemyHeaderView = battleMainView.EnemyHeaderView;
                EnemySkillCooldownItemView enemySkillCooldownView =
                    battleMainView.GetComponentInChildren<EnemySkillCooldownItemView>(true);
                StatusEffectItemView enemyStatusEffectView =
                    enemyHeaderView != null && enemyHeaderView.StatusEffectRoot != null
                        ? enemyHeaderView.StatusEffectRoot.GetComponentInChildren<StatusEffectItemView>(true)
                        : null;
                _enemyHeaderPresenter = new EnemyHeaderPresenter(
                    enemyHeaderView, enemySkillCooldownView, enemyStatusEffectView);
            }
            _synergyController = new SynergyController(
                rosterData, synergyPanelRoot, synergyItemTemplate,
                synergyActiveColor, synergyInactiveColor, this);
            _synergyController.OnEffectApplied = CombatManager.Instance.Facade.ReportFeedback;

            BuildUnitStatLookup();
            _synergyController.BuildUnitTraitLookup(_state.UnitDataById);

            _state.SpawnedFormation = _allySpawner.SpawnAllies(
                _state.SlotUnits, SceneTransitioner.AllyFormationData, UnitFormationCombatLink.BattleUnits);

            _synergyController.ApplySynergies(_state.SpawnedFormation, _state.SlotUnits);
            _synergyController.PopulateSynergyPanel();

            _state.EnemyUnit = _allySpawner.SpawnEnemy();
            _enemyHeaderPresenter?.SetEnemyName(_state.EnemyUnit != null ? _state.EnemyUnit.DisplayName : string.Empty);

            RuntimeEffectManager.Instance.LoadTempUnitJsonAndLog();
            // 전투 시작 이벤트보다 먼저 현재 보유 유닛/유물의 효과 순서를 확정합니다.
            RuntimeEffectManager.Instance.RefreshFromPlayerState();

            // PassiveEventBus 구독은 RaiseBattleStart보다 먼저 끝나 있어야 Always/OnBattleStart
            // 효과를 놓치지 않는다.
            _combatEffectExecutor = new CombatEffectExecutor(CombatManager.Instance.Facade);
            PassiveEventBus.RaiseBattleStart();
        }

        private void Update()
        {
            _enemyHeaderPresenter?.Refresh(_state.EnemyUnit);
        }

        private void BuildUnitStatLookup()
        {
            if (rosterData == null)
            {
                return;
            }

            CombatDataValidator.ValidateRoster(rosterData, this);

            UnitRosterData.RegisterActive(rosterData, this);

            foreach (UnitData data in rosterData.UnitStats)
            {
                _state.UnitDataById[data.id] = data;
            }
        }
    }
}
