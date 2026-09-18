using System.Collections.Generic;
using UnityEngine;

namespace OzGameLab01.Data
{
    [System.Serializable]
    public class WeightedTile
    {
        public GameObject Prefab;

        [Tooltip("생성 가중치입니다. 값이 높을수록 더 자주 등장합니다.")]
        [Range(1, 100)]
        public int Weight = 10;
    }

    // 우클릭 에셋 창 크리에이트(Create) 메뉴에 항목을 띄워주는 속성 추가
    [CreateAssetMenu(fileName = "NewMapTheme", menuName = "OZGL/Data/MapTheme")]
    public class MapThemeData : ScriptableObject
    {
        [Header("Object Display Settings")]
        [Tooltip("타일 위에 생성되는 표시 오브젝트의 추가 크기 배율입니다.")]
        [Min(0.01f)]
        public float ObjectScaleMultiplier = 0.5f;

        [Tooltip("표시 오브젝트를 바닥 타일 위로 띄우는 높이입니다.")]
        public float ObjectHeightOffset = 0.1f;

        [Header("Weighted Normal Tiles")]
        [Tooltip("비어 있으면 기존 NormalPrefab을 사용합니다.")]
        public List<WeightedTile> NormalPrefabs;

        [Header("Split Base / Object Tiles")]
        public GameObject BattleBasePrefab;
        public GameObject BattleObjectPrefab;
        public GameObject EventBasePrefab;
        public GameObject EventObjectPrefab;
        public GameObject ShopBasePrefab;
        public GameObject ShopObjectPrefab;
        public GameObject UnitAcqBasePrefab;
        public GameObject UnitAcqObjectPrefab;
        public GameObject MidBossBasePrefab;
        public GameObject MidBossObjectPrefab;
        public GameObject BossBasePrefab;
        public GameObject BossObjectPrefab;

        [Header("Legacy Single Prefabs (Fallback)")]
        [Tooltip("새 Base/Object 필드가 비어 있을 때 기존 테마와의 호환을 위해 사용합니다.")]
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
