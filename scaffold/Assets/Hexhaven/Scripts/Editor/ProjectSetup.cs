// SCAFFOLD — complete. Spec: docs/02-ARCHITECTURE.md §2.2. Idempotent; safe to run repeatedly.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
#if HEXHAVEN_URP
using UnityEngine.Rendering.Universal;
#endif

namespace Hexhaven.EditorTools
{
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/Hexhaven/Scenes/Main.unity";

        [MenuItem("Hexhaven/Setup Project", priority = 0)]
        public static void Run()
        {
            PlayerSettings.companyName = "Hexhaven Studio";
            PlayerSettings.productName = "Hexhaven";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;

            string[] names = QualitySettings.names;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.vSyncCount = 1;
            }
            QualitySettings.SetQualityLevel(current, false);

#if HEXHAVEN_URP
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null)
            {
                urp.shadowDistance = 30f;
                urp.msaaSampleCount = 4;
                urp.supportsHDR = true;
                EditorUtility.SetDirty(urp);
            }
#endif
            AddAlwaysIncludedShaders(new[] { "Hexhaven/WorldText" });   // never add URP/Lit here (variant explosion)
            CreateTemplateMaterials();

            CreateMainScene();
            AssetDatabase.SaveAssets();
            Debug.Log("Hexhaven: project setup complete.");
        }

        static void AddAlwaysIncludedShaders(string[] shaderNames)
        {
            var graphicsSettings = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
            if (graphicsSettings == null) return;
            var so = new SerializedObject(graphicsSettings);
            var arr = so.FindProperty("m_AlwaysIncludedShaders");
            if (arr == null) return;
            foreach (var name in shaderNames)
            {
                var shader = Shader.Find(name);
                if (shader == null) continue;
                bool present = false;
                for (int i = 0; i < arr.arraySize; i++)
                    if (arr.GetArrayElementAtIndex(i).objectReferenceValue == shader) { present = true; break; }
                if (present) continue;
                arr.InsertArrayElementAtIndex(arr.arraySize);
                arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = shader;
            }
            so.ApplyModifiedProperties();
        }

        public const string MaterialsDir = "Assets/Hexhaven/Resources/Hexhaven/Materials";

        /// <summary>Template materials that pull the needed shader variants into builds (docs/05-VISUALS.md §1).</summary>
        static void CreateTemplateMaterials()
        {
            Directory.CreateDirectory(MaterialsDir);
            bool urp = GraphicsSettings.currentRenderPipeline != null;
            string lit = urp ? "Universal Render Pipeline/Lit" : "Standard";
            string unlit = urp ? "Universal Render Pipeline/Unlit" : "Unlit/Color";
            string particle = urp ? "Universal Render Pipeline/Particles/Unlit" : "Particles/Standard Unlit";

            var opaque = MakeMaterial("LitOpaque", lit);
            if (opaque != null)
            {
                SetFloat(opaque, "_Smoothness", 0.15f); SetFloat(opaque, "_Glossiness", 0.15f); SetFloat(opaque, "_Metallic", 0f);
                opaque.enableInstancing = true;
            }

            var transparent = MakeMaterial("LitTransparent", lit);
            if (transparent != null)
            {
                SetFloat(transparent, "_Surface", 1f); SetFloat(transparent, "_Blend", 0f);
                SetFloat(transparent, "_SrcBlend", 5f); SetFloat(transparent, "_DstBlend", 10f); SetFloat(transparent, "_ZWrite", 0f);
                SetFloat(transparent, "_Mode", 3f);                                  // Built-in Standard: Transparent
                transparent.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                transparent.EnableKeyword("_ALPHAPREMULTIPLY_ON");                  // Built-in Standard transparent path
                transparent.SetOverrideTag("RenderType", "Transparent");
                transparent.renderQueue = 3000;
                transparent.enableInstancing = true;
            }

            var emissive = MakeMaterial("LitEmissive", lit);
            if (emissive != null)
            {
                emissive.EnableKeyword("_EMISSION");
                emissive.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                if (emissive.HasProperty("_EmissionColor")) emissive.SetColor("_EmissionColor", Color.white);
                emissive.enableInstancing = true;
            }

            var u = MakeMaterial("Unlit", unlit);
            if (u != null) u.enableInstancing = true;

            var p = MakeMaterial("ParticleUnlit", particle);
            if (p != null)
            {
                SetFloat(p, "_Surface", 1f); SetFloat(p, "_Blend", 0f);
                p.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                p.renderQueue = 3000;
            }
            AssetDatabase.SaveAssets();
        }

        static Material MakeMaterial(string name, string shaderName)
        {
            string path = MaterialsDir + "/" + name + ".mat";
            var shader = Shader.Find(shaderName);
            if (shader == null) { Debug.LogWarning("Hexhaven: shader not found: " + shaderName); return null; }
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            else mat.shader = shader;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void SetFloat(Material m, string prop, float v) { if (m.HasProperty(prop)) m.SetFloat(prop, v); }

        static void CreateMainScene()
        {
            if (!File.Exists(ScenePath))
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("HexhavenApp").AddComponent<HexhavenApp>();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        [MenuItem("Hexhaven/Play Main Scene", priority = 1)]
        public static void Play()
        {
            if (!File.Exists(ScenePath)) Run();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(ScenePath);
                EditorApplication.isPlaying = true;
            }
        }

        [MenuItem("Hexhaven/Open Test Runner", priority = 20)]
        public static void OpenTestRunner() { EditorApplication.ExecuteMenuItem("Window/General/Test Runner"); }
    }
}
