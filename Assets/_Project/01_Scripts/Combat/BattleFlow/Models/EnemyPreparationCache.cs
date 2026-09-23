using System;
using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.Data;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// 전투 진입 전에 적의 성장 수치와 훔친 액티브 스킬을 한 번만 확정합니다.
    /// 입력 스냅샷이 바뀌면 새 키로 계산하며, 원본 콘텐츠는 수정하지 않습니다.
    /// </summary>
    public sealed class EnemyPreparationCache
    {
        // HP/공격력/방어력에 곱할 value는 더 이상 여기서 계산하지 않는다 — BoardRunState가
        // 매 턴 종료 시점의 낮/밤 상태로 직접 누적한 값을 그대로 받아서 곱하기만 한다.
        // 상세: Docs/ENEMY_SCALING_DESIGN.md 4-3절.

        // 공격속도/치명확률/회피율은 오버턴·중간보스와 무관하게 누적 턴 수에만 비례해서
        // 성장한다(엑셀 주석의 "3턴마다" 계단식 성장을 사용자 확인을 받아 턴당 선형으로 단순화).
        private const float AttackSpeedDecayPerTurn = 0.01f;
        private const float MinAttackSpeed = 0.01f;
        private const float CriticalRateGrowthPerTurn = 0.6f;
        private const float DodgeRateGrowthPerTurn = 0.3f;

        private readonly Dictionary<Key, MonsterData> _prepared = new Dictionary<Key, MonsterData>();
        private long _contentRevision = -1;

        public int Count => _prepared.Count;

        public MonsterData Prepare(long contentRevision, MonsterData baseData,
            int turnCount, float enemyGrowthValue, int mapSeed, IReadOnlyList<UnitData> ownedUnits)
        {
            if (baseData == null) return null;
            if (_contentRevision != contentRevision)
            {
                _prepared.Clear();
                _contentRevision = contentRevision;
            }

            int ownershipSignature = GetOwnershipSignature(ownedUnits);
            Key key = new Key(baseData.id, baseData.type, turnCount, enemyGrowthValue, mapSeed, ownershipSignature);
            if (_prepared.TryGetValue(key, out MonsterData cached)) return Clone(cached);

            MonsterData prepared = Clone(baseData);
            if (baseData.type == MonsterType.boss)
            {
                // 최종보스는 엑셀 finalbossEnemy 시트의 고정값을 그대로 쓴다 — 턴 진행에 따른
                // 성장 공식을 전혀 적용하지 않는다(2026-09-23, 사용자 확인).
            }
            else
            {
                prepared.healthPoint = Mathf.Max(1, Mathf.RoundToInt(baseData.healthPoint * enemyGrowthValue));
                prepared.attackPoint = Mathf.Max(0, Mathf.RoundToInt(baseData.attackPoint * enemyGrowthValue));
                prepared.defensePoint = Mathf.Max(0, baseData.defensePoint * enemyGrowthValue);
                prepared.attackSpeed = Mathf.Max(MinAttackSpeed, baseData.attackSpeed - AttackSpeedDecayPerTurn * turnCount);
                prepared.criticalMult = Mathf.Max(0, baseData.criticalMult);
                prepared.criticalRate = Mathf.Max(0, Mathf.RoundToInt(baseData.criticalRate + CriticalRateGrowthPerTurn * turnCount));
                prepared.dodgeRate = Mathf.Clamp(Mathf.RoundToInt(baseData.dodgeRate + DodgeRateGrowthPerTurn * turnCount), 0, 100);
            }

            prepared.skillIds = BuildSkillIds(prepared.skillIds, ownedUnits, MakeSeed(mapSeed, key));
            _prepared[key] = Clone(prepared);
            return prepared;
        }

        public void Clear()
        {
            _prepared.Clear();
            _contentRevision = -1;
        }

        private static List<int> BuildSkillIds(IReadOnlyList<int> baseSkillIds,
            IReadOnlyList<UnitData> ownedUnits, int seed)
        {
            var result = new List<int>();
            if (baseSkillIds != null && baseSkillIds.Count > 0) result.Add(baseSkillIds[0]);

            var candidates = new List<int>();
            if (ownedUnits != null)
                foreach (UnitData unit in ownedUnits)
                    if (unit?.skillIds != null && unit.skillIds.Count > 1)
                        candidates.Add(unit.skillIds[1]);

            IRandomProvider random = new CombatRandom(seed);
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                int swap = candidates[i]; candidates[i] = candidates[j]; candidates[j] = swap;
            }
            for (int i = 0; i < Mathf.Min(2, candidates.Count); i++) result.Add(candidates[i]);
            return result;
        }

        private static int GetOwnershipSignature(IReadOnlyList<UnitData> units)
        {
            unchecked
            {
                int hash = 17;
                if (units == null) return hash;
                foreach (UnitData unit in units)
                {
                    hash = hash * 31 + (unit?.id ?? 0);
                    if (unit?.skillIds == null) continue;
                    foreach (int skillId in unit.skillIds) hash = hash * 31 + skillId;
                }
                return hash;
            }
        }

        private static int MakeSeed(int mapSeed, Key key)
        {
            unchecked { return mapSeed * 397 ^ key.EnemyId * 31 ^ key.Turn ^ key.EnemyGrowthValue.GetHashCode() * 17 ^ key.OwnershipSignature; }
        }

        private static MonsterData Clone(MonsterData source)
        {
            return new MonsterData
            {
                id = source.id, name = source.name, spriteAddress = source.spriteAddress,
                prefabAddress = source.prefabAddress,
                healthPoint = source.healthPoint, attackPoint = source.attackPoint,
                defensePoint = source.defensePoint, criticalRate = source.criticalRate,
                criticalMult = source.criticalMult, dodgeRate = source.dodgeRate,
                attackSpeed = source.attackSpeed, skillCooldown = source.skillCooldown,
                type = source.type, skillIds = source.skillIds == null ? new List<int>() : new List<int>(source.skillIds)
            };
        }

        private readonly struct Key : IEquatable<Key>
        {
            public readonly int EnemyId, Turn, MapSeed, OwnershipSignature;
            public readonly float EnemyGrowthValue;
            public readonly MonsterType Type;
            public Key(int enemyId, MonsterType type, int turn, float enemyGrowthValue, int mapSeed, int ownershipSignature)
            { EnemyId = enemyId; Type = type; Turn = turn; EnemyGrowthValue = enemyGrowthValue; MapSeed = mapSeed; OwnershipSignature = ownershipSignature; }
            public bool Equals(Key other) => EnemyId == other.EnemyId && Type == other.Type && Turn == other.Turn &&
                EnemyGrowthValue.Equals(other.EnemyGrowthValue) && MapSeed == other.MapSeed && OwnershipSignature == other.OwnershipSignature;
            public override bool Equals(object obj) => obj is Key other && Equals(other);
            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = EnemyId;
                    hash = hash * 31 + (int)Type;
                    hash = hash * 31 + Turn;
                    hash = hash * 31 + EnemyGrowthValue.GetHashCode();
                    hash = hash * 31 + MapSeed;
                    return hash * 31 + OwnershipSignature;
                }
            }
        }
    }
}
