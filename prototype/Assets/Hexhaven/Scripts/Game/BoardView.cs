using System.Collections.Generic;
using Hexhaven.Core;
using UnityEngine;

namespace Hexhaven
{
    public enum PickKind { None, Vertex, Edge, Tile }

    /// <summary>Builds and updates the 3D representation of a <see cref="Game"/>.</summary>
    public class BoardView : MonoBehaviour
    {
        public const float TileTop = 0.25f;

        Game game;
        Transform staticRoot, pieceRoot, markerRoot;
        readonly Dictionary<int, GameObject> vertexPieces = new Dictionary<int, GameObject>();
        readonly Dictionary<int, Building> vertexPieceKind = new Dictionary<int, Building>();
        readonly Dictionary<int, GameObject> edgePieces = new Dictionary<int, GameObject>();
        readonly List<GameObject> markers = new List<GameObject>();
        GameObject storm;
        Vector3 stormTarget;

        // ------------------------------------------------------------------ helpers

        static Vector3 V(double x, double z, float y = TileTop) { return new Vector3((float)x, y, (float)z); }

        public Vector3 VertexPos(int v) { var vx = game.Board.Vertices[v]; return V(vx.X, vx.Z); }
        public Vector3 TilePos(int t) { var tl = game.Board.Tiles[t]; return V(tl.X, tl.Z); }
        public Vector3 EdgePos(int e)
        {
            var ed = game.Board.Edges[e];
            return (VertexPos(ed.A) + VertexPos(ed.B)) * 0.5f;
        }

        static GameObject Part(string name, Transform parent, Mesh mesh, Color color, Vector3 localPos, Quaternion localRot, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Palette.Mat(color);
            return go;
        }

        static GameObject Part(string name, Transform parent, Mesh mesh, Color color, Vector3 localPos)
        {
            return Part(name, parent, mesh, color, localPos, Quaternion.identity, Vector3.one);
        }

        static TextMesh Label(Transform parent, string text, Color color, Vector3 localPos, float size, bool billboard = true)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            var font = Palette.UIFont();
            tm.font = font;
            if (font != null) go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            tm.text = text;
            tm.color = color;
            tm.fontSize = 64;
            tm.characterSize = size;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            if (billboard) go.AddComponent<Billboard>();
            return tm;
        }

        // ------------------------------------------------------------------ static board

        public void Build(Game g, int seed)
        {
            game = g;
            Clear();
            staticRoot = new GameObject("Board").transform; staticRoot.SetParent(transform, false);
            pieceRoot = new GameObject("Pieces").transform; pieceRoot.SetParent(transform, false);
            markerRoot = new GameObject("Markers").transform; markerRoot.SetParent(transform, false);

            Part("Ocean", staticRoot, MeshFactory.Prism(48, 40f, 0.1f), Palette.Ocean, new Vector3(0, -0.2f, 0));
            Part("Shallows", staticRoot, MeshFactory.Prism(6, 5.6f, 0.1f, 0f), Color.Lerp(Palette.Ocean, Color.white, 0.25f), new Vector3(0, -0.15f, 0));
            Part("Beach", staticRoot, MeshFactory.Prism(6, 4.85f, 0.2f, 0f), Palette.Beach, new Vector3(0, -0.1f, 0));

            foreach (var t in g.Board.Tiles) BuildTile(t, new System.Random(seed * 31 + t.Id));
            foreach (var h in g.Board.Harbors) BuildHarbor(h);

            storm = BuildStorm();
            storm.transform.position = stormTarget = TilePos(g.StormTile) + Vector3.up * 1.3f;
            vertexPieces.Clear(); vertexPieceKind.Clear(); edgePieces.Clear();
        }

        void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            markers.Clear();
        }

        void BuildTile(Tile t, System.Random rng)
        {
            var root = new GameObject("Tile " + t.Id + " " + t.Terrain).transform;
            root.SetParent(staticRoot, false);
            root.localPosition = V(t.X, t.Z, 0);
            Color baseColor = Palette.TerrainColor(t.Terrain);
            Part("Base", root, MeshFactory.Prism(6, 0.97f, TileTop, -30f), baseColor, Vector3.zero);

            switch (t.Terrain)
            {
                case HexTerrain.Forest:
                    foreach (var p in Scatter(rng, 6, 0.38f, 0.72f))
                    {
                        float s = 0.8f + (float)rng.NextDouble() * 0.5f;
                        Part("Trunk", root, MeshFactory.Prism(5, 0.035f, 0.1f), Palette.Wood, p + Vector3.up * TileTop, Quaternion.identity, Vector3.one * s);
                        Part("Tree", root, MeshFactory.Pyramid(6, 0.13f, 0.38f), new Color(0.13f, 0.38f, 0.20f), p + Vector3.up * (TileTop + 0.08f * s), Quaternion.Euler(0, rng.Next(60), 0), Vector3.one * s);
                    }
                    break;
                case HexTerrain.ClayPit:
                    foreach (var p in Scatter(rng, 3, 0.42f, 0.66f))
                    {
                        Part("Pit", root, MeshFactory.Prism(7, 0.16f, 0.02f), new Color(0.55f, 0.28f, 0.18f), p + Vector3.up * TileTop);
                        for (int i = 0; i < 3; i++)
                            Part("Brick", root, MeshFactory.Box(0.12f, 0.05f, 0.06f), new Color(0.72f, 0.33f, 0.22f),
                                p + new Vector3(0.12f, TileTop + 0.05f * i, 0.04f), Quaternion.Euler(0, 20 * i + rng.Next(30), 0), Vector3.one);
                    }
                    break;
                case HexTerrain.FlaxField:
                case HexTerrain.Farmland:
                    {
                        bool flax = t.Terrain == HexTerrain.FlaxField;
                        Color crop = flax ? new Color(0.42f, 0.58f, 0.95f) : new Color(0.85f, 0.62f, 0.18f);
                        Color stem = flax ? new Color(0.38f, 0.60f, 0.32f) : new Color(0.80f, 0.66f, 0.30f);
                        float rot = rng.Next(3) * 60f;
                        var q = Quaternion.Euler(0, rot, 0);
                        for (int row = -3; row <= 3; row++)
                        {
                            for (int col = -3; col <= 3; col++)
                            {
                                Vector3 p = q * new Vector3(col * 0.2f, 0, row * 0.2f);
                                if (p.magnitude < 0.36f || p.magnitude > 0.76f) continue;
                                float h = flax ? 0.10f : 0.14f;
                                Part("Stem", root, MeshFactory.Box(0.05f, h, 0.05f), stem, p + Vector3.up * TileTop);
                                Part("Crop", root, flax ? MeshFactory.Box(0.07f, 0.04f, 0.07f) : MeshFactory.Pyramid(4, 0.05f, 0.08f, 45f), crop, p + Vector3.up * (TileTop + h));
                            }
                        }
                    }
                    break;
                case HexTerrain.Mine:
                    {
                        var peaks = Scatter(rng, 3, 0.45f, 0.62f);
                        for (int i = 0; i < peaks.Count; i++)
                        {
                            float s = 0.8f + (float)rng.NextDouble() * 0.5f;
                            Part("Peak", root, MeshFactory.Pyramid(5, 0.26f, 0.5f), new Color(0.44f, 0.45f, 0.50f), peaks[i] + Vector3.up * TileTop, Quaternion.Euler(0, rng.Next(72), 0), Vector3.one * s);
                            Part("Snow", root, MeshFactory.Pyramid(5, 0.09f, 0.17f), new Color(0.95f, 0.95f, 0.97f), peaks[i] + Vector3.up * (TileTop + 0.33f * s), Quaternion.identity, Vector3.one * s);
                        }
                        Vector3 m = peaks[0] * 0.7f;
                        Part("MineEntrance", root, MeshFactory.Box(0.12f, 0.1f, 0.06f), new Color(0.15f, 0.12f, 0.10f), m + Vector3.up * TileTop, Quaternion.LookRotation(-m), Vector3.one);
                    }
                    break;
                case HexTerrain.Ruins:
                    foreach (var p in Scatter(rng, 5, 0.3f, 0.7f))
                    {
                        float h = 0.08f + (float)rng.NextDouble() * 0.3f;
                        Part("Pillar", root, MeshFactory.Prism(8, 0.06f, h), new Color(0.78f, 0.74f, 0.68f), p + Vector3.up * TileTop);
                    }
                    Part("Slab", root, MeshFactory.Box(0.35f, 0.05f, 0.18f), new Color(0.72f, 0.68f, 0.62f), new Vector3(0, TileTop, 0), Quaternion.Euler(0, rng.Next(180), 0), Vector3.one);
                    break;
            }

            if (t.Number > 0)
            {
                Part("Token", root, MeshFactory.Prism(20, 0.27f, 0.05f), Palette.Token, Vector3.up * TileTop);
                bool hot = t.Number == 6 || t.Number == 8;
                string pips = new string('\u2022', Res.Pips(t.Number));
                var label = Label(root, t.Number + "\n" + pips, hot ? new Color(0.80f, 0.12f, 0.10f) : new Color(0.15f, 0.12f, 0.10f), Vector3.up * (TileTop + 0.32f), 0.045f);
                label.lineSpacing = 0.55f;
            }
        }

        /// <summary>Random points in a ring around the tile centre, kept apart from each other.</summary>
        static List<Vector3> Scatter(System.Random rng, int count, float minR, float maxR)
        {
            var pts = new List<Vector3>();
            for (int tries = 0; tries < 200 && pts.Count < count; tries++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = Mathf.Lerp(minR, maxR, (float)rng.NextDouble());
                var p = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                bool ok = true;
                foreach (var q in pts) if ((q - p).sqrMagnitude < 0.06f) { ok = false; break; }
                if (ok) pts.Add(p);
            }
            return pts;
        }

        void BuildHarbor(HarborInfo h)
        {
            var root = new GameObject("Harbor").transform;
            root.SetParent(staticRoot, false);
            var ed = game.Board.Edges[h.EdgeId];
            Vector3 dock = V(h.X, h.Z, 0.1f);
            root.localPosition = dock;
            root.localRotation = Quaternion.LookRotation(new Vector3((float)h.OutX, 0, (float)h.OutZ));
            Part("Dock", root, MeshFactory.Box(0.42f, 0.06f, 0.3f), Palette.Wood, Vector3.zero);
            foreach (int v in new[] { ed.A, ed.B })
            {
                Vector3 from = dock, to = V(game.Board.Vertices[v].X, game.Board.Vertices[v].Z, 0.12f);
                Vector3 mid = (from + to) / 2f;
                var plank = Part("Pier", staticRoot, MeshFactory.Box(0.06f, 0.04f, 1f), Palette.Wood, mid, Quaternion.LookRotation(to - from), new Vector3(1, 1, (to - from).magnitude));
                plank.transform.localPosition = mid;
            }
            bool generic = h.Type == HarborInfo.Generic;
            Color flag = generic ? Color.white : Palette.ResourceColors[h.Type];
            Part("Mast", root, MeshFactory.Prism(5, 0.015f, 0.4f), Palette.Wood, new Vector3(0.14f, 0.06f, 0));
            Part("Flag", root, MeshFactory.Box(0.02f, 0.12f, 0.16f), flag, new Vector3(0.14f, 0.32f, 0.08f));
            Label(root, generic ? "3:1" : "2:1\n" + Res.Names[h.Type], Color.white, new Vector3(0, 0.62f, 0), 0.032f);
        }

        GameObject BuildStorm()
        {
            var root = new GameObject("Storm");
            root.transform.SetParent(transform, false);
            var rng = new System.Random(7);
            for (int i = 0; i < 6; i++)
            {
                var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(puff.GetComponent<Collider>());
                puff.name = "Cloud";
                puff.transform.SetParent(root.transform, false);
                float a = i * 60f * Mathf.Deg2Rad;
                puff.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.25f, (float)rng.NextDouble() * 0.12f, Mathf.Sin(a) * 0.25f);
                puff.transform.localScale = Vector3.one * (0.32f + (float)rng.NextDouble() * 0.15f);
                puff.GetComponent<MeshRenderer>().sharedMaterial = Palette.Mat(Palette.Storm);
            }
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f * Mathf.Deg2Rad;
                Part("Rain", root.transform, MeshFactory.Box(0.015f, 0.35f, 0.015f), new Color(0.55f, 0.70f, 0.90f),
                    new Vector3(Mathf.Cos(a) * 0.2f, -0.65f, Mathf.Sin(a) * 0.2f));
            }
            Part("Bolt", root.transform, MeshFactory.Box(0.04f, 0.4f, 0.04f), new Color(1f, 0.92f, 0.35f), new Vector3(0, -0.6f, 0), Quaternion.Euler(0, 0, 15), Vector3.one);
            return root;
        }

        // ------------------------------------------------------------------ pieces

        public void Sync()
        {
            if (game == null) return;
            var b = game.Board;
            foreach (var v in b.Vertices)
            {
                Building existing;
                vertexPieceKind.TryGetValue(v.Id, out existing);
                if (v.Building == existing) continue;
                GameObject old;
                if (vertexPieces.TryGetValue(v.Id, out old)) Destroy(old);
                vertexPieces.Remove(v.Id);
                vertexPieceKind[v.Id] = v.Building;
                if (v.Building == Building.None) continue;
                var piece = v.Building == Building.Keep ? MakeKeep(v.Owner) : MakeOutpost(v.Owner);
                piece.transform.position = VertexPos(v.Id);
                piece.transform.rotation = Quaternion.Euler(0, 30 + 60 * (v.Id % 2), 0);
                piece.AddComponent<PopIn>();
                vertexPieces[v.Id] = piece;
            }
            foreach (var e in b.Edges)
            {
                if (e.Owner < 0 || edgePieces.ContainsKey(e.Id)) continue;
                Vector3 a = VertexPos(e.A), c = VertexPos(e.B);
                var go = Part("Path", pieceRoot, MeshFactory.Box(0.11f, 0.09f, 0.62f), Palette.PlayerColors[e.Owner], (a + c) / 2f, Quaternion.LookRotation(c - a), Vector3.one);
                go.AddComponent<PopIn>();
                edgePieces[e.Id] = go;
            }
            stormTarget = TilePos(game.StormTile) + Vector3.up * 1.3f;
        }

        GameObject MakeOutpost(int owner)
        {
            var root = new GameObject("Outpost");
            root.transform.SetParent(pieceRoot, false);
            Color c = Palette.PlayerColors[owner];
            Part("Walls", root.transform, MeshFactory.Box(0.22f, 0.16f, 0.18f), c, Vector3.zero);
            Part("Roof", root.transform, MeshFactory.Roof(0.26f, 0.24f, 0.13f), Color.Lerp(c, Color.black, 0.35f), new Vector3(0, 0.16f, 0));
            return root;
        }

        GameObject MakeKeep(int owner)
        {
            var root = new GameObject("Keep");
            root.transform.SetParent(pieceRoot, false);
            Color c = Palette.PlayerColors[owner];
            Color dark = Color.Lerp(c, Color.black, 0.35f);
            Part("Hall", root.transform, MeshFactory.Box(0.34f, 0.16f, 0.22f), c, Vector3.zero);
            Part("HallRoof", root.transform, MeshFactory.Roof(0.36f, 0.26f, 0.12f), dark, new Vector3(0, 0.16f, 0));
            Part("Tower", root.transform, MeshFactory.Prism(8, 0.09f, 0.38f, 22.5f), c, new Vector3(0.14f, 0, 0));
            Part("Spire", root.transform, MeshFactory.Pyramid(8, 0.11f, 0.18f, 22.5f), dark, new Vector3(0.14f, 0.38f, 0));
            return root;
        }

        // ------------------------------------------------------------------ markers & picking

        public void ShowMarkers(PickKind kind, List<int> ids, int hovered, Color color)
        {
            foreach (var m in markers) Destroy(m);
            markers.Clear();
            if (kind == PickKind.None || ids == null) return;
            foreach (int id in ids)
            {
                bool hot = id == hovered;
                Color c = hot ? Palette.HoverHighlight : color;
                GameObject m;
                if (kind == PickKind.Vertex)
                {
                    // lift the marker above an existing outpost when offering an upgrade
                    float lift = game.Board.Vertices[id].Building != Building.None ? 0.4f : 0f;
                    m = Part("VertexMarker", markerRoot, MeshFactory.Prism(12, hot ? 0.14f : 0.1f, 0.05f), c, VertexPos(id) + Vector3.up * lift);
                }
                else if (kind == PickKind.Edge)
                {
                    var ed = game.Board.Edges[id];
                    Vector3 a = VertexPos(ed.A), b = VertexPos(ed.B);
                    m = Part("EdgeMarker", markerRoot, MeshFactory.Box(hot ? 0.12f : 0.07f, 0.04f, 0.5f), c, (a + b) / 2f, Quaternion.LookRotation(b - a), Vector3.one);
                }
                else
                {
                    m = Part("TileMarker", markerRoot, MeshFactory.Prism(6, hot ? 0.5f : 0.4f, 0.03f, -30f), c, TilePos(id) + Vector3.up * 0.06f);
                }
                m.AddComponent<Pulse>();
                markers.Add(m);
            }
        }

        static bool RayToBoard(Ray ray, out Vector3 hit)
        {
            var plane = new Plane(Vector3.up, new Vector3(0, TileTop, 0));
            float d;
            if (plane.Raycast(ray, out d)) { hit = ray.GetPoint(d); return true; }
            hit = Vector3.zero;
            return false;
        }

        /// <summary>Finds the nearest candidate of the given kind under the mouse ray, or -1.</summary>
        public int Pick(Ray ray, PickKind kind, List<int> candidates)
        {
            Vector3 p;
            if (game == null || candidates == null || !RayToBoard(ray, out p)) return -1;
            float maxDist = kind == PickKind.Tile ? 0.9f : 0.35f;
            int best = -1; float bestD = maxDist * maxDist;
            foreach (int id in candidates)
            {
                Vector3 c = kind == PickKind.Vertex ? VertexPos(id) : kind == PickKind.Edge ? EdgePos(id) : TilePos(id);
                float d = (c - p).sqrMagnitude;
                if (d < bestD) { bestD = d; best = id; }
            }
            return best;
        }

        /// <summary>Tile under the cursor regardless of legality (for tooltips).</summary>
        public int TileUnder(Ray ray)
        {
            Vector3 p;
            if (game == null || !RayToBoard(ray, out p)) return -1;
            int best = -1; float bestD = 0.85f;
            foreach (var t in game.Board.Tiles)
            {
                float d = (TilePos(t.Id) - p).sqrMagnitude;
                if (d < bestD) { bestD = d; best = t.Id; }
            }
            return best;
        }

        void Update()
        {
            if (storm == null) return;
            Vector3 pos = Vector3.Lerp(storm.transform.position, stormTarget, 1f - Mathf.Exp(-4f * Time.deltaTime));
            pos.y = stormTarget.y + Mathf.Sin(Time.time * 2f) * 0.06f;
            storm.transform.position = pos;
            storm.transform.Rotate(0, 25f * Time.deltaTime, 0);
        }
    }

    /// <summary>Keeps world-space text facing the camera.</summary>
    public class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
        }
    }

    /// <summary>Scales a freshly placed piece in with a little bounce.</summary>
    public class PopIn : MonoBehaviour
    {
        float t;
        void Awake() { transform.localScale = Vector3.zero; }
        void Update()
        {
            t += Time.deltaTime * 3.5f;
            float s = t >= 1f ? 1f : 1f + Mathf.Sin(t * Mathf.PI) * 0.35f * (1f - t) - (1f - t) * (1f - t);
            transform.localScale = Vector3.one * Mathf.Max(0f, s);
            if (t >= 1f) { transform.localScale = Vector3.one; Destroy(this); }
        }
    }

    /// <summary>Gentle bob for placement markers.</summary>
    public class Pulse : MonoBehaviour
    {
        Vector3 basePos;
        void Start() { basePos = transform.localPosition; }
        void Update() { transform.localPosition = basePos + Vector3.up * (0.03f + Mathf.Sin(Time.time * 5f) * 0.03f); }
    }
}
