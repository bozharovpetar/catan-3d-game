// SCAFFOLD — complete (geometry + ids are normative). Spec: docs/03-CORE-API.md §2.
using System;
using System.Collections.Generic;

namespace Hexhaven.Core
{
    /// <summary>A land hex.</summary>
    public sealed class Tile
    {
        public int Id, Q, R;
        public Terrain Terrain;
        public int Number;                              // 0 for Ruins
        public double X, Z;
        public readonly int[] Corners = new int[6];     // vertex id of corner i
        public readonly int[] Sides = new int[6];       // edge id of side s (corner s → s+1)
        public readonly List<int> Neighbors = new List<int>();

        public bool TryGetResource(out Resource r) { return TerrainInfo.TryGetResource(Terrain, out r); }
    }

    /// <summary>A corner where buildings stand.</summary>
    public sealed class Vertex
    {
        public int Id;
        public double X, Z;
        public readonly List<int> Tiles = new List<int>();
        public readonly List<int> Edges = new List<int>();
        public readonly List<int> Neighbors = new List<int>();   // parallel to Edges
        public HarborType Harbor = HarborType.None;
        public BuildingType Building = BuildingType.None;
        public int Owner = -1;
    }

    /// <summary>A border between two corners where paths go.</summary>
    public sealed class Edge
    {
        public int Id, A, B;
        public readonly List<int> Tiles = new List<int>();
        public int Owner = -1;
        public int HarborSlot = -1;

        public int Other(int vertex) { return vertex == A ? B : A; }
    }

    /// <summary>A harbor slot on a coastal edge.</summary>
    public sealed class Harbor
    {
        public int Slot, TileId, Side, EdgeId;
        public HarborType Type;
        public double X, Z;
        public double OutX, OutZ;
    }

    /// <summary>Board topology plus terrain/number/harbor assignment.</summary>
    public sealed class Board
    {
        public const int Radius = 2;
        public const int TileCount = 19, VertexCount = 54, EdgeCount = 72, HarborCount = 9;

        /// <summary>Harbor slots (01-RULES): (q, r, side).</summary>
        public static readonly int[,] HarborSlots =
        {
            { 2, 0, 0 }, { 2, -1, 1 }, { 1, -2, 1 }, { 0, -2, 2 }, { -1, -1, 3 },
            { -2, 1, 3 }, { -2, 2, 4 }, { -1, 2, 5 }, { 1, 1, 5 }
        };

        public readonly List<Tile> Tiles = new List<Tile>();
        public readonly List<Vertex> Vertices = new List<Vertex>();
        public readonly List<Edge> Edges = new List<Edge>();
        public readonly List<Harbor> Harbors = new List<Harbor>();

        readonly Dictionary<long, int> tileByAxial = new Dictionary<long, int>();

        public int RuinsTile
        {
            get { foreach (var t in Tiles) if (t.Terrain == Terrain.Ruins) return t.Id; return -1; }
        }

        public Tile TileAt(int q, int r)
        {
            int id;
            return tileByAxial.TryGetValue(AxialKey(q, r), out id) ? Tiles[id] : null;
        }

        public int EdgeBetween(int vA, int vB)
        {
            var v = Vertices[vA];
            for (int k = 0; k < v.Neighbors.Count; k++) if (v.Neighbors[k] == vB) return v.Edges[k];
            return -1;
        }

        /// <summary>Builds ids and adjacency exactly per 03-CORE-API §2. Terrain/numbers/harbor types unset.</summary>
        public static Board CreateGeometry()
        {
            var b = new Board();
            for (int q = -Radius; q <= Radius; q++)
            {
                int rMin = Math.Max(-Radius, -q - Radius), rMax = Math.Min(Radius, -q + Radius);
                for (int r = rMin; r <= rMax; r++)
                {
                    var t = new Tile { Id = b.Tiles.Count, Q = q, R = r };
                    HexMath.AxialToWorld(q, r, out t.X, out t.Z);
                    b.Tiles.Add(t);
                    b.tileByAxial[AxialKey(q, r)] = t.Id;
                }
            }

            var vertexByPos = new Dictionary<long, int>();
            foreach (var t in b.Tiles)
            {
                for (int i = 0; i < 6; i++)
                {
                    double x, z;
                    HexMath.Corner(t.X, t.Z, i, out x, out z);
                    long key = PosKey(x, z);
                    int vid;
                    if (!vertexByPos.TryGetValue(key, out vid))
                    {
                        vid = b.Vertices.Count;
                        b.Vertices.Add(new Vertex { Id = vid, X = x, Z = z });
                        vertexByPos[key] = vid;
                    }
                    t.Corners[i] = vid;
                    b.Vertices[vid].Tiles.Add(t.Id);
                }
            }

            var edgeByPair = new Dictionary<long, int>();
            foreach (var t in b.Tiles)
            {
                for (int s = 0; s < 6; s++)
                {
                    int a = t.Corners[s], c = t.Corners[(s + 1) % 6];
                    long key = ((long)Math.Min(a, c) << 32) | (uint)Math.Max(a, c);
                    int eid;
                    if (!edgeByPair.TryGetValue(key, out eid))
                    {
                        eid = b.Edges.Count;
                        b.Edges.Add(new Edge { Id = eid, A = a, B = c });
                        edgeByPair[key] = eid;
                        b.Vertices[a].Edges.Add(eid); b.Vertices[a].Neighbors.Add(c);
                        b.Vertices[c].Edges.Add(eid); b.Vertices[c].Neighbors.Add(a);
                    }
                    b.Edges[eid].Tiles.Add(t.Id);
                    t.Sides[s] = eid;
                }
            }

            foreach (var t in b.Tiles)
                for (int s = 0; s < 6; s++)
                {
                    var n = b.TileAt(t.Q + HexMath.SideDq[s], t.R + HexMath.SideDr[s]);
                    if (n != null) t.Neighbors.Add(n.Id);
                }

            for (int slot = 0; slot < HarborCount; slot++)
            {
                var t = b.TileAt(HarborSlots[slot, 0], HarborSlots[slot, 1]);
                int side = HarborSlots[slot, 2];
                int eid = t.Sides[side];
                var e = b.Edges[eid];
                if (e.Tiles.Count != 1) throw new InvalidOperationException("Harbor slot is not coastal: " + slot);
                e.HarborSlot = slot;
                double a = Math.PI / 180.0 * (60 * side);
                double ox = Math.Cos(a), oz = Math.Sin(a);
                var va = b.Vertices[e.A]; var vb = b.Vertices[e.B];
                double mx = (va.X + vb.X) / 2, mz = (va.Z + vb.Z) / 2;
                b.Harbors.Add(new Harbor
                {
                    Slot = slot, TileId = t.Id, Side = side, EdgeId = eid, Type = HarborType.None,
                    X = mx + ox * 0.45, Z = mz + oz * 0.45, OutX = ox, OutZ = oz
                });
            }
            return b;
        }

        /// <summary>Sets harbor types (slot order) and the corresponding vertex harbor flags.</summary>
        public void AssignHarborTypes(IList<HarborType> types)
        {
            for (int i = 0; i < Harbors.Count; i++)
            {
                Harbors[i].Type = types[i];
                var e = Edges[Harbors[i].EdgeId];
                Vertices[e.A].Harbor = types[i];
                Vertices[e.B].Harbor = types[i];
            }
        }

        static long AxialKey(int q, int r) { return ((long)q << 32) | (uint)r; }

        static long PosKey(double x, double z)
        {
            long ix = (long)Math.Round(x * 1000), iz = (long)Math.Round(z * 1000);
            return (ix << 32) ^ (iz & 0xffffffffL);
        }
    }
}
