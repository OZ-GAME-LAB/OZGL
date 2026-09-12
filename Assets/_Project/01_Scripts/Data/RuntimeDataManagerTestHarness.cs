using UnityEngine;
using OzGameLab01.Combat;
using OzGameLab01.Managers;

/// <summary>
/// 데이터 파이프라인 전용 샌드박스(TestScenes/SandboxRuntimeDataPipeline.unity)에서
/// JSON 리소스가 역직렬화되어 RuntimeDataManager에 올바르게 캐싱되는지 확인합니다.
/// 실전투 씬을 켜지 않고도 UnitRosterData/MonsterRosterData를 로드시켜(OnEnable 트리거)
/// RuntimeDataManager.Units/Enemies/Synergies 값을 검증합니다.
/// </summary>
public class RuntimeDataManagerTestHarness : MonoBehaviour
{
    [SerializeField] private UnitRosterData unitRosterData;
    [SerializeField] private MonsterRosterData monsterRosterData;

    private void Start()
    {
        if (unitRosterData != null)
        {
            UnitRosterData.RegisterActive(unitRosterData, this);
        }

        RuntimeDataManager manager = RuntimeDataManager.Instance;

        Debug.Log($"[RuntimeDataManagerTest] Units={manager.Units.Count} Enemies={manager.Enemies.Count} Synergies={manager.Synergies.Count}");

        Check("Units.Count", manager.Units.Count, 21);
        Check("Enemies.Count", manager.Enemies.Count, 3);
        Check("Synergies.Count", manager.Synergies.Count, 12);

        CheckUnit(manager, 100, "앨리스");
        CheckUnit(manager, 120, "제페토");

        CheckEnemy(manager, 1, "Enemy_Melee");

        CheckSynergy(manager, 5001, "기사");
        CheckSynergy(manager, 5012, "공주");
    }

    private void CheckUnit(RuntimeDataManager manager, int id, string expectedName)
    {
        OzGameLab01.Data.UnitData unit = manager.GetUnit(id);
        if (unit == null)
        {
            Debug.LogError($"[RuntimeDataManagerTest] FAIL — GetUnit({id})가 null입니다.");
            return;
        }

        Check($"Unit({id}).name", unit.name, expectedName);
    }

    private void CheckEnemy(RuntimeDataManager manager, int id, string expectedName)
    {
        MonsterData enemy = manager.GetEnemy(id);
        if (enemy == null)
        {
            Debug.LogError($"[RuntimeDataManagerTest] FAIL — GetEnemy({id})가 null입니다.");
            return;
        }

        Check($"Enemy({id}).name", enemy.name, expectedName);
    }

    private void CheckSynergy(RuntimeDataManager manager, int id, string expectedName)
    {
        SynergyData synergy = manager.GetSynergy(id);
        if (synergy == null)
        {
            Debug.LogError($"[RuntimeDataManagerTest] FAIL — GetSynergy({id})가 null입니다.");
            return;
        }

        Check($"Synergy({id}).name", synergy.name, expectedName);
    }

    private static void Check(string label, object actual, object expected)
    {
        if (Equals(actual, expected))
        {
            Debug.Log($"[RuntimeDataManagerTest] PASS — {label} = {actual}");
        }
        else
        {
            Debug.LogError($"[RuntimeDataManagerTest] FAIL — {label}: 기대값 {expected}, 실제값 {actual}");
        }
    }
}
