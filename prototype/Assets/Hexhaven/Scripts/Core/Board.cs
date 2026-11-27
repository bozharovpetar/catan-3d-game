using System;
using System.Collections.Generic;

namespace Hexhaven.Core
{
    public class Tile
    {
        public int Id;
        public int Q, R;
        public HexTerrain Terrain;
        public int Number;          // 0 for Ruins
        public double X, Z;         // world-space centre (hex radius = 1)
        public int[] Corners = new int[6];  // vertex ids, corner i at angle 60*i - 30 degrees
        public readonly List<int> Neighbors = new List<int>();
        public int Resource { get { return Res.FromTerrain(Terrain); } }
    }

    public class Vertex
    {
        public int Id;
        public double X, Z;
        public readonly List<int> Tiles = new List<int>();
        public readonly List<int> Edges = new List<int>();
        public readonly List<int> Neighbors = new List<int>();
        public int Owner = -1;
        public Building Building = Building.None;
        public int Harbor = -1;     // -1 none, 0..4 resource (2:1), Harbor.Generic (3:1)
    }

    public class Edge
    {
        public int Id;
        public int A, B;            // vertex ids
        public int Owner = -1;
        public readonly List<int> Tiles = new List<int>();
        public int Other(int v) { return v == A ? B : A; }
    }

    public class HarborInfo
    {
        public const int Generic = 5;
        public int EdgeId;
        public int Type;            // 0..4 resource or Generic
        public double X, Z;         // marker position just offshore
        public double OutX, OutZ;   // unit vector pointing out to sea
    }

    public class Board
    {
        public const double Sqrt3 = 1.7320508075688772;
        public const int Radius = 2;

        public readonly List<Tile> Tiles = new List<Tile>();
        public readonly List<Vertex> Vertices = new List<Vertex>();
        public readonly List<Edge> Edges = new List<Edge>();
        public readonly List<HarborInfo> Harbors = new List<HarborInfo>();

        static readonly int[] NumberPool = { 2, 3, 3, 4, 4, 5, 5, 6, 6, 8, 8, 9, 9, 10, 10, 11, 11, 12 };
        static readonly HexTerrain[] TerrainPool =
        {
            HexTerrain.Forest, HexTerrain.Forest, HexTerrain.Forest, HexTerrain.Forest,
            HexTerrain.ClayPit, HexTerrain.ClayPit, HexTerrain.ClayPit,
            HexTerrain.FlaxField, HexTerrain.FlaxField, HexTerrain.FlaxField, HexTerrain.FlaxField,
            HexTerrain.Farmland, HexTerrain.Farmland, HexTerrain.Farmland, HexTerrain.Farmland,
            HexTerrain.Mine, HexTerrain.Mine, HexTerrain.Mine,
            HexTerrain.Ruins
        };

        public static void HexToWorld(int q, int r, out double x, out double z)
        {
            x = Sqrt3 * (q + r / 2.0);
            z = -1.5 * r;
        }

        public static Board Generate(Random rng)
        {
            var b = new Board();
            b.BuildGeometry();
            b.AssignTerrainAndNumbers(rng);
            b.PlaceHarbors(rng);
            return b;
        }

        void BuildGeometry()
        {
            var vertexByKey = new Dictionary<long, int>();
            var edgeByKey = new Dictionary<long, int>();
            var tileByHex = new Dictionary<long, int>();

            for (int q = -Radius; q <= Radius; q++)
            {
                int rMin = Math.Max(-Radius, -q - Radius), rMax = Math.Min(Radius, -q + Radius);
                for (int r = rMin; r <= rMax; r++)
                {
                    var t = new Tile { Id = Tiles.Count, Q = q, R = r };
                    HexToWorld(q, r, out t.X, out t.Z);
                    Tiles.Add(t);
                    tileByHex[HexKey(q, r)] = t.Id;
                }
            }

            foreach (var t in Tiles)
            {
                for (int i = 0; i < 6; i++)
                {
                    double a = Math.PI / 180.0 * (60 * i - 30);
                    double vx = t.X + Math.Cos(a), vz = t.Z + Math.Sin(a);
                    long key = PosKey(vx, vz);
                    int vid;
                    if (!vertexByKey.TryGetValue(key, out vid))
                    {
                        vid = Vertices.Count;
                        Vertices.Add(new Vertex { Id = vid, X = vx, Z = vz });
                        vertexByKey[key] = vid;
                    }
                    t.Corners[i] = vid;
                    Vertices[vid].Tiles.Add(t.Id);
                }
                for (int i = 0; i < 6; i++)
                {
                    int a = t.Corners[i], c = t.Corners[(i + 1) % 6];
                    long key = ((long)Math.Min(a, c) << 32) | (uint)Math.Max(a, c);
                    int eid;
                    if (!edgeByKey.TryGetValue(key, out eid))
                    {
                        eid = Edges.Count;
                        Edges.Add(new Edge { Id = eid, A = a, B = c });
                        edgeByKey[key] = eid;
                        Vertices[a].Edges.Add(eid);
                        Vertices[c].Edges.Add(eid);
                        Vertices[a].Neighbors.Add(c);
                        Vertices[c].Neighbors.Add(a);
                    }
                    Edges[eid].Tiles.Add(t.Id);
                }
            }

            int[,] dirs = { { 1, 0 }, { 1, -1 }, { 0, -1 }, { -1, 0 }, { -1, 1 }, { 0, 1 } };
            foreach (var t in Tiles)
            {
                for (int d = 0; d < 6; d++)
                {
                    int n;
                    if (tileByHex.TryGetValue(HexKey(t.Q + dirs[d, 0], t.R + dirs[d, 1]), out n)) t.Neighbors.Add(n);
                }
            }
        }

        void AssignTerrainAndNumbers(Random rng)
        {
            var terrains = (HexTerrain[])TerrainPool.Clone();
            Shuffle(terrains, rng);
            for (int i = 0; i < Tiles.Count; i++) Tiles[i].Terrain = terrains[i];

            var numbers = (int[])NumberPool.Clone();
            for (int attempt = 0; attempt < 2000; attempt++)
            {
                Shuffle(numbers, rng);
                int k = 0;
                foreach (var t in Tiles) t.Number = t.Terrain == HexTerrain.Ruins ? 0 : numbers[k++];
                if (HotNumbersSeparated()) break;
            }
        }

        bool HotNumbersSeparated()
        {
            foreach (var t in Tiles)
            {
                if (t.Number != 6 && t.Number != 8) continue;
                foreach (int n in t.Neighbors)
                {
                    int m = Tiles[n].Number;
                    if (m == 6 || m == 8) return false;
                }
            }
            return true;
        }

        void PlaceHarbors(Random rng)
        {
            var coastal = new List<Edge>();
            foreach (var e in Edges) if (e.Tiles.Count == 1) coastal.Add(e);
            coastal.Sort((e1, e2) => Angle(e1).CompareTo(Angle(e2)));

            int[] types = { 0, 1, 2, 3, 4, HarborInfo.Generic, HarborInfo.Generic, HarborInfo.Generic, HarborInfo.Generic };
            Shuffle(types, rng);
            int offset = rng.Next(coastal.Count);
            for (int i = 0; i < types.Length; i++)
            {
                var e = coastal[(offset + (int)Math.Round(i * coastal.Count / (double)types.Length)) % coastal.Count];
                var tile = Tiles[e.Tiles[0]];
                var va = Vertices[e.A]; var vb = Vertices[e.B];
                double mx = (va.X + vb.X) / 2, mz = (va.Z + vb.Z) / 2;
                double ox = mx - tile.X, oz = mz - tile.Z;
                double len = Math.Sqrt(ox * ox + oz * oz);
                ox /= len; oz /= len;
                Harbors.Add(new HarborInfo { EdgeId = e.Id, Type = types[i], X = mx + ox * 0.45, Z = mz + oz * 0.45, OutX = ox, OutZ = oz });
                va.Harbor = types[i];
                vb.Harbor = types[i];
            }
        }

        double Angle(Edge e)
        {
            var a = Vertices[e.A]; var b = Vertices[e.B];
            return Math.Atan2((a.Z + b.Z) / 2, (a.X + b.X) / 2);
        }

        public int RuinsTile()
        {
            foreach (var t in Tiles) if (t.Terrain == HexTerrain.Ruins) return t.Id;
            return 0;
        }

        static long HexKey(int q, int r) { return ((long)q << 32) | (uint)r; }

        static long PosKey(double x, double z)
        {
            long ix = (long)Math.Round(x * 1000), iz = (long)Math.Round(z * 1000);
            return (ix << 32) ^ (iz & 0xffffffffL);
        }

        public static void Shuffle<T>(T[] arr, Random rng)
        {
            for (int i = arr.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                T tmp = arr[i]; arr[i] = arr[j]; arr[j] = tmp;
            }
        }
    }
}
