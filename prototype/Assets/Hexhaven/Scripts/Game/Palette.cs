using System.Collections.Generic;
using Hexhaven.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hexhaven
{
    /// <summary>Colours and a material cache that works in both the Built-in pipeline and URP.</summary>
    public static class Palette
    {
        public static readonly Color[] PlayerColors =
        {
            new Color(0.86f, 0.22f, 0.20f),  // Crimson
            new Color(0.20f, 0.46f, 0.90f),  // Azure
            new Color(0.96f, 0.68f, 0.12f),  // Amber
            new Color(0.58f, 0.32f, 0.82f),  // Violet
        };
        public static readonly string[] PlayerColorNames = { "Crimson", "Azure", "Amber", "Violet" };

        public static readonly Color[] ResourceColors =
        {
            new Color(0.22f, 0.50f, 0.27f),  // Timber
            new Color(0.80f, 0.43f, 0.26f),  // Clay
            new Color(0.45f, 0.62f, 0.95f),  // Fiber
            new Color(0.95f, 0.80f, 0.32f),  // Grain
            new Color(0.58f, 0.60f, 0.66f),  // Iron
        };

        public static Color TerrainColor(Terrain t)
        {
            switch (t)
            {
                case Terrain.Forest: return new Color(0.26f, 0.52f, 0.30f);
                case Terrain.ClayPit: return new Color(0.80f, 0.47f, 0.30f);
                case Terrain.FlaxField: return new Color(0.60f, 0.76f, 0.50f);
                case Terrain.Farmland: return new Color(0.93f, 0.80f, 0.42f);
                case Terrain.Mine: return new Color(0.52f, 0.53f, 0.58f);
                default: return new Color(0.86f, 0.79f, 0.62f);
            }
        }

        public static string TerrainName(Terrain t)
        {
            switch (t)
            {
                case Terrain.ClayPit: return "Clay Pit";
                case Terrain.FlaxField: return "Flax Field";
                default: return t.ToString();
            }
        }

        public static readonly Color Ocean = new Color(0.16f, 0.45f, 0.66f);
        public static readonly Color Beach = new Color(0.92f, 0.85f, 0.66f);
        public static readonly Color Sky = new Color(0.62f, 0.80f, 0.92f);
        public static readonly Color Wood = new Color(0.50f, 0.34f, 0.20f);
        public static readonly Color Token = new Color(0.98f, 0.95f, 0.86f);
        public static readonly Color Storm = new Color(0.25f, 0.26f, 0.32f);
        public static readonly Color Highlight = new Color(1f, 1f, 1f);
        public static readonly Color HoverHighlight = new Color(1f, 0.92f, 0.25f);

        static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        static Shader litShader;

        static Shader LitShader()
        {
            if (litShader != null) return litShader;
            if (GraphicsSettings.currentRenderPipeline != null)
                litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Simple Lit");
            if (litShader == null) litShader = Shader.Find("Standard");
            if (litShader == null) litShader = Shader.Find("Diffuse");
            return litShader;
        }

        public static Material Mat(Color c)
        {
            Material m;
            if (materials.TryGetValue(c, out m) && m != null) return m;
            m = new Material(LitShader()) { name = "Hexhaven_" + ColorUtility.ToHtmlStringRGB(c) };
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.15f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.15f);
            materials[c] = m;
            return m;
        }

        public static Font UIFont()
        {
#if UNITY_2022_2_OR_NEWER
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        }
    }
}
