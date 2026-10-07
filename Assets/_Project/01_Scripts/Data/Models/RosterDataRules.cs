using System.Collections.Generic;

namespace OzGameLab01.Data
{
    /// <summary>로스터 스킬의 기존 항목 교체 규칙</summary>
    public static class RosterDataRules
    {
        public static void MergeSkills(List<SkillData> current, List<SkillData> incoming)
        {
            HashSet<int> ids = new HashSet<int>(incoming.ConvertAll(skill => skill.id));
            current.RemoveAll(skill => skill != null && ids.Contains(skill.id));
            current.AddRange(incoming);
        }

        /// <summary>기존 로스터의 첫 번째 일치 ID 우선 조회</summary>
        public static T FindFirst<T>(IReadOnlyList<T> items, int id, System.Func<T, int> getId) where T : class
        {
            for (int i = 0; i < items.Count; i++)
            {
                T item = items[i];
                if (item != null && getId(item) == id)
                {
                    return item;
                }
            }
            return null;
        }
    }
}