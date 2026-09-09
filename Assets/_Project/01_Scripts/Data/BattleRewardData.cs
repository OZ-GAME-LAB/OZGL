using System;
using UnityEngine;

namespace OzGameLab01.Combat
{
    public enum BattleRewardKind
    {
        Experience,
        Relic,
        Unit
    }

    [Serializable]
    public struct BattleRewardData
    {
        public BattleRewardKind kind;
        public string description;
        public Sprite icon;
        public int targetId;
        public float amount;
    }
}
