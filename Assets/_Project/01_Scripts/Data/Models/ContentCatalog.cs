using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OzGameLab01.Data
{
    /// <summary>Owned definitions; every read returns a detached DTO, including nested lists.</summary>
    public sealed class ContentTable<T> : IReadOnlyDictionary<int, T> where T : class
    {
        private readonly Dictionary<int, string> _rows = new Dictionary<int, string>();
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ContractResolver = new ContentFieldResolver(),
            TypeNameHandling = TypeNameHandling.None
        };

        public ContentTable(IEnumerable<T> rows, Func<T, int> key)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            foreach (T row in rows)
            {
                if (row == null) throw new ArgumentException($"Null {typeof(T).Name} row.");
                int id = key(row);
                if (_rows.ContainsKey(id)) throw new ArgumentException($"Duplicate {typeof(T).Name} ID {id}.");
                _rows.Add(id, JsonConvert.SerializeObject(row, Settings));
            }
        }

        public T this[int key] => JsonConvert.DeserializeObject<T>(_rows[key], Settings);
        public IEnumerable<int> Keys => _rows.Keys;
        public IEnumerable<T> Values => _rows.Keys.Select(key => this[key]);
        public int Count => _rows.Count;
        public bool ContainsKey(int key) => _rows.ContainsKey(key);
        public bool TryGetValue(int key, out T value)
        {
            if (!_rows.TryGetValue(key, out string json)) { value = null; return false; }
            value = JsonConvert.DeserializeObject<T>(json, Settings);
            return true;
        }
        public T Get(int key) => TryGetValue(key, out T value) ? value : null;
        public IEnumerator<KeyValuePair<int, T>> GetEnumerator()
        {
            foreach (int key in _rows.Keys) yield return new KeyValuePair<int, T>(key, this[key]);
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    // Content DTOs expose fields. In particular, Color's computed properties must not be serialized.
    internal sealed class ContentFieldResolver : DefaultContractResolver
    {
        protected override List<MemberInfo> GetSerializableMembers(Type objectType)
            => objectType.GetFields(BindingFlags.Public | BindingFlags.Instance).Cast<MemberInfo>().ToList();
    }

    public sealed class ContentCatalog
    {
        private readonly ContentTable<UnitData> _units;
        private readonly ContentTable<MonsterData> _enemies;
        public ContentTable<SkillData> Skills { get; }
        public ContentTable<SynergyData> Synergies { get; }
        public ContentTable<RelicData> Relics { get; }
        private readonly Dictionary<string, EventContent> _events = new Dictionary<string, EventContent>();
        public IReadOnlyList<EventContent> Events => _events.Values.Select(row => row.Copy()).ToList().AsReadOnly();
        public int EventCount => _events.Count;
        public EventContent GetEvent(string id)
            => id != null && _events.TryGetValue(id, out var value) ? value.Copy() : null;
        public IReadOnlyList<UnitData> Units => _units.Values.ToList().AsReadOnly();
        public IReadOnlyList<MonsterData> Enemies => _enemies.Values.ToList().AsReadOnly();
        public int UnitCount => _units.Count;
        public int EnemyCount => _enemies.Count;

        public ContentCatalog(IEnumerable<UnitData> units, IEnumerable<MonsterData> enemies,
            IEnumerable<SkillData> skills, IEnumerable<SynergyData> synergies, IEnumerable<RelicData> relics,
            IEnumerable<EventContent> events = null)
        {
            _units = new ContentTable<UnitData>(units, row => row.id);
            _enemies = new ContentTable<MonsterData>(enemies, row => row.id);
            Skills = new ContentTable<SkillData>(skills, row => row.id);
            Synergies = new ContentTable<SynergyData>(synergies, row => row.id);
            Relics = new ContentTable<RelicData>(relics, row => row.id);
            if (events != null)
                foreach (EventContent row in events)
                {
                    if (row == null || string.IsNullOrWhiteSpace(row.id)) throw new ArgumentException("Missing event ID.");
                    if (_events.ContainsKey(row.id)) throw new ArgumentException($"Duplicate event ID {row.id}.");
                    _events.Add(row.id, row.Copy());
                }
            if (_units.Count == 0 || _enemies.Count == 0 || Skills.Count == 0)
                throw new ArgumentException("Unit, enemy and skill content must not be empty.");
            foreach (UnitData unit in _units.Values) ValidateSkills(unit.skillIds, $"Unit {unit.id}");
            foreach (MonsterData enemy in _enemies.Values) ValidateSkills(enemy.skillIds, $"Enemy {enemy.id}");
        }

        private void ValidateSkills(IEnumerable<int> ids, string owner)
        {
            if (ids == null) return;
            foreach (int id in ids)
                if (!Skills.ContainsKey(id)) throw new ArgumentException($"{owner} references missing skill {id}.");
        }

        public UnitData GetUnit(int id) => _units.Get(id);
        public MonsterData GetEnemy(int id) => _enemies.Get(id);
        public SkillData GetSkill(int id) => Skills.Get(id);
        public SynergyData GetSynergy(int id) => Synergies.Get(id);
        public RelicData GetRelic(int id) => Relics.Get(id);
    }
}
