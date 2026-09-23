#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.Combat;
using OzGameLab01.Data;
using OzGameLab01.Managers;

namespace OzGameLab01.DebugTools
{
    /// <summary>
    /// TestScenes/SkillVfxTest 전용 디버그 하니스. allyPrefab/enemyPrefab 슬롯에 테스트하고 싶은
    /// Ally_*/Enemy_* 프리팹을 꽂고 Play하면 실제 매니저(같은 씬의 GlobalManagers)가 살아있는
    /// 상태로 두 유닛이 스폰되고, 화면의 버튼으로 힐/상태이상/스탯 버프·디버프를 즉시 발동시켜
    /// CombatVfxLibrary가 재생하는 VFX를 바로 확인할 수 있다. 실제 게임 코드 경로는 건드리지
    /// 않고 CombatSession의 상태(State)만 채워서 Unit.Update()의 타겟 해석이 정상 동작하게 한다.
    /// </summary>
    public class SkillVfxTestHarness : MonoBehaviour
    {
        [Header("테스트할 유닛 프리팹을 여기에 꽂고 Play (Resources/Characters 하위 어떤 Ally_*/Enemy_*든 가능)")]
        [SerializeField] private Unit allyPrefab;
        [SerializeField] private Unit enemyPrefab;

        [Header("전투 세션 스텁 (씬에 비활성 상태로 있어야 함 — Awake가 돌면 안 됨)")]
        [SerializeField] private CombatSession sessionStub;

        [SerializeField] private Vector3 allySpawnPosition = new Vector3(-4f, -1.5f, 0f);
        [SerializeField] private Vector3 enemySpawnPosition = new Vector3(4f, -1.5f, 0f);

        [Header("합성 스탯 (프리팹에 대응하는 실제 데이터를 못 찾았을 때 폴백)")]
        [SerializeField] private float testHealthPoints = 999999f;
        [SerializeField] private float testAttackPoint = 3f;

        private Unit _ally;
        private Unit _enemy;

        private void Start()
        {
            _ally = Spawn(allyPrefab, allySpawnPosition, isAlly: true);
            _enemy = Spawn(enemyPrefab, enemySpawnPosition, isAlly: false);

            if (sessionStub != null)
            {
                sessionStub.State.SlotUnits[0, (int)CombatManager.SlotRow.Front] = _ally;
                sessionStub.State.EnemyUnit = _enemy;
            }
        }

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
            return unit;
        }

        private UnitData FindMatchingUnitData(string prefabName)
        {
            foreach (UnitData candidate in RuntimeContent.Catalog.Units)
            {
                if (!MatchesPrefabName(candidate.prefabAddress, prefabName)) continue;
                UnitData clone = new UnitData
                {
                    id = candidate.id, name = candidate.name, prefabAddress = candidate.prefabAddress,
                    healthPoint = testHealthPoints, attackPoint = candidate.attackPoint,
                    defensePoint = candidate.defensePoint, attackSpeed = candidate.attackSpeed,
                    criticalMult = candidate.criticalMult, criticalRate = candidate.criticalRate,
                    dodgeRate = candidate.dodgeRate, skillIds = candidate.skillIds, color = candidate.color,
                };
                Debug.Log($"[SkillVfxTestHarness] '{prefabName}' -> UnitData #{candidate.id} '{candidate.name}' 매칭됨 (실제 스킬 사용)");
                return clone;
            }
            Debug.LogWarning($"[SkillVfxTestHarness] '{prefabName}'에 대응하는 UnitData를 못 찾음 — 합성 스탯 사용(액티브 스킬 없음, 버튼으로만 테스트 가능)");
            return null;
        }

        private MonsterData FindMatchingMonsterData(string prefabName)
        {
            foreach (MonsterData candidate in RuntimeContent.Catalog.Enemies)
            {
                if (!MatchesPrefabName(candidate.prefabAddress, prefabName)) continue;
                MonsterData clone = new MonsterData
                {
                    id = candidate.id, name = candidate.name, prefabAddress = candidate.prefabAddress,
                    healthPoint = (int)testHealthPoints, attackPoint = candidate.attackPoint,
                    defensePoint = candidate.defensePoint, attackSpeed = candidate.attackSpeed,
                    criticalMult = candidate.criticalMult, criticalRate = candidate.criticalRate,
                    dodgeRate = candidate.dodgeRate, skillCooldown = candidate.skillCooldown,
                    skillIds = candidate.skillIds, type = candidate.type,
                };
                Debug.Log($"[SkillVfxTestHarness] '{prefabName}' -> MonsterData #{candidate.id} '{candidate.name}' 매칭됨 (실제 스킬 사용)");
                return clone;
            }
            // 일반/야간 몹(1,2)은 종족 프리팹이 랜덤 배정이라 프리팹명으로 매칭이 안 된다 — id=1(일반)로 폴백.
            MonsterData normal = RuntimeContent.Catalog.GetEnemy(1);
            if (normal != null)
            {
                Debug.Log($"[SkillVfxTestHarness] '{prefabName}'은 일반 몹 종족(랜덤 배정) — MonsterData #1로 폴백(실제 스킬 사용)");
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
            Debug.LogWarning($"[SkillVfxTestHarness] '{prefabName}'에 대응하는 MonsterData를 못 찾음 — 합성 스탯 사용(액티브 스킬 없음)");
            return null;
        }

        /// <summary>
        /// UnitData.prefabAddress는 "Units/Ally/Alice/Vanilla" 형식(끝에서 두번째 구간이 이름),
        /// MonsterData.prefabAddress는 "Characters/EnemyPrefabs/Enemy_Witch_Vanilla" 형식(끝
        /// 구간이 프리팹명 그대로)이라 두 관례를 모두 느슨하게 매칭한다.
        /// </summary>
        private static bool MatchesPrefabName(string prefabAddress, string prefabName)
        {
            if (string.IsNullOrEmpty(prefabAddress)) return false;
            string[] parts = prefabAddress.Split('/');
            string last = parts[parts.Length - 1];
            if (!string.IsNullOrEmpty(last) && prefabName.Contains(last)) return true;
            if (parts.Length >= 2)
            {
                string mid = parts[parts.Length - 2];
                if (!string.IsNullOrEmpty(mid) && prefabName.Contains(mid)) return true;
            }
            return false;
        }

        private UnitData SyntheticUnitData() => new UnitData
        {
            id = -1, name = "TestAlly", healthPoint = testHealthPoints, attackPoint = testAttackPoint,
            defensePoint = 0, attackSpeed = 1f, criticalMult = 150f, criticalRate = 10f, dodgeRate = 5f,
            skillIds = new List<int> { 900 }, color = Color.white,
        };

        private MonsterData SyntheticMonsterData() => new MonsterData
        {
            id = -1, name = "TestEnemy", healthPoint = (int)testHealthPoints, attackPoint = (int)testAttackPoint,
            defensePoint = 0, attackSpeed = 1f, criticalMult = 150f, criticalRate = 10, dodgeRate = 5,
            skillIds = new List<int> { 900 }, type = MonsterType.normal,
        };

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 600, 700));
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(290));
            GUILayout.Label($"Ally: {(_ally != null ? _ally.DisplayName ?? _ally.gameObject.name : "없음")}");
            DrawEffectButtons(_ally);
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.Width(290));
            GUILayout.Label($"Enemy: {(_enemy != null ? _enemy.DisplayName ?? _enemy.gameObject.name : "없음")}");
            DrawEffectButtons(_enemy);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private static void DrawEffectButtons(Unit unit)
        {
            if (unit == null)
            {
                GUILayout.Label("(프리팹 미배치)");
                return;
            }

            if (GUILayout.Button("Heal (+9999)")) unit.Heal(9999f);
            if (GUILayout.Button("Cleanse Debuffs")) unit.CleanseDebuffs();

            GUILayout.Space(6);
            if (GUILayout.Button("Stun 5s")) unit.ApplyDebuff(new DebuffProfile { type = DebuffType.Stun, duration = 5f });
            if (GUILayout.Button("Silence 5s")) unit.ApplyDebuff(new DebuffProfile { type = DebuffType.Silence, duration = 5f });
            if (GUILayout.Button("DamageOverTime 5s")) unit.ApplyDebuff(new DebuffProfile { type = DebuffType.DamageOverTime, duration = 5f, magnitude = 1f, tickInterval = 1f });

            GUILayout.Space(6);
            DrawStatPair(unit, "ATK", EffectStatType.Attack);
            DrawStatPair(unit, "DEF", EffectStatType.Defense);
            DrawStatPair(unit, "CRT%", EffectStatType.CriticalChance);
            DrawStatPair(unit, "CRT.DMG", EffectStatType.CriticalMultiplier);
            DrawStatPair(unit, "EVA", EffectStatType.DodgeChance);
            DrawStatPair(unit, "ATK.SPD", EffectStatType.AttackInterval);
        }

        private static void DrawStatPair(Unit unit, string label, EffectStatType stat)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(70));
            if (GUILayout.Button("+20%")) unit.ApplyStatEffect(stat, 20f, EffectOperation.Add, 0, true);
            if (GUILayout.Button("-20%")) unit.ApplyStatEffect(stat, -20f, EffectOperation.Add, 0, true);
            GUILayout.EndHorizontal();
        }
    }
}
#endif
