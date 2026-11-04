// SCAFFOLD — complete. Spec: docs/03-CORE-API.md §1.2.
using System;

namespace Hexhaven.Core
{
    /// <summary>Pointy-top axial hex geometry. Hex size (centre to corner) = 1 world unit.</summary>
    public static class HexMath
    {
        public const double Sqrt3 = 1.7320508075688772;
        public const double HexSize = 1.0;

        /// <summary>Neighbour offsets across side s (side s spans corner s → s+1; outward angle 60·s°).</summary>
        public static readonly int[] SideDq = { 1, 1, 0, -1, -1, 0 };
        public static readonly int[] SideDr = { 0, -1, -1, 0, 1, 1 };

        /// <summary>x = √3·(q + r/2), z = −1.5·r.</summary>
        public static void AxialToWorld(int q, int r, out double x, out double z)
        {
            x = Sqrt3 * (q + r / 2.0) * HexSize;
            z = -1.5 * r * HexSize;
        }

        /// <summary>Corner i (0..5) at angle (60·i − 30)° from the centre.</summary>
        public static void Corner(double cx, double cz, int i, out double x, out double z)
        {
            double a = Math.PI / 180.0 * (60 * i - 30);
            x = cx + Math.Cos(a) * HexSize;
            z = cz + Math.Sin(a) * HexSize;
        }

        /// <summary>Number of 2d6 combinations rolling n (0 for 0, 7 or out of range).</summary>
        public static int Pips(int number)
        {
            if (number < 2 || number > 12 || number == 7) return 0;
            return 6 - Math.Abs(7 - number);
        }
    }
}
