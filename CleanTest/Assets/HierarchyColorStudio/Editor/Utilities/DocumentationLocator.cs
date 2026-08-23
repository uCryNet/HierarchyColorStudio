using System.IO;
using UnityEditor;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Locates the bundled documentation and reveals it in the Project window.
    /// </summary>
    /// <remarks>
    /// The package folder can be moved or renamed by the user, so the location is derived from the
    /// plugin's assembly definition asset instead of a hard-coded path. The documentation is revealed
    /// inside Unity rather than handed to an external application, which keeps the plugin free of any
    /// process or shell interaction.
    /// </remarks>
    internal static class DocumentationLocator
    {
        private const string AssemblyName = "CryNet.HierarchyColorStudio.Editor";
        private const string AssemblySearchFilter = AssemblyName + " t:AssemblyDefinitionAsset";
        private const string DocumentationFolderName = "Documentation";
        private const string ReadmeFileName = "README.md";
        private const string EditorFolderName = "Editor";

        /// <summary>Selects and highlights the bundled documentation in the Project window.</summary>
        internal static void Open()
        {
            string path = FindDocumentationPath();
            if (string.IsNullOrEmpty(path))
            {
                StudioLog.WarnOnce("docs-missing",
                    "The bundled documentation could not be located. Look for the " + DocumentationFolderName +
                    " folder inside the " + UiStrings.ProductName + " package.");
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (asset == null)
                return;

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        /// <summary>Returns the asset path of the bundled README, or of its folder, or an empty string.</summary>
        internal static string FindDocumentationPath()
        {
            string root = FindPackageRoot();
            if (string.IsNullOrEmpty(root))
                return string.Empty;

            string documentation = root + "/" + DocumentationFolderName;
            string readme = documentation + "/" + ReadmeFileName;

            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(readme)))
                return readme;

            return !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(documentation)) ? documentation : string.Empty;
        }

        /// <summary>Returns the asset path of the package's top-level folder, or an empty string.</summary>
        internal static string FindPackageRoot()
        {
            var guids = AssetDatabase.FindAssets(AssemblySearchFilter);
            for (int i = 0; i < guids.Length; i++)
            {
                string assemblyPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(assemblyPath))
                    continue;

                string editorFolder = Path.GetDirectoryName(assemblyPath)?.Replace('\\', '/');
                if (string.IsNullOrEmpty(editorFolder))
                    continue;

                string parent = Path.GetDirectoryName(editorFolder)?.Replace('\\', '/');
                bool insideEditorFolder = editorFolder.EndsWith("/" + EditorFolderName, System.StringComparison.Ordinal);
                return insideEditorFolder && !string.IsNullOrEmpty(parent) ? parent : editorFolder;
            }

            return string.Empty;
        }
    }
}
