#if UNITY_EDITOR
using UnityEngine;
using OzGameLab01.Combat;
using OzGameLab01.Data;
using OzGameLab01.Managers;

namespace OzGameLab01.DebugTools
{
    /// <summary>
    /// TestScenes/SkillVfxTest 전용 디버그 하니스. 실제 전투 씬처럼 왼쪽에 아군 하나, 오른쪽에
    /// 적 하나를 배치하고 기본공격/스킬 사용 버튼 4개(아군 기본공격/스킬, 적 기본공격/스킬)로
    /// 즉시 발동시켜 VFX만 확인한다. 데미지가 실제로 들어가든 말든 상관없어서(체력을 크게 잡아
    /// 죽지 않게만 해둠) 별도의 데미지 억제 로직은 두지 않았다. 좌우 화살표로 Resources 폴더에
    /// 있는 모든 Ally_*/Enemy_* 프리팹을 순서대로 훑어볼 수 있다.
    /// </summary>
    public class SkillVfxTestHarness : MonoBehaviour
    {
        [Header("전투 세션 스텁 (씬에 비활성 상태로 있어야 함 — Awake가 돌면 안 됨)")]
        [SerializeField] private CombatSession sessionStub;

        [SerializeField] private Vector3 allySpawnPosition = new Vector3(-4f, -1.5f, 0f);
        [SerializeField] private Vector3 enemySpawnPosition = new Vector3(4f, -1.5f, 0f);

        [Header("합성 스탯 (프리팹에 대응하는 실제 데이터를 못 찾았을 때 폴백)")]
        [SerializeField] private float testHealthPoints = 999999f;
        [SerializeField] private float testAttackPoint = 3f;

        private Unit[] _allyPrefabs;
        private Unit[] _enemyPrefabs;
        private int _allyIndex;
        private int _enemyIndex;
        private Unit _ally;
        private Unit _enemy;

        private void Start()
        {
            _allyPrefabs = Resources.LoadAll<Unit>("Characters/AllyPrefabs");
            _enemyPrefabs = Resources.LoadAll<Unit>("Characters/EnemyPrefabs");
            System.Array.Sort(_allyPrefabs, (a, b) => string.CompareOrdinal(a.name, b.name));
            System.Array.Sort(_enemyPrefabs, (a, b) => string.CompareOrdinal(a.name, b.name));

            SpawnAlly(0);
            SpawnEnemy(0);
        }

        private void SpawnAlly(int index)
        {
            if (_allyPrefabs == null || _allyPrefabs.Length == 0) return;
            _allyIndex = Wrap(index, _allyPrefabs.Length);
            if (_ally != null) Destroy(_ally.gameObject);
            _ally = Spawn(_allyPrefabs[_allyIndex], allySpawnPosition, isAlly: true);
            if (sessionStub != null) sessionStub.State.SlotUnits[0, (int)CombatManager.SlotRow.Front] = _ally;
        }

        private void SpawnEnemy(int index)
        {
            if (_enemyPrefabs == null || _enemyPrefabs.Length == 0) return;
            _enemyIndex = Wrap(index, _enemyPrefabs.Length);
            if (_enemy != null) Destroy(_enemy.gameObject);
            _enemy = Spawn(_enemyPrefabs[_enemyIndex], enemySpawnPosition, isAlly: false);
            if (sessionStub != null) sessionStub.State.EnemyUnit = _enemy;
        }

        private static int Wrap(int index, int length) => ((index % length) + length) % length;

        private Unit Spawn(Unit prefab, Vector3 position, bool isAlly)
        {
            if (prefab == null) return null;

            Unit unit = Instantiate(prefab, position, Quaternion.identity);
            unit.name = prefab.name;

            if (isAlly)
            {
                unit.Configure(FindMatchingUnitData(prefab.name) ?? SyntheticUnitData());
            }
            else
            {
                unit.ConfigureEnemy(FindMatchingMonsterData(prefab.name) ?? SyntheticMonsterData());
            }

            unit.BindCombatUI(null, null, null);
            unit.SetVisualsVisible(true);
            unit.gameObject.SetActive(true);
            unit.AutoActionEnabled = false;
            return unit;
        }

        private UnitData FindMatchingUnitData(string prefabName)
        {
            foreach (UnitData candidate in RuntimeContent.Catalog.Units)
            {
                if (!MatchesPrefabName(candidate.prefabAddress, prefabName)) continue;
                return new UnitData
                {
                    id = candidate.id, name = candidate.name, prefabAddress = candidate.prefabAddress,
                    healthPoint = testHealthPoints, attackPoint = candidate.attackPoint,
                    defensePoint = candidate.defensePoint, attackSpeed = candidate.attackSpeed,
                    criticalMult = candidate.criticalMult, criticalRate = candidate.criticalRate,
                    dodgeRate = candidate.dodgeRate, skillIds = candidate.skillIds, color = candidate.color,
                };
            }
            return null;
        }

        private MonsterData FindMatchingMonsterData(string prefabName)
        {
            foreach (MonsterData candidate in RuntimeContent.Catalog.Enemies)
            {
                if (!MatchesPrefabName(candidate.prefabAddress, prefabName)) continue;
                return new MonsterData
                {
                    id = candidate.id, name = candidate.name, prefabAddress = candidate.prefabAddress,
                    healthPoint = (int)testHealthPoints, attackPoint = candidate.attackPoint,
                    defensePoint = candidate.defensePoint, attackSpeed = candidate.attackSpeed,
                    criticalMult = candidate.criticalMult, criticalRate = candidate.criticalRate,
                    dodgeRate = candidate.dodgeRate, skillCooldown = candidate.skillCooldown,
                    skillIds = candidate.skillIds, type = candidate.type,
                };
            }
            // 일반/야간 몹(1,2)은 종족 프리팹이 랜덤 배정이라 프리팹명으로 매칭이 안 된다 — id=1(일반)로 폴백.
            MonsterData normal = RuntimeContent.Catalog.GetEnemy(1);
            if (normal == null) return null;
            return new MonsterData
            {
                id = normal.id, name = normal.name, prefabAddress = prefabName,
                healthPoint = (int)testHealthPoints, attackPoint = normal.attackPoint,
                defensePoint = normal.defensePoint, attackSpeed = normal.attackSpeed,
                criticalMult = normal.criticalMult, criticalRate = normal.criticalRate,
                dodgeRate = normal.dodgeRate, skillCooldown = normal.skillCooldown,
                skillIds = normal.skillIds, type = normal.type,
            };
        }

        /// <summary>
        /// UnitData.prefabAddress는 "Units/Ally/Alice/Vanilla" 형식(끝에서 두번째 구간이 이름),
        /// MonsterData.prefabAddress는 "Characters/EnemyPrefabs/Enemy_Witch_Vanilla" 형식(끝
        /// 구간이 프리팹명 그대로)이라 두 관례를 모두 느슨하게 매칭한다.
        /// 버그였던 부분: 끝 구간("Vanilla")을 먼저/무조건 체크하면 모든 아군 변형 이름이
        /// "_Vanilla"로 끝나서 아무 캐릭터한테나 다 매칭돼버린다(전부 Alice 스킬을 쓰는 것처럼
        /// 보였던 원인) — 끝에서 두번째 구간(진짜 이름)을 먼저 확인하고, 끝 구간은 "Vanilla"
        /// 처럼 모든 프리팹에 공통인 변형명이면 매칭 신호로 쓰지 않는다.
        /// </summary>
        private static bool MatchesPrefabName(string prefabAddress, string prefabName)
        {
            if (string.IsNullOrEmpty(prefabAddress)) return false;
            string[] parts = prefabAddress.Split('/');
            if (parts.Length >= 2)
            {
                string mid = parts[parts.Length - 2];
                if (!string.IsNullOrEmpty(mid) && prefabName.Contains(mid)) return true;
            }
            string last = parts[parts.Length - 1];
            if (string.IsNullOrEmpty(last) || last == "Vanilla") return false;
            return prefabName.Contains(last);
        }

        private UnitData SyntheticUnitData() => new UnitData
        {
            id = -1, name = "TestAlly", healthPoint = testHealthPoints, attackPoint = testAttackPoint,
            defensePoint = 0, attackSpeed = 1f, criticalMult = 150f, criticalRate = 10f, dodgeRate = 5f,
            skillIds = new System.Collections.Generic.List<int> { 900 }, color = Color.white,
        };

        private MonsterData SyntheticMonsterData() => new MonsterData
        {
            id = -1, name = "TestEnemy", healthPoint = (int)testHealthPoints, attackPoint = (int)testAttackPoint,
            defensePoint = 0, attackSpeed = 1f, criticalMult = 150f, criticalRate = 10, dodgeRate = 5,
            skillIds = new System.Collections.Generic.List<int> { 900 }, type = MonsterType.normal,
        };

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 700, 160));
            GUILayout.BeginHorizontal();

            DrawUnitPanel(
                _ally, allyPrefabsLength: _allyPrefabs?.Length ?? 0,
                onPrev: () => SpawnAlly(_allyIndex - 1), onNext: () => SpawnAlly(_allyIndex + 1),
                onBasicAttack: () => _ally?.ForceUseSkill(0), onSkill: () => _ally?.ForceUseSkill(1));

            GUILayout.FlexibleSpace();

            DrawUnitPanel(
                _enemy, allyPrefabsLength: _enemyPrefabs?.Length ?? 0,
                onPrev: () => SpawnEnemy(_enemyIndex - 1), onNext: () => SpawnEnemy(_enemyIndex + 1),
                onBasicAttack: () => _enemy?.ForceUseSkill(0), onSkill: () => _enemy?.ForceUseSkill(1));

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private static void DrawUnitPanel(Unit unit, int allyPrefabsLength,
            System.Action onPrev, System.Action onNext, System.Action onBasicAttack, System.Action onSkill)
        {
            GUILayout.BeginVertical(GUILayout.Width(320));

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◀", GUILayout.Width(40))) onPrev();
            GUILayout.Label(unit != null ? $"{unit.DisplayName ?? unit.gameObject.name} ({allyPrefabsLength}종 중)" : "없음",
                GUILayout.ExpandWidth(true));
            if (GUILayout.Button("▶", GUILayout.Width(40))) onNext();
            GUILayout.EndHorizontal();

            if (GUILayout.Button("기본공격 사용")) onBasicAttack();
            if (GUILayout.Button("스킬 사용")) onSkill();

            GUILayout.EndVertical();
        }
    }
}
#endif
