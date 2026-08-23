using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CryNet.HierarchyColorStudio.Authoring
{
    /// <summary>
    /// Development-only authoring tool that regenerates the bundled demo scene and its color set.
    /// This file is not part of the distributed package.
    /// </summary>
    internal static class DemoSceneAuthoring
    {
        private const string SampleFolder = "Assets/HierarchyColorStudio/Samples/Demo";
        private const string ScenePath = SampleFolder + "/HierarchyColorStudioDemo.unity";
        private const string ColorSetPath = SampleFolder + "/HierarchyColorStudioDemoColors.json";

        private static readonly (string Path, string Preset)[] ColorPlan =
        {
            ("Environment", "Green"),
            ("Environment/Terrain", "Green"),
            ("Environment/Props", "Green"),
            ("Characters", "Blue"),
            ("Characters/Player", "Teal"),
            ("Characters/Enemies", "Red"),
            ("Characters/NPCs", "Amber"),
            ("Gameplay", "Orange"),
            ("Systems", "Violet"),
            ("UI", "Pink"),
            ("UI/HUD", "Pink")
        };

        [MenuItem("Tools/Hierarchy Color Studio Development/Rebuild Demo Scene", false, 1000)]
        internal static void RebuildDemoScene()
        {
            Directory.CreateDirectory(SampleFolder);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildHierarchy();

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError("Could not save the demo scene to " + ScenePath);
                return;
            }

            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceSynchronousImport);

            ApplyColors();
            HierarchyColorService.SaveNow();

            if (!HierarchyColorService.ExportColors(Path.GetFullPath(ColorSetPath)))
            {
                Debug.LogError("Could not export the demo color set.");
                return;
            }

            AssetDatabase.ImportAsset(ColorSetPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.SaveAssets();
            Debug.Log("Demo scene and color set rebuilt.");
        }

        private static void BuildHierarchy()
        {
            var camera = new GameObject("Main Camera", typeof(Camera));
            camera.transform.SetPositionAndRotation(new Vector3(0f, 3f, -10f), Quaternion.Euler(10f, 0f, 0f));
            camera.tag = "MainCamera";

            var light = new GameObject("Directional Light", typeof(Light));
            light.GetComponent<Light>().type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            CreateBranch("Environment", "Terrain", "Props", "Buildings", "Water");
            CreateBranch("Environment/Terrain", "Ground", "Cliffs");
            CreateBranch("Environment/Props", "Trees", "Rocks", "Fences");
            CreateBranch("Characters", "Player", "Enemies", "NPCs");
            CreateBranch("Characters/Enemies", "Enemy_Scout", "Enemy_Brute");
            CreateBranch("Characters/NPCs", "NPC_Merchant", "NPC_Guard");
            CreateBranch("Gameplay", "Checkpoints", "Spawners", "Triggers");
            CreateBranch("Systems", "GameManager", "AudioManager", "SaveSystem");
            CreateBranch("UI", "HUD", "Menus", "Overlays");
            CreateBranch("UI/HUD", "HealthBar", "Minimap");
        }

        private static void CreateBranch(string parentPath, params string[] children)
        {
            var parent = EnsureObject(parentPath);
            for (int i = 0; i < children.Length; i++)
                EnsureObject(parentPath + "/" + children[i]);

            if (parent == null)
                Debug.LogError("Could not create " + parentPath);
        }

        private static GameObject EnsureObject(string path)
        {
            var existing = GameObject.Find(path);
            if (existing != null)
                return existing;

            int separator = path.LastIndexOf('/');
            string name = separator >= 0 ? path.Substring(separator + 1) : path;
            var created = new GameObject(name);

            if (separator >= 0)
            {
                var parent = EnsureObject(path.Substring(0, separator));
                created.transform.SetParent(parent.transform, false);
            }

            return created;
        }

        private static void ApplyColors()
        {
            for (int i = 0; i < ColorPlan.Length; i++)
            {
                var target = GameObject.Find(ColorPlan[i].Path);
                if (target == null)
                {
                    Debug.LogError("Demo object not found: " + ColorPlan[i].Path);
                    continue;
                }

                if (!HierarchyColorService.TryGetPreset(ColorPlan[i].Preset, out var preset))
                {
                    Debug.LogError("Demo preset not found: " + ColorPlan[i].Preset);
                    continue;
                }

                HierarchyColorService.Assign(new[] { target }, preset.Color, preset.Id);
            }
        }
    }
}
