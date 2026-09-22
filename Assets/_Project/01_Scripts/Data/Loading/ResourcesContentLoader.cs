using System;
using System.Collections.Generic;
using UnityEngine;

namespace OzGameLab01.Data
{
    /// <summary>Migration adapter for existing runtime Resources, not a second definition cache.</summary>
    public static class ResourcesContentLoader
    {
        public static ContentCatalog Load(OzGameLab01.Events.EventDB eventDatabase = null)
        {
            UnitRosterData units = Resources.Load<UnitRosterData>("UnitRosterData");
            MonsterRosterData enemies = Resources.Load<MonsterRosterData>("MonsterRosterData");
            if (units == null || enemies == null) throw new InvalidOperationException("Missing unit/enemy roster.");
            // Parse JSON again on each reload. OnEnable alone does not reload already-loaded assets.
            TextAsset unitJson = Resources.Load<TextAsset>("TempUnitData");
            TextAsset enemyJson = Resources.Load<TextAsset>("EnemyData");
            if (unitJson == null || enemyJson == null) throw new InvalidOperationException("Missing unit/enemy JSON.");
            var skillDefinitions = new Dictionary<int, SkillData>();
            foreach (SkillData skill in units.SkillDefinitions) skillDefinitions.Add(skill.id, skill);
            var overrides = GameDataLoader.LoadUnitSkills();
            if (overrides == null) throw new InvalidOperationException("Missing unit skill JSON.");
            foreach (SkillData skill in overrides) skillDefinitions[skill.id] = skill;
            // Preserve the existing enemy-first skill resolver when legacy IDs overlap.
            foreach (SkillData skill in enemies.SkillDefinitions) skillDefinitions[skill.id] = skill;
            return new ContentCatalog(UnitRosterData.ParseUnitList(unitJson.text),
                MonsterRosterData.ParseMonsterList(enemyJson.text), skillDefinitions.Values,
                GameDataLoader.LoadSynergies(), GameDataLoader.LoadRelics(), LoadEvents(eventDatabase));
        }

        internal static IEnumerable<EventContent> LoadEvents(OzGameLab01.Events.EventDB database)
        {
            if (database == null) yield break;
            var pools = new[] { database.EventList_Battle, database.EventList_Quiz, database.EventList_Relic,
                database.EventList_Finish, database.EventList_Event };
            var kinds = new[] { OzGameLab01.Events.EventChoiceCategory.Battle, OzGameLab01.Events.EventChoiceCategory.Quiz,
                OzGameLab01.Events.EventChoiceCategory.Relic, OzGameLab01.Events.EventChoiceCategory.Exit,
                OzGameLab01.Events.EventChoiceCategory.Event };
            for (int i = 0; i < pools.Length; i++)
                foreach (var asset in pools[i])
                    if (asset != null && !string.IsNullOrWhiteSpace(asset.id)) yield return EventContent.FromAsset(asset, kinds[i]);
        }
    }
}
