using System.Collections.Generic;
using UnityEngine;

namespace OZGL.Data
{
    // 유니티 프로젝트 창 우클릭(Create) 메뉴에서 데이터를 생성할 수 있도록 속성 추가
    [CreateAssetMenu(fileName = "NewMapTheme", menuName = "OZGL/Data/MapTheme")]
    public class MapThemeData : ScriptableObject
    {
        [Header("필수 타일들")]
        public GameObject NormalPrefab;
        public GameObject BossPrefab;
        public GameObject BattlePrefab;
        public GameObject EventPrefab;
        public GameObject ShopPrefab;
        public GameObject ElitePrefab;

        [Header("장애물 타일")]
        [Tooltip("비워두면 맵 생성 시 해당 장애물 생성 로직을 무시합니다.")]
        public List<GameObject> TreePrefabs;
        public List<GameObject> RockPrefabs;

        [Header("웅덩이, 호수 생성 시스템")]
        [Tooltip("4가지 물 프리팹 중 하나라도 누락되면 물 지형을 생성하지 않습니다.")]
        public GameObject WaterPuddlePrefab;
        public GameObject WaterStartPrefab;
        public List<GameObject> WaterBodyPrefabs;
        public GameObject WaterEndPrefab;
    }
}