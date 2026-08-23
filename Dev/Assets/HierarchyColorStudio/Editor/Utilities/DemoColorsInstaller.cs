using System.IO;
using UnityEditor;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Applies the color set that ships with the demo scene.
    /// </summary>
    /// <remarks>
    /// Color assignments live in the project's settings folder rather than inside a scene, so they cannot
    /// travel inside a <c>.unitypackage</c>. The demo therefore ships its colors as an exported color set
    /// that the user applies with one menu command. The stored identifiers stay valid because the demo
    /// scene and its meta file are imported unchanged.
    /// </remarks>
    internal static class DemoColorsInstaller
    {
        private const string DemoColorSetName = "HierarchyColorStudioDemoColors";
        private const string SearchFilter = DemoColorSetName + " t:TextAsset";

        [MenuItem(UiStrings.MenuRootTools + UiStrings.MenuApplySampleColors, false, 200)]
        private static void ApplyDemoColors()
        {
            string assetPath = FindColorSetPath();
            if (string.IsNullOrEmpty(assetPath))
            {
                StudioLog.WarnOnce("demo-colors-missing",
                    "The demo color set could not be found. It ships in the Samples folder of the " +
                    UiStrings.ProductName + " package.");
                return;
            }

            string absolutePath = Path.GetFullPath(assetPath);
            int applied = ColorTransfer.Import(absolutePath, replace: false);
            if (applied > 0)
                HierarchyColorStudioWindow.Open();
        }

        [MenuItem(UiStrings.MenuRootTools + UiStrings.MenuApplySampleColors, true)]
        private static bool ApplyDemoColorsValidate()
        {
            return !string.IsNullOrEmpty(FindColorSetPath());
        }

        private static string FindColorSetPath()
        {
            var guids = AssetDatabase.FindAssets(SearchFilter);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!string.IsNullOrEmpty(path) && path.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase))
                    return path;
            }

            return string.Empty;
        }
    }
}
