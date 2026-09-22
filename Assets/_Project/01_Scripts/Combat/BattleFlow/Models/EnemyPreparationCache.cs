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
        // 매 턴 value가 누적 성장한다: 낮 턴 +0.07, 밤 턴 +0.12 (엑셀 normal/nightEnemy 시트의
        // value 열 수식 그대로: previous turn 기준 mod(turn,15)<=10이면 낮, 아니면 밤).
        // 웨이브(15턴) 마감까지 중간보스를 못 잡으면 오버턴 진입 — value 성장은 그 시점에서
        // 멈추고, 별도의 오버턴 값이 턴당 +0.3씩 쌓인다. 중간보스를 잡으면 오버턴 값은 사라지고
        // value에 +1이 더해진 뒤 다시 평소처럼 성장한다. (2026-09-22, 사용자 확인)
        private const int WaveLength = 15;
        private const float ValueBase = 0.7f;
        private const float DayValueIncrement = 0.07f;
        private const float NightValueIncrement = 0.12f;
        private const float OverturnValueIncrement = 0.3f;
        private const int DayResidueCutoff = 10;

        // 공격속도/치명확률/회피율은 웨이브·중간보스와 무관하게 누적 턴 수에만 비례해서
        // 성장한다(엑셀 주석의 "3턴마다" 계단식 성장을 사용자 확인을 받아 턴당 선형으로 단순화).
        private const float AttackSpeedDecayPerTurn = 0.01f;
        private const float MinAttackSpeed = 0.01f;
        private const float CriticalRateGrowthPerTurn = 0.6f;
        private const float DodgeRateGrowthPerTurn = 0.3f;

        private readonly Dictionary<Key, MonsterData> _prepared = new Dictionary<Key, MonsterData>();
        private long _contentRevision = -1;

        public int Count => _prepared.Count;

        public MonsterData Prepare(long contentRevision, MonsterData baseData,
            int turnCount, int defeatedElites, int mapSeed, IReadOnlyList<UnitData> ownedUnits)
        {
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
            float value = ComputeValue(turnCount, defeatedElites);
            prepared.healthPoint = Mathf.Max(1, Mathf.RoundToInt(baseData.healthPoint * value));
            prepared.attackPoint = Mathf.Max(0, Mathf.RoundToInt(baseData.attackPoint * value));
            prepared.defensePoint = Mathf.Max(0, baseData.defensePoint * value);
            prepared.attackSpeed = Mathf.Max(MinAttackSpeed, baseData.attackSpeed - AttackSpeedDecayPerTurn * turnCount);
            prepared.criticalMult = Mathf.Max(0, baseData.criticalMult);
            prepared.criticalRate = Mathf.Max(0, Mathf.RoundToInt(baseData.criticalRate + CriticalRateGrowthPerTurn * turnCount));
            prepared.dodgeRate = Mathf.Clamp(Mathf.RoundToInt(baseData.dodgeRate + DodgeRateGrowthPerTurn * turnCount), 0, 100);

            prepared.skillIds = BuildSkillIds(prepared.skillIds, ownedUnits, MakeSeed(mapSeed, key));
            _prepared[key] = Clone(prepared);
            return prepared;
        }

        public void Clear()
        {
            _prepared.Clear();
            _contentRevision = -1;
        }

        /// <summary>
        /// 현재 턴/중간보스 처치 수로부터 HP/공격력/방어력에 곱할 value를 계산합니다.
        /// 웨이브(15턴) 마감을 처치 수가 못 따라잡았으면 오버턴 상태로 간주해 value를
        /// 마감 시점 값에 얼리고, 그 위에 오버턴 증가분만 얹습니다.
        /// </summary>
        private static float ComputeValue(int turnCount, int defeatedElites)
        {
            int wavesOnTime = Mathf.Min(defeatedElites, turnCount / WaveLength);
            int deadlineTurnCount = (wavesOnTime + 1) * WaveLength;
            int frozenTurnCount = Mathf.Min(turnCount, deadlineTurnCount - 1);
            float value = ComputeGrowthValue(frozenTurnCount);
            if (turnCount < deadlineTurnCount) return value;
            int overturnTurns = turnCount - deadlineTurnCount;
            return value + OverturnValueIncrement * overturnTurns;
        }

        /// <summary>
        /// 중간보스 처치가 웨이브 마감을 앞서거나 맞춰온, 정상 성장 구간의 value.
        /// turnCount는 0턴이 표시상 첫 턴(엑셀 시트의 1턴)이라 시트 턴 번호는 turnCount+1.
        /// </summary>
        private static float ComputeGrowthValue(int turnCount)
        {
            float value = ValueBase;
            int sheetTurn = turnCount + 1;
            for (int previousSheetTurn = 1; previousSheetTurn < sheetTurn; previousSheetTurn++)
            {
                int residue = previousSheetTurn % WaveLength;
                value += residue <= DayResidueCutoff ? DayValueIncrement : NightValueIncrement;
                if (residue == 0) value += 1f;
            }
            return value;
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
