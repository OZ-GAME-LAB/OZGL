using System.Collections.Generic;
using UnityEngine;

namespace OzGameLab01.Data
{
    /// <summary>로스터 리소스 로드와 역직렬화 결과 적용</summary>
    public static class RosterDataLoader
    {
        public static void LoadUnit(ref List<UnitData> unitStats, List<SkillData> skillDefinitions, Object context)
        {
            // 공용 기본공격과 기본 스킬 정의 주입
            List<SkillData> tempSkills = TempRosterSeed.CreateAllTempUnitSkills();
            RosterDataRules.MergeSkills(skillDefinitions, tempSkills);

            // 기본 스킬보다 우선하는 유닛별 JSON 스킬 정의
            List<SkillData> unitSkills = GameDataLoader.LoadUnitSkills();
            if (unitSkills != null)
            {
                RosterDataRules.MergeSkills(skillDefinitions, unitSkills);
            }

            // 기존 유닛 리소스 경로와 실패 시 저장값 유지 정책
            TextAsset jsonFile = Resources.Load<TextAsset>("UnitData");
            if (jsonFile == null)
            {
                Debug.LogWarning("[UnitRosterData] 05_Data/Resources/UnitData.json을 찾을 수 없어 UnitStats가 마지막으로 저장된 값 그대로 유지됩니다.", context);
                return;
            }

            List<UnitData> parsed;
            try
            {
                parsed = JsonDataParser.ParseOptional<UnitData, UnitDataList>(jsonFile.text);
            }
            catch (Newtonsoft.Json.JsonException e)
            {
                Debug.LogWarning($"[UnitRosterData] UnitData.json 파싱에 실패해 UnitStats가 마지막으로 저장된 값 그대로 유지됩니다. ({e.Message})", context);
                return;
            }

            if (parsed == null)
            {
                Debug.LogWarning("[UnitRosterData] UnitData.json에 unitList가 없어 UnitStats가 마지막으로 저장된 값 그대로 유지됩니다.", context);
                return;
            }

            unitStats = parsed;
        }

        public static void LoadMonster(ref List<MonsterData> monsterStats, Object context)
        {

            // 턴별 성장 적용 전 몬스터 기본 데이터 로드
            TextAsset jsonFile = Resources.Load<TextAsset>("EnemyData");
            if (jsonFile == null)
            {
                Debug.LogWarning("[MonsterRosterData] 05_Data/Resources/EnemyData.json을 찾을 수 없어 monsterStats가 마지막으로 저장된 값 그대로 유지됩니다.", context);
                return;
            }

            List<MonsterData> parsed;
            try
            {
                parsed = JsonDataParser.ParseOptional<MonsterData, MonsterDataList>(jsonFile.text);
            }
            catch (Newtonsoft.Json.JsonException e)
            {
                Debug.LogWarning($"[MonsterRosterData] EnemyData.json 파싱에 실패해 monsterStats가 마지막으로 저장된 값 그대로 유지됩니다. ({e.Message})", context);
                return;
            }

            if (parsed == null)
            {
                Debug.LogWarning("[MonsterRosterData] EnemyData.json에 monsterList가 없어 monsterStats가 마지막으로 저장된 값 그대로 유지됩니다.", context);
                return;
            }

            monsterStats = parsed;
        }
    }
}
