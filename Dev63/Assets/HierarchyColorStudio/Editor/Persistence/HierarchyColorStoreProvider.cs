using System;
using UnityEditor;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Owns the single in-memory <see cref="HierarchyColorStore"/> instance, tracks unsaved changes and
    /// writes them to disk on a short debounce so bulk edits produce a single file write.
    /// </summary>
    internal static class HierarchyColorStoreProvider
    {
        private const double SaveDebounceSeconds = 0.35;
        private const string SessionMarkerKey = "CryNet.HierarchyColorStudio.SessionMarker";
        private const string SessionMarkerValue = "1";

        private static HierarchyColorStore s_Store;
        private static bool s_Dirty;
        private static double s_LastChangeTime;

        /// <summary>Raised whenever the store content changes, including after undo and reload.</summary>
        internal static event Action Changed;

        /// <summary>The live store instance. Loaded on first access and after every domain reload.</summary>
        internal static HierarchyColorStore Store
        {
            get
            {
                if (s_Store == null)
                    Load();
                return s_Store;
            }
        }

        /// <summary><c>true</c> when there are changes that have not been written to disk yet.</summary>
        internal static bool HasUnsavedChanges => s_Dirty;

        /// <summary>Marks the store as changed, schedules a debounced save and notifies listeners.</summary>
        internal static void MarkChanged()
        {
            s_Dirty = true;
            s_LastChangeTime = EditorApplication.timeSinceStartup;
            Store.InvalidateLookup();
            Changed?.Invoke();
        }

        /// <summary>Writes pending changes to disk immediately.</summary>
        internal static void Flush()
        {
            if (!s_Dirty || s_Store == null)
                return;

            if (HierarchyColorStoreFile.Save(s_Store))
                s_Dirty = false;
        }

        /// <summary>Called from the Editor update loop to perform the debounced save.</summary>
        internal static void Tick()
        {
            if (!s_Dirty)
                return;

            if (EditorApplication.timeSinceStartup - s_LastChangeTime >= SaveDebounceSeconds)
                Flush();
        }

        /// <summary>Discards the in-memory instance and reloads the store from disk.</summary>
        internal static void Reload()
        {
            s_Store = null;
            s_Dirty = false;
            Load();
            Changed?.Invoke();
        }

        /// <summary>Resets every setting, preset and assignment to the factory defaults and saves.</summary>
        internal static void ResetEverything()
        {
            var store = Store;
            UndoScope.Record(store, UiStrings.UndoResetEverything);
            store.ResetToDefaults();
            MarkChanged();
            Flush();
        }

        private static void Load()
        {
            s_Store = HierarchyColorStoreFile.LoadOrCreate();

            // Session-scoped keys reference instance ids, which are only meaningful until the Editor
            // is restarted. They must survive a domain reload but not a new session.
            if (SessionState.GetString(SessionMarkerKey, string.Empty) != SessionMarkerValue)
            {
                SessionState.SetString(SessionMarkerKey, SessionMarkerValue);
                int removed = s_Store.RemoveSessionScopedAssignments();
                if (removed > 0)
                {
                    s_Dirty = true;
                    s_LastChangeTime = EditorApplication.timeSinceStartup;
                    StudioLog.Info(removed + " session-scoped color assignment(s) from a previous session were discarded.");
                }
            }
        }
    }
}
