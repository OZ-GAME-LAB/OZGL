using OzGameLab01.Map;

namespace OzGameLab01.Board.Controllers
{
    /// <summary>
    /// 타일 뷰가 구체 컨트롤러를 알지 않고 입력을 전달하도록 정의합니다.
    /// </summary>
    public interface IBoardTileInput
    {
        void OnTileHovered(TileView tile);
        void ClearHover();
        void OnTileClicked(TileView tile);
    }
}
