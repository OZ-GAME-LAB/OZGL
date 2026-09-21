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
        private const int FinalBossFallbackStep = 3;
        private readonly Dictionary<Key, MonsterData> _prepared = new Dictionary<Key, MonsterData>();
        private long _contentRevision = -1;

        public int Count => _prepared.Count;

        public MonsterData Prepare(ContentCatalog content, long contentRevision, MonsterData baseData,
            int turnCount, int defeatedElites, int mapSeed, IReadOnlyList<UnitData> ownedUnits)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (baseData == null) return null;
            if (_contentRevision != contentRevision)
            {
                _prepared.Clear();
                _contentRevision = contentRevision;
            }

            int ownershipSignature = GetOwnershipSignature(ownedUnits);
            Key key = new Key(baseData.id, baseData.type, turnCount, defeatedElites, mapSeed, ownershipSignature);
            if (_prepared.TryGetValue(key, out MonsterData cached)) return Clone(cached);

            MonsterData prepared = Clone(baseData);
            EnemyGrowthRow growth = FindGrowth(content, baseData.type, turnCount, defeatedElites);
            if (growth != null)
            {
                prepared.healthPoint = Mathf.Max(1, Mathf.RoundToInt(growth.health));
                prepared.attackPoint = Mathf.Max(0, Mathf.RoundToInt(growth.attack));
                prepared.defensePoint = Mathf.Max(0, growth.defense);
                prepared.attackSpeed = Mathf.Max(0.01f, growth.attackInterval);
                prepared.criticalMult = Mathf.Max(0, growth.criticalMultiplier);
                prepared.criticalRate = Mathf.Max(0, Mathf.RoundToInt(growth.criticalChance));
                prepared.dodgeRate = Mathf.Clamp(Mathf.RoundToInt(growth.dodgeChance), 0, 100);
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

        private static EnemyGrowthRow FindGrowth(ContentCatalog content, MonsterType type, int turnCount, int defeatedElites)
        {
            MonsterType growthType = type == MonsterType.boss ? MonsterType.semiboss : type;
            // Final-boss rows are not authored yet. The agreed temporary rule is the
            // third semiboss stage regardless of when the boss battle is entered.
            int requestedStep = type == MonsterType.boss
                ? FinalBossFallbackStep
                : Mathf.Max(1, turnCount + defeatedElites + 1);
            EnemyGrowthRow best = null;
            foreach (EnemyGrowthRow row in content.EnemyGrowth.Values)
            {
                if (row == null || row.type != growthType) continue;
                if (best == null || Mathf.Abs(row.step - requestedStep) < Mathf.Abs(best.step - requestedStep) ||
                    (Mathf.Abs(row.step - requestedStep) == Mathf.Abs(best.step - requestedStep) && row.step > best.step))
                {
                    best = row;
                }
            }
            return best;
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
            unchecked { return mapSeed * 397 ^ key.EnemyId * 31 ^ key.Turn ^ key.Elites * 17 ^ key.OwnershipSignature; }
        }

        private static MonsterData Clone(MonsterData source)
        {
            return new MonsterData
            {
                id = source.id, name = source.name, spriteAddress = source.spriteAddress,
                healthPoint = source.healthPoint, attackPoint = source.attackPoint,
                defensePoint = source.defensePoint, criticalRate = source.criticalRate,
                criticalMult = source.criticalMult, dodgeRate = source.dodgeRate,
                attackSpeed = source.attackSpeed, skillCooldown = source.skillCooldown,
                type = source.type, skillIds = source.skillIds == null ? new List<int>() : new List<int>(source.skillIds)
            };
        }

        private readonly struct Key : IEquatable<Key>
        {
            public readonly int EnemyId, Turn, Elites, MapSeed, OwnershipSignature;
            public readonly MonsterType Type;
            public Key(int enemyId, MonsterType type, int turn, int elites, int mapSeed, int ownershipSignature)
            { EnemyId = enemyId; Type = type; Turn = turn; Elites = elites; MapSeed = mapSeed; OwnershipSignature = ownershipSignature; }
            public bool Equals(Key other) => EnemyId == other.EnemyId && Type == other.Type && Turn == other.Turn &&
                Elites == other.Elites && MapSeed == other.MapSeed && OwnershipSignature == other.OwnershipSignature;
            public override bool Equals(object obj) => obj is Key other && Equals(other);
            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = EnemyId;
                    hash = hash * 31 + (int)Type;
                    hash = hash * 31 + Turn;
                    hash = hash * 31 + Elites;
                    hash = hash * 31 + MapSeed;
                    return hash * 31 + OwnershipSignature;
                }
            }
        }
    }
}
