using UnityEngine;
using OzGameLab01.Interfaces;

public static class RelicFactory
{
    public static RelicLogic CreateLogic(string relicLogic)
    {
        return relicLogic switch
        {
            "AllAtkOne" => new AllUnitAtkPlusOne(),
            _ => null
        };
    }
}
