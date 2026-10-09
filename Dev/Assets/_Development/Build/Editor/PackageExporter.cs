using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CryNet.HierarchyColorStudio.Build
{
    /// <summary>
    /// Builds the distributable <c>.unitypackage</c> from this project.
    /// </summary>
    /// <remarks>
    /// Exporting by hand through <b>Assets → Export Package…</b> works, but it is the one step of the
    /// release process where a mistake is silent: the dialog's tree is easy to mis-tick, and a package
    /// that carries <c>_Development</c> — the tests and this exporter — is rejected by the Asset Store
    /// only after submission. Doing it in code makes the file list a reviewable constant and lets the
    /// step run headlessly next to the compile and test runs.
    ///
    /// <see cref="AssetDatabase.ExportPackage"/> reads the AssetDatabase, not the file system, so any
    /// file Unity has not imported yet is skipped without a warning. The refresh below is therefore
    /// mandatory rather than defensive: documentation added outside the Editor has no meta file until it
    /// happens, and would be missing from the package.
    /// </remarks>
    internal static class PackageExporter
    {
        private const string PackageFolder = "Assets/HierarchyColorStudio";
        private const string DevelopmentFolder = "Assets/_Development";
        private const string OutputDirectoryName = "Dist";
        private const string OutputFileName = "HierarchyColorStudio.unitypackage";

        private const string MenuPath = "Tools/Hierarchy Color Studio/Development/Export .unitypackage";

        [MenuItem(MenuPath, false, 500)]
        private static void ExportFromMenu()
        {
            if (TryExport(out string message))
                EditorUtility.DisplayDialog("Export Package", message, "OK");
            else
                EditorUtility.DisplayDialog("Export Package", "Export failed.\n\n" + message, "OK");
        }

        /// <summary>Entry point for <c>-executeMethod</c>. Sets the process exit code.</summary>
        internal static void ExportFromCommandLine()
        {
            bool succeeded = TryExport(out string message);
            Debug.Log((succeeded ? "[PackageExporter] " : "[PackageExporter] FAILED: ") + message);
            EditorApplication.Exit(succeeded ? 0 : 1);
        }

        private static bool TryExport(out string message)
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            if (!AssetDatabase.IsValidFolder(PackageFolder))
            {
                message = PackageFolder + " does not exist.";
                return false;
            }

            // A development file that had drifted into the shipped folder would be exported with it, and
            // Recurse gives no way to exclude a subtree. Fail instead of shipping it.
            if (Directory.Exists(Path.Combine(PackageFolder, "_Development")))
            {
                message = "A _Development folder exists inside " + PackageFolder + ". Move it out first.";
                return false;
            }

            string outputPath = ResolveOutputPath();
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            try
            {
                // Recurse alone. IncludeDependencies would pull in whatever the AssetDatabase considers
                // referenced, which for an Editor-only tool is nothing useful and everything risky.
                AssetDatabase.ExportPackage(PackageFolder, outputPath, ExportPackageOptions.Recurse);
            }
            catch (Exception exception)
            {
                message = exception.Message;
                return false;
            }

            var info = new FileInfo(outputPath);
            if (!info.Exists || info.Length == 0L)
            {
                message = "Unity reported no error but produced no file at " + outputPath + ".";
                return false;
            }

            message = "Exported " + CountExportedAssets() + " assets to " + outputPath +
                      " (" + (info.Length / 1024L) + " KB). " + DevelopmentFolder + " was not included.";
            return true;
        }

        private static string ResolveOutputPath()
        {
            // Application.dataPath is <project>/Assets; the distribution folder is a sibling of the
            // project so that build output never lands inside the AssetDatabase.
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            string containerRoot = Directory.GetParent(projectRoot)?.FullName ?? projectRoot;
            return Path.Combine(containerRoot, OutputDirectoryName, OutputFileName);
        }

        private static int CountExportedAssets()
        {
            return AssetDatabase.FindAssets(string.Empty, new[] { PackageFolder }).Length;
        }
    }
}
