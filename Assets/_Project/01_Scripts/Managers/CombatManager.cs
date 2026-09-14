using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.UI;
using OzGameLab01.UI.Battle;
using OzGameLab01.Managers;
using OzGameLab01.Controllers;
using OzGameLab01.Data;

namespace OzGameLab01.Combat
{
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }


        public enum SlotRow { Front, Mid, Back }

        [System.Serializable]
        public struct SlotKey : System.IEquatable<SlotKey>
        {
            public int column;
            public SlotRow row;

            public bool Equals(SlotKey other) => column == other.column && row == other.row;
            public override bool Equals(object obj) => obj is SlotKey other && Equals(other);
            public override int GetHashCode() => (column, row).GetHashCode();
        }

        [System.Serializable]
        private struct SlotPlacement
        {
            public SlotKey slot;
            public int unitId;
        }

        private const int SlotColumns = 3;
        private const int SlotRows = 3;

        [SerializeField] private Transform unitsRoot;
        [SerializeField] private Vector3 gridOrigin = new Vector3(-4.5f, -1.2f, 0f);
        [SerializeField] private float columnSpacing = 1.2f;
        [SerializeField] private float rowSpacing = 1.2f;
        [SerializeField] private Vector3 enemyPosition = new Vector3(4.5f, 0f, 0f);
        [SerializeField] private string enemyPrefabResourceName = "Characters/Enemy_Melee";
        [SerializeField] private float enemyScale = 3f;

        [Tooltip("모든 아군이 공유하는 프리팹입니다. Instantiate 후 UnitData로 Configure()하여 실제 유닛으로 만듭니다. 프리팹 루트는 비활성 상태여야 합니다(Configure가 Awake보다 먼저 실행되어야 하므로).")]
        [SerializeField] private GameObject allyTemplatePrefab;

        [Tooltip("아군 유닛마다 UnitAnchor 아래에 생성되는 체력/스킬 쿨다운 HUD입니다. 비워두면 HUD 없이 진행됩니다.")]
        [SerializeField] private AllyUnitCombatHUDView allyHudPrefab;

        [Header("플레이어 전투 슬롯")]
        [SerializeField] private BattleMainView battleMainView;

        [Tooltip("맵 씬에서 미리 정한 아군 배치. 슬롯(열+행)을 키로, 그 슬롯에 들어갈 유닛 id(GameDB 기준)를 값으로 가짐.")]
        [SerializeField] private List<SlotPlacement> allyFormation = new List<SlotPlacement>();

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

        [Header("유닛 정보 UI")]
        [Tooltip("화면 하단에 현재 전투 중인 아군을 표시하는 패널입니다.")]
        [SerializeField] private BattleUnitInfoView battleUnitInfoView;

        [Tooltip("전투에 직접 참여하지 않는 서브 유닛 id 목록. 전투 그리드에는 스폰되지 않고 하단 UI에만 표시됩니다.")]
        [SerializeField] private List<int> supportFormation = new List<int>();

        private Dictionary<SlotKey, int> _allyFormation;
        private Dictionary<SlotKey, int> _spawnedFormation;
        private Dictionary<int, UnitData> _unitDataById;
        private readonly Unit[,] _slotUnits = new Unit[SlotColumns, SlotRows];
        private Unit _enemyUnit;
        private UIProjectilePool _uiProjectilePool;
        private AllySpawner _allySpawner;
        private SynergyController _synergyController;
        private CombatEffectExecutor _combatEffectExecutor;
        private BattleEffectFeedbackView _feedbackView;
        private BattleEnemyHeaderView _enemyHeaderView;
        private EnemySkillCooldownItemView _enemySkillCooldownView;
        private StatusEffectItemView _enemyStatusEffectView;

        public void ReportFeedback(CombatFeedback feedback)
        {
            if (_feedbackView != null) _feedbackView.Show(feedback);
        }

        public Unit EnemyUnit => _enemyUnit;

        private void Awake()
        {
            Instance = this;
            // 정적 상태라 실기기 빌드에서는 씬 전환만으로 비워지지 않는다.
            // 이전 전투 세션에서 남아있을 수 있는 참조를 새 전투 시작 전에 비운다.
            BattleUnitRegistry.Clear();
            PassiveEventBus.ResetRunState();
            //  씬/프리팹에서 직접 연결하지 못한 경우 비활성 BattleUI까지 포함해 자동으로 찾기
            if (battleMainView == null)
            {
                battleMainView = FindFirstObjectByType<BattleMainView>(FindObjectsInactive.Include);
            }
            // BattleMainView 아래에 런타임 투사체 풀은 한번만 생성
            if (battleMainView != null)
            {
                _feedbackView = BattleEffectFeedbackView.Create(battleMainView);
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

            // 스폰/시너지 책임은 별도 클래스로 분리되어 있다. Inspector 참조는 CombatManager가
            // 그대로 들고 있고, 생성자로 넘겨주기만 한다(씬/프리팹 재배선 불필요).
            MonsterData enemyMonsterData = monsterRosterData != null
                ? monsterRosterData.GetById(enemyMonsterId)
                : null;

            // 턴/낮밤/중간보스 상태로 스케일링한 체력과 플레이어 보유 유닛에서 훔친 액티브
            // 스킬까지 반영한 전투용 스펙으로 교체합니다. 원본 로스터 캐시는 수정하지 않습니다.
            enemyMonsterData = EnemyManager.Instance.BuildCombatSpec(enemyMonsterData);

            _allySpawner = new AllySpawner(
                battleMainView, allyTemplatePrefab, unitsRoot,
                gridOrigin, columnSpacing, rowSpacing,
                enemyPosition, enemyPrefabResourceName, enemyScale,
                _uiProjectilePool, enemyMonsterData, allyHudPrefab);

            // dev 브랜치 머지로 들어온 전투 UI(적 이름/체력/스킬쿨타임/상태이상)를 실제 수치와
            // 연동합니다. 아직 이 값들을 갱신하는 코드가 없어 화면에는 붙어 있어도 항상
            // 초기값(0)만 보이던 상태였습니다.
            if (battleMainView != null)
            {
                _enemyHeaderView = battleMainView.EnemyHeaderView;
                _enemySkillCooldownView = battleMainView.GetComponentInChildren<EnemySkillCooldownItemView>(true);
                if (_enemyHeaderView != null && _enemyHeaderView.StatusEffectRoot != null)
                {
                    _enemyStatusEffectView = _enemyHeaderView.StatusEffectRoot
                        .GetComponentInChildren<StatusEffectItemView>(true);
                }
            }
            _synergyController = new SynergyController(
                rosterData, synergyPanelRoot, synergyItemTemplate,
                synergyActiveColor, synergyInactiveColor, this);
            _synergyController.OnEffectApplied = ReportFeedback;

            BuildAllyFormation();
            BuildUnitStatLookup();
            _synergyController.BuildUnitTraitLookup(_unitDataById);

            AllySpawner.SpawnResult spawnResult = _allySpawner.SpawnAllies(
                _slotUnits, _allyFormation, _unitDataById,
                SceneTransitioner.AllyFormationData, UnitFormationCombatLink.BattleUnits);
            _spawnedFormation = spawnResult.SpawnedFormation;

            _synergyController.ApplySynergies(_spawnedFormation, _slotUnits);
            _synergyController.PopulateSynergyPanel();

            // 배치 데이터로 스폰했다면 BattleFormationInfoController가 유닛 정보 패널을
            // 실제 편성 기준으로 채운다. 인스펙터 폴백 편성일 때만 여기서 직접 채운다.
            if (!spawnResult.UsedPlacementData)
            {
                PopulateUnitInfoPanel();
            }

            _enemyUnit = _allySpawner.SpawnEnemy();
            _enemyHeaderView?.SetEnemyName(_enemyUnit != null ? _enemyUnit.DisplayName : string.Empty);

            RuntimeEffectManager.Instance.LoadTempUnitJsonAndLog();
            // 전투 시작 이벤트보다 먼저 현재 보유 유닛/유물의 효과 순서를 확정합니다.
            RuntimeEffectManager.Instance.RefreshFromPlayerState();

            // PassiveEventBus 구독은 RaiseBattleStart보다 먼저 끝나 있어야 Always/OnBattleStart
            // 효과를 놓치지 않는다.
            _combatEffectExecutor = new CombatEffectExecutor(this);
            PassiveEventBus.RaiseBattleStart();
        }

        private void Update()
        {
            UpdateEnemyHeader();
        }

        /// <summary>
        /// 적 체력/스킬 쿨타임/상태이상 UI를 매 프레임 실제 수치로 갱신합니다.
        /// 아이콘(스킬/상태이상)은 아직 원화 에셋이 없어 항상 비어 있고, 게이지·수치만 반영됩니다.
        /// </summary>
        private void UpdateEnemyHeader()
        {
            if (_enemyUnit == null)
            {
                return;
            }

            _enemyHeaderView?.SetHealth(_enemyUnit.CurrentHp, _enemyUnit.MaxHp);

            if (_enemySkillCooldownView != null)
            {
                if (_enemyUnit.TryGetActiveSkillCooldown(out float remaining, out float duration))
                {
                    _enemySkillCooldownView.SetCooldown(remaining, duration);
                }
                else
                {
                    _enemySkillCooldownView.SetVisible(false);
                }
            }

            if (_enemyStatusEffectView != null)
            {
                if (_enemyUnit.TryGetPrimaryDebuff(out _, out float debuffRemaining, out float debuffDuration))
                {
                    _enemyStatusEffectView.SetVisible(true);
                    _enemyStatusEffectView.SetDuration(debuffRemaining, debuffDuration);
                }
                else
                {
                    _enemyStatusEffectView.SetVisible(false);
                }
            }
        }

        private void BuildAllyFormation()
        {
            _allyFormation = new Dictionary<SlotKey, int>();
            foreach (SlotPlacement placement in allyFormation)
            {
                _allyFormation[placement.slot] = placement.unitId;
            }
        }

        private void BuildUnitStatLookup()
        {
            _unitDataById = new Dictionary<int, UnitData>();
            if (rosterData == null)
            {
                return;
            }

            CombatDataValidator.ValidateRoster(rosterData, this);

            UnitRosterData.RegisterActive(rosterData, this);

            foreach (UnitData data in rosterData.UnitStats)
            {
                _unitDataById[data.id] = data;
            }
        }

        public Unit ResolveAllyTarget()
        {
            List<Unit> exposed = new List<Unit>();

            for (int column = 0; column < SlotColumns; column++)
            {
                Unit front = _slotUnits[column, (int)SlotRow.Front];
                Unit mid = _slotUnits[column, (int)SlotRow.Mid];
                Unit back = _slotUnits[column, (int)SlotRow.Back];

                if (front != null && !front.IsDead)
                {
                    exposed.Add(front);
                }
                else if (mid != null && !mid.IsDead)
                {
                    exposed.Add(mid);
                }
                else if (back != null && !back.IsDead)
                {
                    exposed.Add(back);
                }
            }

            if (exposed.Count == 0)
            {
                return null;
            }

            return exposed[Random.Range(0, exposed.Count)];
        }

        public List<Unit> GetParticipatingAllyUnits()
        {
            List<Unit> units = new List<Unit>();
            foreach (Unit unit in _slotUnits)
            {
                if (unit != null)
                {
                    units.Add(unit);
                }
            }

            return units;
        }

        /// <summary>
        /// 특정 행(front/mid/back)에 살아있는 아군만 반환합니다. CombatEffectExecutor의
        /// EffectTarget.FrontRow/MidRow/BackRow 해석에 사용합니다.
        /// </summary>
        public List<Unit> GetAliveAlliesInRow(SlotRow row)
        {
            List<Unit> units = new List<Unit>();
            for (int column = 0; column < SlotColumns; column++)
            {
                Unit unit = _slotUnits[column, (int)row];
                if (unit != null && !unit.IsDead)
                {
                    units.Add(unit);
                }
            }

            return units;
        }

        /// <summary>
        /// 유닛 id(GameDB 기준)로 현재 전투에 스폰된 아군 Unit을 찾습니다. 패시브 효과의
        /// Self 타겟(효과를 보유한 유닛 자신)을 해석할 때 사용합니다 — 소유는 하고 있지만
        /// 이번 전투 편성에는 없는 유닛이면 null을 반환합니다.
        /// </summary>
        public Unit GetAllyUnitById(int unitId)
        {
            foreach (KeyValuePair<SlotKey, int> kvp in _spawnedFormation)
            {
                if (kvp.Value == unitId)
                {
                    return _slotUnits[kvp.Key.column, (int)kvp.Key.row];
                }
            }

            return null;
        }

        /// <summary>
        /// 화면 하단 유닛 정보 패널에 현재 전투 중인 아군을 표시합니다.
        /// 초상화는 스폰된 유닛의 SpriteRenderer에서, 이름은 UnitData에서 가져옵니다.
        /// </summary>
        private void PopulateUnitInfoPanel()
        {
            if (battleUnitInfoView == null)
            {
                return;
            }

            battleUnitInfoView.ClearUnitInfoItems();

            foreach (KeyValuePair<SlotKey, int> kvp in _spawnedFormation)
            {
                Unit unit = _slotUnits[kvp.Key.column, (int)kvp.Key.row];
                if (unit == null)
                {
                    continue;
                }

                BattleUnitInfoItemView item = battleUnitInfoView.CreateBattleUnitInfoItem();
                if (item == null)
                {
                    continue;
                }

                SpriteRenderer spriteRenderer = unit.GetComponentInChildren<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    item.SetPortrait(spriteRenderer.sprite);
                }

                if (_unitDataById.TryGetValue(kvp.Value, out UnitData data) && data != null)
                {
                    item.SetUnitName(data.name);
                }
            }

            Sprite allyIconSprite = allyTemplatePrefab != null
                ? allyTemplatePrefab.GetComponentInChildren<SpriteRenderer>(true)?.sprite
                : null;

            foreach (int unitId in supportFormation)
            {
                if (!_unitDataById.TryGetValue(unitId, out UnitData data) || data == null)
                {
                    continue;
                }

                SupportUnitInfoItemView item = battleUnitInfoView.CreateSupportUnitInfoItem();
                if (item == null)
                {
                    continue;
                }

                item.SetPortrait(allyIconSprite);
                if (item.PortraitImage != null)
                {
                    item.PortraitImage.color = data.color;
                }

                item.SetUnitName(data.name);
            }
        }
    }
}
