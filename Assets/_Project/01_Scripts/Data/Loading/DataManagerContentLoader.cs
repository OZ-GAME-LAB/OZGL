using System;
using System.Collections.Generic;
using OzGameLab01.Managers;
using UnityEngine;

namespace OzGameLab01.Data
{
    /// <summary>
    /// Builds the gameplay snapshot from the Addressables-backed DataManager caches.
    /// ScriptableObject rosters currently provide only the shared basic/enemy skill definitions.
    /// </summary>
    public static class DataManagerContentLoader
    {
        public static ContentCatalog Load(OzGameLab01.Events.EventDB eventDatabase = null)
        {
            if (!DataManager.IsInitialized)
                throw new InvalidOperationException("DataManager is not initialized.");

            var skills = new Dictionary<int, SkillData>();
            UnitRosterData unitRoster = Resources.Load<UnitRosterData>("UnitRosterData");
            MonsterRosterData monsterRoster = Resources.Load<MonsterRosterData>("MonsterRosterData");
            AddSkills(skills, unitRoster?.SkillDefinitions);
            AddSkills(skills, DataManager.Skills.GetAll().Values);
            // Preserve the existing enemy-first override rule when IDs overlap.
            AddSkills(skills, monsterRoster?.SkillDefinitions);

            return new ContentCatalog(
                DataManager.Units.GetAll().Values,
                DataManager.Monsters.GetAll().Values,
                skills.Values,
                DataManager.Synergies.GetAll().Values,
                DataManager.Relics.GetAll().Values,
                ResourcesContentLoader.LoadEvents(eventDatabase));
        }

        private static void AddSkills(IDictionary<int, SkillData> destination, IEnumerable<SkillData> source)
        {
            if (source == null) return;
            foreach (SkillData skill in source)
            {
                if (skill != null) destination[skill.id] = skill;
            }
        }
    }
}
