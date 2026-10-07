using System;

namespace OzGameLab01.Combat
{
    public interface IRandomProvider
    {
        double NextDouble();
        int Next(int exclusiveMax);
    }

    public sealed class CombatRandom : IRandomProvider
    {
        private readonly Random _random;
        public CombatRandom() : this(Environment.TickCount) { }
        public CombatRandom(int seed) => _random = new Random(seed);
        public double NextDouble() => _random.NextDouble();
        public int Next(int exclusiveMax) => _random.Next(exclusiveMax);
    }
}
