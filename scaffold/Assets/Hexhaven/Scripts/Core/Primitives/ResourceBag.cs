// SCAFFOLD — complete. Spec: docs/03-CORE-API.md §1.1.
using System;
using System.Collections.Generic;

namespace Hexhaven.Core
{
    /// <summary>Counts of the five resources. Value type.</summary>
    public struct ResourceBag : IEquatable<ResourceBag>
    {
        public int Timber, Clay, Fiber, Grain, Iron;

        public static readonly ResourceBag Zero = new ResourceBag();

        public int this[Resource r]
        {
            get { return this[(int)r]; }
            set { this[(int)r] = value; }
        }

        public int this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0: return Timber;
                    case 1: return Clay;
                    case 2: return Fiber;
                    case 3: return Grain;
                    case 4: return Iron;
                    default: throw new ArgumentOutOfRangeException("i");
                }
            }
            set
            {
                switch (i)
                {
                    case 0: Timber = value; break;
                    case 1: Clay = value; break;
                    case 2: Fiber = value; break;
                    case 3: Grain = value; break;
                    case 4: Iron = value; break;
                    default: throw new ArgumentOutOfRangeException("i");
                }
            }
        }

        public int Total { get { return Timber + Clay + Fiber + Grain + Iron; } }
        public bool AnyNegative { get { return Timber < 0 || Clay < 0 || Fiber < 0 || Grain < 0 || Iron < 0; } }
        public bool IsEmpty { get { return Total == 0 && !AnyNegative; } }

        public int DistinctTypes
        {
            get { int n = 0; for (int i = 0; i < ResourceInfo.Count; i++) if (this[i] > 0) n++; return n; }
        }

        public static ResourceBag Of(int timber, int clay, int fiber, int grain, int iron)
        {
            return new ResourceBag { Timber = timber, Clay = clay, Fiber = fiber, Grain = grain, Iron = iron };
        }

        public static ResourceBag Single(Resource r, int count = 1)
        {
            var b = new ResourceBag();
            b[r] = count;
            return b;
        }

        public bool Contains(ResourceBag other)
        {
            return Timber >= other.Timber && Clay >= other.Clay && Fiber >= other.Fiber && Grain >= other.Grain && Iron >= other.Iron;
        }

        public bool SharesResourceWith(ResourceBag other)
        {
            for (int i = 0; i < ResourceInfo.Count; i++) if (this[i] > 0 && other[i] > 0) return true;
            return false;
        }

        public static ResourceBag operator +(ResourceBag a, ResourceBag b)
        {
            return Of(a.Timber + b.Timber, a.Clay + b.Clay, a.Fiber + b.Fiber, a.Grain + b.Grain, a.Iron + b.Iron);
        }

        public static ResourceBag operator -(ResourceBag a, ResourceBag b)
        {
            return Of(a.Timber - b.Timber, a.Clay - b.Clay, a.Fiber - b.Fiber, a.Grain - b.Grain, a.Iron - b.Iron);
        }

        public static bool operator ==(ResourceBag a, ResourceBag b) { return a.Equals(b); }
        public static bool operator !=(ResourceBag a, ResourceBag b) { return !a.Equals(b); }

        public bool Equals(ResourceBag o)
        {
            return Timber == o.Timber && Clay == o.Clay && Fiber == o.Fiber && Grain == o.Grain && Iron == o.Iron;
        }

        public override bool Equals(object obj) { return obj is ResourceBag && Equals((ResourceBag)obj); }

        public override int GetHashCode()
        {
            unchecked { return (((Timber * 31 + Clay) * 31 + Fiber) * 31 + Grain) * 31 + Iron; }
        }

        /// <summary>"t,c,f,g,i" — e.g. "1,0,2,0,3".</summary>
        public string ToCodec()
        {
            return Timber + "," + Clay + "," + Fiber + "," + Grain + "," + Iron;
        }

        public static ResourceBag FromCodec(string s)
        {
            var parts = s.Split(',');
            if (parts.Length != 5) throw new FormatException("ResourceBag codec needs 5 values: " + s);
            var b = new ResourceBag();
            for (int i = 0; i < 5; i++) b[i] = int.Parse(parts[i], System.Globalization.CultureInfo.InvariantCulture);
            return b;
        }

        public override string ToString()
        {
            var parts = new List<string>();
            for (int i = 0; i < ResourceInfo.Count; i++)
                if (this[i] != 0) parts.Add(this[i] + " " + ResourceInfo.Name((Resource)i));
            return parts.Count == 0 ? "nothing" : string.Join(", ", parts.ToArray());
        }
    }
}
