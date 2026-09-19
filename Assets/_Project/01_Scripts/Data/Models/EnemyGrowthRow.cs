namespace OzGameLab01.Data
{
    [System.Serializable]
    public sealed class EnemyGrowthRow
    {
        public int id;
        public MonsterType type;
        public int step;
        public float value;
        public float health;
        public float attack;
        public float defense;
        public float attackInterval;
        public float criticalMultiplier;
        public float criticalChance;
        public float dodgeChance;
    }
}
