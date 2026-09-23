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
            TextAsset unitJson = Resources.Load<TextAsset>("UnitData");
            TextAsset enemyJson = Resources.Load<TextAsset>("EnemyData");
            if (unitJson == null || enemyJson == null) throw new InvalidOperationException("Missing unit/enemy JSON.");
            var skillDefinitions = new Dictionary<int, SkillData>();
            // 통합 스킬 정의(SkillData.json)를 우선 채운다 — 유닛별/적별 스킬은 이 기반 위에 덮어쓴다.
            List<SkillData> commonSkills = GameDataLoader.LoadSkills();
            if (commonSkills != null)
            {
                foreach (SkillData skill in commonSkills) skillDefinitions[skill.id] = skill;
            }
            foreach (SkillData skill in units.SkillDefinitions) skillDefinitions[skill.id] = skill;
            // UnitSkillData.json은 삭제된 상태로도 유효하다(유닛별 스킬 오버라이드가 없다는 뜻) —
            // GameDataLoader.LoadUnitSkills()가 이미 없음을 경고로 남기므로 여기서 다시 막지 않는다.
            var overrides = GameDataLoader.LoadUnitSkills();
            if (overrides != null)
            {
                foreach (SkillData skill in overrides) skillDefinitions[skill.id] = skill;
            }
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
