using UnityEngine;
using System.Collections.Generic;
using OzGameLab01.Combat;

namespace OzGameLab01.Data
{
    /// <summary>
    /// 유물의 기본 데이터 모델
    /// 이 형태를 따라 JSON 직렬화
    /// </summary>

    [System.Serializable]
    public class RelicDataList : IDataList<RelicData>
    {
        public List<RelicData> relicList;
        public List<RelicData> GetList() => relicList;
    }

    [System.Serializable]
    public class RelicData
    {
        public int id;                      // 유물 ID
        public string name;                 // 유물 명칭
        public string description;          // 유물 설명
        public string iconAddress;          // 유물 스프라이트의 어드레서블 주소
        public string relicLogic;           // 유물 로직 식별자

        public int baseValue;               // 유물의 고유 수치
        public float dropWeight;            // 전투 승리 보상 뽑기 가중치(RelicData.xlsx "확률" 열)
        public string targetScene;          // 효과가 적용되는 씬("보드씬"/"전투씬", RelicData.xlsx 원본 그대로)
        public List<EffectInstance> effects = new List<EffectInstance>();
    }
}

