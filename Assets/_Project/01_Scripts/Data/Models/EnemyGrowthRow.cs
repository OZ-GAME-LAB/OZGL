namespace OzGameLab01.Data
{
    [System.Serializable]
    public sealed class EnemyGrowthDataList : IDataList<EnemyGrowthRow>
    {
        public System.Collections.Generic.List<EnemyGrowthRow> enemyGrowthList;
        public System.Collections.Generic.List<EnemyGrowthRow> GetList() => enemyGrowthList;
    }

    [System.Serializable]
    public sealed class EnemyGrowthRow : IIdentifiable
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
        public int Id => id;
    }
}
