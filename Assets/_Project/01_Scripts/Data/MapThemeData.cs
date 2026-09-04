using System.Collections.Generic;
using UnityEngine;

namespace OZGL.Data
{
    // 우클릭 에셋 창 크리에이트(Create) 메뉴에 항목을 띄워주는 속성 추가
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
        public GameObject UnitAcquisitionPrefab; // [추가됨] 유닛 획득 타일 프리팹

        [Header("장애물 타일")]
        [Tooltip("배열에 여러 개를 넣으면 랜덤으로 선택합니다.")]
        public List<GameObject> TreePrefabs;
        public List<GameObject> RockPrefabs;

        [Header("강, 호수 등 수계")]
        [Tooltip("4개 타일 중 최소 하나는 비어있지 않아야 합니다.")]
        public GameObject WaterPuddlePrefab;
        public GameObject WaterStartPrefab;
        public List<GameObject> WaterBodyPrefabs;
        public GameObject WaterEndPrefab;
    }
}