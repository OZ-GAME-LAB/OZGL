using UnityEngine;
namespace OzGameLab01.Board.Views
{
    // 씬 전환 중 보드 환경 표시
    public static class BoardEnvironmentView
    {
        public static void SetVisible(GameObject root, bool visible) { if (root != null) { root.SetActive(visible); } }
    }
}
