using UnityEngine;

namespace OzGameLab01.Controllers
{
    public class MapTile : MonoBehaviour
    {
        public enum TileType { Normal, Combat, Event }

        public TileType tileType;
    }
}
