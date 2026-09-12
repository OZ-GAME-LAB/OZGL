using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

[System.Serializable]
public class SynergyDataList : IDataList<SynergyData>
{
    public List<SynergyData> synergyList = new();
    public List<SynergyData> GetList() => synergyList;
}

/// <summary>
/// 개별 시너지 데이터
/// </summary>
[System.Serializable]
public class SynergyData
{
    public int id;
    public string name;
    public string subTitle;
    public string iconAddress;

    public List<SynergyTier> tiers = new();
}

/// <summary>
/// 필요 기물 수에 따른 단계별 상세 효과
/// </summary>
[System.Serializable]
public class SynergyTier
{
    public int tierLevel;           // 단계
    public int requiredCount;       // 필요 기물 수
    public string description;      // 효과별 설명

    // 세부 효과 목록
    public List<SynergyEffectNode> effects = new();
}

/// <summary>
/// 런타임 계산 및 스탯 연산에 쓸 단일 효과 노드
/// </summary>
[System.Serializable]
public class SynergyEffectNode
{
    public SynergyTargetType targetType;        // 효과 적용 대상

    public string effectType;                   // 효과 식별자
    public string statType;                     // 스탯 종류
    public float value;                         // 기본 수치
    public float extraParam;                    // 보조 수치(복합 효과 시 적용)
}

/// <summary>
/// 시너지 효과 적용 대상
/// </summary>
public enum SynergyTargetType
{
    SelfSynergy,        // 해당 시너지를 지닌 기물
    AllAllies,          // 모든 아군 기물
    HighestHPUnit,      // 가장 체력이 높은 유닛
    SpecificUnit        // 특정 유닛
}