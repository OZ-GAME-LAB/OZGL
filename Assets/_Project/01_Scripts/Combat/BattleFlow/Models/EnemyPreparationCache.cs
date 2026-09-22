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

        // EnemyGrowthData.json의 normal/night 45행은 15스텝짜리 파도가 3번 반복되는 구조다
        // (낮 10턴 +0.07 → 밤 4턴 +0.12 → 중간보스 처치 파도 시작 +1.07 점프). 원본 엑셀
        // 셀 주석("중간보스를 처치하고 다시 낮이 되면 1 증가")과 실제 45행 수치 확인(2026-09-21)
        // 결과다.
        private const int WaveLength = 15;
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
            // 파도 번호는 "누적 턴 수로 자연스럽게 도달했을 파도"와 "중간보스 처치 수"
            // 중 큰 쪽을 쓴다 — 중간보스를 처치하면 그만큼 파도를 앞당겨 점프하고,
            // 처치 없이 턴만 흘러도 시간 경과만으로 계속 다음 파도로 넘어가(절대 약해지지
            // 않음) 원본 표의 낮/밤/점프 구조를 재현한다(2026-09-21, turnCount%15로 순환시켜
            // 15턴마다 최약체로 되돌아가던 첫 구현의 회귀를 수정).
            int waveIndex = Mathf.Max(turnCount / WaveLength, defeatedElites);
            int requestedStep = type == MonsterType.boss
                ? FinalBossFallbackStep
                : Mathf.Max(1, (waveIndex * WaveLength) + (turnCount % WaveLength) + 1);
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
