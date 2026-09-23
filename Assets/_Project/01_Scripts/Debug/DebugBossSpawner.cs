#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using OzGameLab01.Map;
using OzGameLab01.Data;
using UnityEngine.InputSystem;
using System.Reflection;

namespace OzGameLab01.DebugTools
{
    public class DebugBossSpawner : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            var go = new GameObject("DebugBossSpawner");
            DontDestroyOnLoad(go);
            go.AddComponent<DebugBossSpawner>();
        }

        private void Update()
        {
            // 02_MainGame 씬에서만 작동
            if (SceneManager.GetActiveScene().name != "02_MainGame")
                return;

            // F7 키 입력 감지 (New Input System)
            if (Keyboard.current != null && Keyboard.current.f7Key.wasPressedThisFrame)
            {
                SpawnBossNearby();
            }
        }

        private void SpawnBossNearby()
        {
            if (!BoardRunData.HasActiveRun || !BoardRunData.HasPlayerPosition)
            {
                Debug.LogWarning("[DebugBossSpawner] 활성화된 런이 없거나 플레이어 위치를 알 수 없습니다.");
                return;
            }

            // 1. 중간보스 처리 조건 모두 만족시키기
            // 내부 상태에 직접 접근하기 위해 리플렉션을 사용합니다.
            try
            {
                var stateField = typeof(BoardRunData).GetField("_state", BindingFlags.NonPublic | BindingFlags.Static);
                if (stateField != null)
                {
                    var state = stateField.GetValue(null);
                    // DefeatedElitesCount 프로퍼티에 충분히 큰 값을 넣어 조건을 강제 만족시킵니다.
                    var countProp = state.GetType().GetProperty("DefeatedElitesCount", BindingFlags.Public | BindingFlags.Instance);
                    if (countProp != null)
                    {
                        countProp.SetValue(state, 999);
                        Debug.Log("[DebugBossSpawner] 중간보스 처리 조건을 만족시켰습니다 (DefeatedElitesCount = 999).");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DebugBossSpawner] 중간보스 조건 강제 충족 실패: {e.Message}");
            }

            // 2. 플레이어 주변 4방향 중 하나에 보스 타일 생성
            MapGenerator mapGenerator = FindFirstObjectByType<MapGenerator>();
            if (mapGenerator == null)
            {
                Debug.LogError("[DebugBossSpawner] MapGenerator를 찾을 수 없습니다.");
                return;
            }

            Vector2Int playerPos = BoardRunData.PlayerPosition;
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(0, 1),   // 위
                new Vector2Int(0, -1),  // 아래
                new Vector2Int(-1, 0),  // 왼쪽
                new Vector2Int(1, 0)    // 오른쪽
            };

            foreach (var dir in directions)
            {
                Vector2Int targetPos = playerPos + dir;
                
                // 빈 타일이 아닌 위치(NodeDict에 존재하는 위치) 중 한 곳을 찾음
                if (mapGenerator.NodeDict.TryGetValue(targetPos, out MapNode node))
                {
                    // 보스 타일로 교체 (장애물/특수타일 무시)
                    node.Type = NodeType.Boss;
                    mapGenerator.ReplaceTileVisual(node);
                    Debug.Log($"[DebugBossSpawner] {targetPos} 위치를 보스 타일로 교체했습니다.");
                    return; // 하나만 교체하고 종료
                }
            }

            Debug.LogWarning("[DebugBossSpawner] 플레이어 주변 4방향에 교체 가능한 타일이 존재하지 않습니다.");
        }
    }
}
#endif
