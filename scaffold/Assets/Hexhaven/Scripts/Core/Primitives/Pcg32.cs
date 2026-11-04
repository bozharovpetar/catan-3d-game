// SCAFFOLD — complete, do not modify (determinism). Spec: docs/02-ARCHITECTURE.md §5.1.
using System.Collections.Generic;

namespace Hexhaven.Core
{
    /// <summary>PCG-XSH-RR 32-bit generator. Deterministic across platforms. State is fully serializable.</summary>
    public sealed class Pcg32
    {
        public ulong State;
        public ulong Inc;

        public Pcg32(ulong seed, ulong sequence = 54u)
        {
            State = 0UL;
            Inc = (sequence << 1) | 1UL;
            NextUInt();
            State += seed;
            NextUInt();
        }

        public uint NextUInt()
        {
            ulong old = State;
            State = unchecked(old * 6364136223846793005UL + Inc);
            uint xorshifted = (uint)(((old >> 18) ^ old) >> 27);
            int rot = (int)(old >> 59);
            return (xorshifted >> rot) | (xorshifted << ((-rot) & 31));
        }

        /// <summary>Uniform integer in [0, bound). Unbiased (rejection sampling).</summary>
        public int NextInt(int bound)
        {
            if (bound <= 0) throw new System.ArgumentOutOfRangeException("bound");
            uint b = (uint)bound;
            uint threshold = unchecked((uint)(-(int)b)) % b;
            while (true)
            {
                uint r = NextUInt();
                if (r >= threshold) return (int)(r % b);
            }
        }

        /// <summary>Uniform double in [0, 1).</summary>
        public double NextDouble() { return NextUInt() / 4294967296.0; }

        public int RollDie() { return NextInt(6) + 1; }

        public Pcg32 Clone()
        {
            var c = new Pcg32(0);
            c.State = State;
            c.Inc = Inc;
            return c;
        }

        /// <summary>Fisher–Yates shuffle, iterating from the end.</summary>
        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = NextInt(i + 1);
                T tmp = list[i]; list[i] = list[j]; list[j] = tmp;
            }
        }
    }
}
