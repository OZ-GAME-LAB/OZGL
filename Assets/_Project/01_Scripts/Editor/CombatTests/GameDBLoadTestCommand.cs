using System;
using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using OzGameLab01.Managers;

namespace OzGameLab01.Tests.EditMode
{
    /// <summary>
    /// GameDB(어드레서블 JSON) 로딩이 실제로 동작하는지 라이브 에디터에서 확인하기 위한
    /// 임시 진입점. CliCommand는 동기 시그니처만 지원하는 것으로 보여, 비동기 로드는
    /// fire-and-forget으로 시작하고 별도 커맨드로 결과를 폴링합니다.
    /// </summary>
    public static class GameDBLoadTestCommand
    {
        private static string _lastResult = "(아직 실행 안 함)";

        [CliCommand("test_unit_gamedb", "DataManager.Units 어드레서블 로드를 시작합니다. 결과는 test_unit_gamedb_result로 폴링.")]
        public static string TestUnitGameDb()
        {
            _lastResult = "실행 중...";
            _ = RunAsync();
            return "시작됨. test_unit_gamedb_result로 결과 확인.";
        }

        [CliCommand("test_unit_gamedb_result", "가장 최근 test_unit_gamedb 실행 결과를 반환합니다.")]
        public static string TestUnitGameDbResult()
        {
            return _lastResult;
        }

        private static async Task RunAsync()
        {
            try
            {
                await DataManager.Units.LoadAsync("JSON/UnitJSON");
                var all = DataManager.Units.GetAll();
                if (all.Count == 0)
                {
                    _lastResult = "FAIL: 로드는 예외 없이 끝났지만 0개입니다.";
                    return;
                }

                var first = all[0];
                _lastResult = $"OK: count={all.Count}, first.id={first.id}, first.name={first.name}, " +
                              $"first.skillIds.Count={(first.skillIds?.Count ?? -1)}";
            }
            catch (Exception e)
            {
                _lastResult = $"EXCEPTION: {e}";
            }
        }
    }
}
