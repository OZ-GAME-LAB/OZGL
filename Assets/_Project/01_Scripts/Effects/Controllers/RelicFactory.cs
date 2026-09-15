using UnityEngine;
using OzGameLab01.Interfaces;

namespace OzGameLab01.Data
{
    public static class RelicFactory
    {
        public static RelicLogic CreateLogic(string relicLogic)
        {
            return relicLogic switch
            {
                
                _ => null
            };
        }
    }
}
