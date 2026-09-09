using System.Collections.Generic;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// 현재 전투에 참여 중인 Unit을 추적합니다. 예전 Unit.All(공개 static List)을 대체합니다.
    /// Unit이 자신의 생명주기(Awake/OnDisable/OnDestroy/Die)에서 스스로 등록·해제하고,
    /// 승패를 판정하는 쪽(CombatSceneController 등)은 Units로 읽기 전용 조회만 합니다.
    /// </summary>
    public static class BattleUnitRegistry
    {
        private static readonly List<Unit> _units = new List<Unit>();

        public static IReadOnlyList<Unit> Units => _units;

        public static void Register(Unit unit)
        {
            if (unit == null || _units.Contains(unit))
            {
                return;
            }

            _units.Add(unit);
        }

        public static void Unregister(Unit unit)
        {
            _units.Remove(unit);
        }

        /// <summary>
        /// 새 전투를 시작하기 전에 호출해, 이전 세션에서 비정상 종료 등으로 남아있을 수 있는
        /// 참조를 비웁니다(정적 상태이므로 실기기 빌드에서는 씬 전환만으로 초기화되지 않습니다).
        /// </summary>
        public static void Clear()
        {
            _units.Clear();
        }
    }
}
