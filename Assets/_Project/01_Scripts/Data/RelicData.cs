using UnityEngine;
using System.Collections.Generic;

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
}
