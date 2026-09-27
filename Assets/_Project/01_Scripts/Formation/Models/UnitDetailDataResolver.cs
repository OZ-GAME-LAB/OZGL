using System.Collections.Generic;
using System.Linq;
using OzGameLab01.Data;

namespace OzGameLab01.Formation
{
    public sealed class UnitDetailData
    {
        public SkillData ActiveSkill { get; }
        public SkillData PassiveSkill { get; }
        public IReadOnlyList<SynergyData> Synergies { get; }

        public UnitDetailData(SkillData activeSkill, SkillData passiveSkill, IReadOnlyList<SynergyData> synergies)
        {
            ActiveSkill = activeSkill;
            PassiveSkill = passiveSkill;
            Synergies = synergies;
        }
    }

    /// <summary>
    /// 전투용 UnitData와 공용 콘텐츠 카탈로그를 편성 상세 UI 데이터로 변환합니다.
    /// </summary>
    public static class UnitDetailDataResolver
    {
        public static UnitDetailData Resolve(
            UnitData unit,
            IEnumerable<SkillData> skills,
            IEnumerable<SynergyData> synergies)
        {
            if (unit == null)
                return new UnitDetailData(null, null, new List<SynergyData>());

            List<SkillData> skillList = skills?.Where(skill => skill != null).ToList() ?? new List<SkillData>();
            List<SynergyData> synergyList = synergies?.Where(synergy => synergy != null).ToList() ?? new List<SynergyData>();

            SkillData activeSkill = skillList.FirstOrDefault(skill => skill.id == unit.activeSkillId);
            SkillData passiveSkill = skillList.FirstOrDefault(skill => skill.id == unit.passiveSkillId);
            var resolvedSynergies = new List<SynergyData>(2);

            AddSynergy(resolvedSynergies, synergyList, GetJobSynergyId(unit.jobType));
            AddSynergy(resolvedSynergies, synergyList, GetTribeSynergyId(unit.tribeType));

            return new UnitDetailData(activeSkill, passiveSkill, resolvedSynergies);
        }

        public static int GetJobSynergyId(UnitTypeJob job)
        {
            return job switch
            {
                UnitTypeJob.Knight => 200,
                UnitTypeJob.Assasin => 201,
                UnitTypeJob.Shooter => 202,
                UnitTypeJob.Healer => 203,
                UnitTypeJob.Sage => 204,
                UnitTypeJob.Tricster => 205,
                _ => 0
            };
        }

        public static int GetTribeSynergyId(UnitTypeTribe tribe)
        {
            return tribe switch
            {
                UnitTypeTribe.Human => 206,
                UnitTypeTribe.Beast => 207,
                UnitTypeTribe.Fairy => 208,
                UnitTypeTribe.Kid => 209,
                UnitTypeTribe.Machine => 210,
                UnitTypeTribe.Princess => 211,
                _ => 0
            };
        }

        public static string GetDisplayName(SynergyData synergy)
        {
            if (synergy == null)
                return string.Empty;

            // 기존 편성/전투 시너지 명칭과 맞추기 위해 암살자 계열은 테마명인 '재간둥이'를 표시합니다.
            return synergy.id == 201 && !string.IsNullOrWhiteSpace(synergy.subTitle)
                ? synergy.subTitle
                : synergy.name;
        }

        private static void AddSynergy(List<SynergyData> target, List<SynergyData> source, int synergyId)
        {
            SynergyData synergy = source.FirstOrDefault(item => item.id == synergyId);
            if (synergy != null)
                target.Add(synergy);
        }
    }
}
