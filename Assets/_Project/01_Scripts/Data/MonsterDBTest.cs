//using UnityEngine;
//using System.Threading.Tasks;

//public class MonsterDBTest : MonoBehaviour
//{
//    public static readonly GameDB<MonsterData, MonsterDataList> monsters = new();

//    private async void Start()
//    {
//        Debug.Log("<color=yellow>[DB Test] 몬스터 데이터 로드 시작...</color>");

//        // 1. 데이터베이스 로드 실행 (DataManager 클래스 명칭에 맞춰 호출)
//        await LoadDatabase();

//        // 2. 전체 데이터 개수 검증
//        var monsterList = monsters.GetAll();
//        Debug.Log($"<color=green>[DB Test] 로드 완료! 총 몬스터 수: {monsterList.Count}</color>");

//        // 3. 로드된 데이터 내용 확인 (콘솔 출력)
//        foreach (var monster in monsterList)
//        {
//            // MonsterData 내부 필드(id, name 등)를 확인
//            Debug.Log($"[Monster ID: {monster.id}] 이름: {monster.name}, 스프라이트: {monster.spriteAddress}");
//        }

//        // 4. 단일 ID 탐색(Get) 테스트 (예: 1번 몬스터 또는 MON_001)
//        if (monsterList.Count > 0)
//        {
//            var firstMonster = monsterList[0];
//            var testGet = monsters.Get(firstMonster.id);

//            if (testGet != null)
//            {
//                Debug.Log($"<color=cyan>[Get Test 성공] ID: {firstMonster.id} 탐색 확인</color>");
//            }
//            else
//            {
//                Debug.LogError($"[Get Test 실패] ID: {firstMonster.id} 데이터를 찾을 수 없습니다.");
//            }
//        }
//    }

//    public static async Task LoadDatabase()
//    {
//        await Task.WhenAll(
//            monsters.LoadAsync("Docs/JSON/Test_Monster")
//        );
//    }
//}