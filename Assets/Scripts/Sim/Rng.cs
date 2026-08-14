namespace KombiRush.Sim
{
    /// <summary>
    /// Deterministic xorshift32 RNG. The whole simulation must draw randomness from here
    /// and nowhere else, so a run is fully reproducible from (config, upgrades, seed).
    /// </summary>
    public sealed class Rng
    {
        private uint _state;

        public Rng(uint seed)
        {
            _state = seed == 0u ? 0x9E3779B9u : seed;
        }

        public uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        /// <summary>Uniform in [0, exclusiveMax). Returns 0 when exclusiveMax &lt;= 0.</summary>
        public int NextInt(int exclusiveMax)
        {
            if (exclusiveMax <= 0) return 0;
            return (int)(NextUInt() % (uint)exclusiveMax);
        }

        /// <summary>Uniform in [inclusiveMin, exclusiveMax).</summary>
        public int NextInt(int inclusiveMin, int exclusiveMax)
        {
            if (exclusiveMax <= inclusiveMin) return inclusiveMin;
            return inclusiveMin + NextInt(exclusiveMax - inclusiveMin);
        }

        /// <summary>Uniform in [0, 1).</summary>
        public float NextFloat()
        {
            return (NextUInt() >> 8) * (1.0f / 16777216.0f);
        }

        public float NextRange(float min, float max)
        {
            return min + (max - min) * NextFloat();
        }

        public bool Chance(float probability)
        {
            return NextFloat() < probability;
        }

        public uint State => _state;
    }
}
