using System.Collections.Generic;
using UnityEngine;

namespace Hexhaven
{
    /// <summary>Builds low-poly, flat-shaded convex meshes at runtime (no imported art required).</summary>
    public static class MeshFactory
    {
        class Builder
        {
            readonly List<Vector3> verts = new List<Vector3>();
            readonly List<int> tris = new List<int>();
            readonly Vector3 centroid;

            public Builder(Vector3 centroid) { this.centroid = centroid; }

            /// <summary>Adds a convex polygon, flipping it if needed so its normal faces away from the centroid.</summary>
            public void Face(params Vector3[] poly)
            {
                Vector3 n = Vector3.Cross(poly[1] - poly[0], poly[2] - poly[0]);
                Vector3 c = Vector3.zero;
                foreach (var p in poly) c += p;
                c /= poly.Length;
                if (Vector3.Dot(n, c - centroid) < 0) System.Array.Reverse(poly);
                int start = verts.Count;
                verts.AddRange(poly);
                for (int i = 1; i < poly.Length - 1; i++)
                {
                    tris.Add(start); tris.Add(start + i); tris.Add(start + i + 1);
                }
            }

            public Mesh Build(string name)
            {
                var m = new Mesh { name = name };
                m.SetVertices(verts);
                m.SetTriangles(tris, 0);
                m.RecalculateNormals();
                m.RecalculateBounds();
                return m;
            }
        }

        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        static Vector3[] Ring(int sides, float radius, float y, float angleOffsetDeg)
        {
            var ring = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = Mathf.Deg2Rad * (angleOffsetDeg + 360f * i / sides);
                ring[i] = new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius);
            }
            return ring;
        }

        /// <summary>Regular n-gon prism standing on y = 0.</summary>
        public static Mesh Prism(int sides, float radius, float height, float angleOffsetDeg = 0f)
        {
            string key = "prism" + sides + "_" + radius + "_" + height + "_" + angleOffsetDeg;
            Mesh m;
            if (cache.TryGetValue(key, out m)) return m;
            var b = new Builder(new Vector3(0, height / 2, 0));
            var bottom = Ring(sides, radius, 0, angleOffsetDeg);
            var top = Ring(sides, radius, height, angleOffsetDeg);
            b.Face((Vector3[])top.Clone());
            b.Face((Vector3[])bottom.Clone());
            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;
                b.Face(bottom[i], bottom[j], top[j], top[i]);
            }
            m = b.Build(key);
            cache[key] = m;
            return m;
        }

        /// <summary>Regular n-gon pyramid (a low-poly cone) standing on y = 0.</summary>
        public static Mesh Pyramid(int sides, float radius, float height, float angleOffsetDeg = 0f)
        {
            string key = "pyr" + sides + "_" + radius + "_" + height + "_" + angleOffsetDeg;
            Mesh m;
            if (cache.TryGetValue(key, out m)) return m;
            var b = new Builder(new Vector3(0, height / 3, 0));
            var bottom = Ring(sides, radius, 0, angleOffsetDeg);
            var apex = new Vector3(0, height, 0);
            b.Face((Vector3[])bottom.Clone());
            for (int i = 0; i < sides; i++) b.Face(bottom[i], bottom[(i + 1) % sides], apex);
            m = b.Build(key);
            cache[key] = m;
            return m;
        }

        /// <summary>Axis-aligned box centred on x/z, standing on y = 0.</summary>
        public static Mesh Box(float sx, float sy, float sz)
        {
            string key = "box" + sx + "_" + sy + "_" + sz;
            Mesh m;
            if (cache.TryGetValue(key, out m)) return m;
            float x = sx / 2, z = sz / 2;
            var b = new Builder(new Vector3(0, sy / 2, 0));
            Vector3 p000 = new Vector3(-x, 0, -z), p100 = new Vector3(x, 0, -z), p110 = new Vector3(x, sy, -z), p010 = new Vector3(-x, sy, -z);
            Vector3 p001 = new Vector3(-x, 0, z), p101 = new Vector3(x, 0, z), p111 = new Vector3(x, sy, z), p011 = new Vector3(-x, sy, z);
            b.Face(p000, p100, p110, p010);
            b.Face(p001, p101, p111, p011);
            b.Face(p000, p001, p011, p010);
            b.Face(p100, p101, p111, p110);
            b.Face(p010, p110, p111, p011);
            b.Face(p000, p100, p101, p001);
            m = b.Build(key);
            cache[key] = m;
            return m;
        }

        /// <summary>Gabled roof: a triangular prism with its ridge along X, standing on y = 0.</summary>
        public static Mesh Roof(float width, float depth, float height)
        {
            string key = "roof" + width + "_" + depth + "_" + height;
            Mesh m;
            if (cache.TryGetValue(key, out m)) return m;
            float x = width / 2, z = depth / 2;
            var b = new Builder(new Vector3(0, height / 3, 0));
            Vector3 a0 = new Vector3(-x, 0, -z), a1 = new Vector3(x, 0, -z), a2 = new Vector3(x, 0, z), a3 = new Vector3(-x, 0, z);
            Vector3 r0 = new Vector3(-x, height, 0), r1 = new Vector3(x, height, 0);
            b.Face(a0, a1, a2, a3);
            b.Face(a0, a1, r1, r0);
            b.Face(a3, a2, r1, r0);
            b.Face(a0, a3, r0);
            b.Face(a1, a2, r1);
            m = b.Build(key);
            cache[key] = m;
            return m;
        }
    }
}
