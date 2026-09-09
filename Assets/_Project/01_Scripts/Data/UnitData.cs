using UnityEngine;
using System.Collections.Generic;
using OzGameLab01.Combat;

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
        public float criticalRate;        // 치명 확률
        public float dodgeRate;         // 회피율
        public float basicAttackCooldown = 1f;

        public string passiveSkillKey;
        public string activeSkillKey;
        public int skillCooldown;

        public string attackKey;

        // 공용 아군 프리팹(Unit.Configure)이 생성 시 참조하는 시각/전투 클래스 정보
        public Color color = Color.white;
        public Unit.SkillType skillType;

        public UnitTypeJob jobType;
        public UnitTypeTribe tribeType;
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