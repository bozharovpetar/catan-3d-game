using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hexhaven.EditorTools
{
    /// <summary>Menu items to create the game scene through the Editor API (no hand-written scene YAML).</summary>
    public static class HexhavenMenu
    {
        const string ScenePath = "Assets/Hexhaven/Scenes/Hexhaven.unity";

        [MenuItem("Hexhaven/Create Game Scene", priority = 0)]
        public static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            new GameObject("Hexhaven").AddComponent<HexhavenGame>();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            AssetDatabase.Refresh();
            Debug.Log("Hexhaven: created " + ScenePath + " and added it to Build Settings. Press Play!");
        }

        [MenuItem("Hexhaven/Play", priority = 1)]
        public static void Play()
        {
            if (!File.Exists(ScenePath)) CreateScene();
            else if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.isPlaying = true;
        }
    }
}
