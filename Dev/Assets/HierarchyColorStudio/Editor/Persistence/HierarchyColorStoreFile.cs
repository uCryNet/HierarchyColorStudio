using System;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Reads and writes the <see cref="HierarchyColorStore"/> as a text-serialized file inside the
    /// project's <c>ProjectSettings</c> folder.
    /// </summary>
    /// <remarks>
    /// The file lives outside <c>Assets</c> on purpose: it never enters the AssetDatabase, so it costs
    /// no import time, owns no GUID that could change, and is diff- and merge-friendly in source control.
    /// </remarks>
    internal static class HierarchyColorStoreFile
    {
        /// <summary>File name of the store inside the <c>ProjectSettings</c> folder.</summary>
        internal const string FileName = "HierarchyColorStudio.asset";

        private const string BackupSuffix = ".corrupt";
        private const string SettingsFolderName = "ProjectSettings";

        /// <summary>Absolute path of the store file for the current project.</summary>
        internal static string FullPath
        {
            get
            {
                string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
                return Path.Combine(Path.Combine(projectRoot, SettingsFolderName), FileName);
            }
        }

        /// <summary>Project-relative path of the store file, for display and documentation.</summary>
        internal static string RelativePath => SettingsFolderName + "/" + FileName;

        /// <summary>
        /// Loads the store, or creates a defaulted instance when the file is missing or unreadable.
        /// A corrupted file is preserved next to the original with a <c>.corrupt</c> suffix.
        /// </summary>
        internal static HierarchyColorStore LoadOrCreate()
        {
            return LoadOrCreate(FullPath);
        }

        /// <summary>
        /// Loads the store from an explicit path, or creates a defaulted instance when the file is
        /// missing or unreadable.
        /// </summary>
        /// <param name="path">Absolute path of the file to read.</param>
        internal static HierarchyColorStore LoadOrCreate(string path)
        {
            HierarchyColorStore store = null;

            if (File.Exists(path))
            {
                try
                {
                    var loaded = InternalEditorUtility.LoadSerializedFileAndForget(path);
                    if (loaded != null)
                    {
                        for (int i = 0; i < loaded.Length; i++)
                        {
                            store = loaded[i] as HierarchyColorStore;
                            if (store != null)
                                break;
                        }
                    }
                }
                catch (Exception exception)
                {
                    store = null;
                    StudioLog.ExceptionOnce("store-load", exception,
                        "Could not read " + RelativePath + ". A defaulted configuration will be used.");
                }

                if (store == null)
                    TryBackupUnreadableFile(path);
            }

            if (store == null)
            {
                store = ScriptableObject.CreateInstance<HierarchyColorStore>();
                store.ResetToDefaults();
            }

            store.name = "Hierarchy Color Studio";
            store.hideFlags = HideFlags.None;

            int repairs = store.Sanitize();
            if (repairs > 0)
                StudioLog.WarnOnce("store-repair", repairs + " invalid record(s) in " + RelativePath + " were repaired or dropped.");

            return store;
        }

        /// <summary>Writes the store to disk as text. Failures are reported once and never rethrown.</summary>
        /// <param name="store">Store instance to persist.</param>
        /// <returns><c>true</c> when the file was written.</returns>
        internal static bool Save(HierarchyColorStore store)
        {
            return Save(store, FullPath);
        }

        /// <summary>Writes the store to an explicit path as text.</summary>
        /// <param name="store">Store instance to persist.</param>
        /// <param name="path">Absolute destination path.</param>
        /// <returns><c>true</c> when the file was written.</returns>
        internal static bool Save(HierarchyColorStore store, string path)
        {
            if (store == null)
                return false;

            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                InternalEditorUtility.SaveToSerializedFileAndForget(new UnityEngine.Object[] { store }, path, true);
                StudioLog.Info("Saved " + path);
                return true;
            }
            catch (Exception exception)
            {
                StudioLog.ExceptionOnce("store-save", exception, "Could not write " + path + ".");
                return false;
            }
        }

        /// <summary>Deletes the store file, if present.</summary>
        internal static void Delete()
        {
            try
            {
                string path = FullPath;
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception exception)
            {
                StudioLog.ExceptionOnce("store-delete", exception, "Could not delete " + RelativePath + ".");
            }
        }

        private static void TryBackupUnreadableFile(string path)
        {
            try
            {
                string backupPath = path + BackupSuffix;
                if (File.Exists(backupPath))
                    File.Delete(backupPath);
                File.Move(path, backupPath);
                StudioLog.WarnOnce("store-backup",
                    RelativePath + " could not be read and was moved to " + RelativePath + BackupSuffix + ".");
            }
            catch (Exception exception)
            {
                StudioLog.ExceptionOnce("store-backup-failed", exception,
                    "Could not move the unreadable file " + RelativePath + " aside.");
            }
        }
    }
}
