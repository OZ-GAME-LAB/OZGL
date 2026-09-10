using UnityEngine;
using System.Collections.Generic;

namespace OzGameLab01.Data
{
    public class UnitDataList : IDataList<UnitData>
    {
        public List<UnitData> unitList;
        public List<UnitData> GetList() => unitList;
    }

    [System.Serializable]
    public class UnitData
    {
        public int id;
        public string name;
        public string spriteAddress;

        public float healthPoint;       // 체력
        public float attackPoint;       // 공격력
        public float defensePoint;      // 방어력
        public float attackSpeed;       // 공격 속도
        public float criticalMult;      // 치명타 배율
        public float criticalRate;      // 치명 확률
        public float dodgeRate;         // 회피율
        public int bloodDrain;          // 흡혈
        public float basicAttackCooldown = 1f;

        public List<int> skillIds = new List<int>();

        public string passiveSkillKey;
        public string activeSkillKey;
        public int skillCooldown;

        public string attackKey;

        public string synergy;
        public UnitTypeJob jobType;
        public UnitTypeTribe tribeType;

        public Color color = Color.white;
    }

    public enum UnitTypeJob
    {
        Assasin,
        Tricster,
        Sage,
        Knight,
        Healer,
        Shooter,
    }

    public enum UnitTypeTribe
    {
        Human,
        Beast,
        Fairy,
        Princess,
        Machine,
        Kid
    }
}