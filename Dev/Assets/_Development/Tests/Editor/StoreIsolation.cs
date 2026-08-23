using System.IO;

namespace CryNet.HierarchyColorStudio.Tests
{
    /// <summary>
    /// Snapshots and restores the project's real settings file so tests can work on a clean store
    /// without destroying the data of whoever runs them.
    /// </summary>
    internal sealed class StoreIsolation
    {
        private string m_Backup;
        private bool m_FileExisted;

        /// <summary>Backs up the current settings file and resets the store to its defaults.</summary>
        internal void Begin()
        {
            string path = HierarchyColorStoreFile.FullPath;
            m_FileExisted = File.Exists(path);
            m_Backup = m_FileExisted ? File.ReadAllText(path) : null;

            var store = HierarchyColorService.Store;
            store.ResetToDefaults();
            HierarchyColorService.InvalidateAll();
        }

        /// <summary>Restores the settings file that was present before the test ran.</summary>
        internal void End()
        {
            string path = HierarchyColorStoreFile.FullPath;
            if (m_FileExisted)
                File.WriteAllText(path, m_Backup);
            else if (File.Exists(path))
                File.Delete(path);

            HierarchyColorStoreProvider.Reload();
            HierarchyColorService.InvalidateAll();
        }
    }
}
