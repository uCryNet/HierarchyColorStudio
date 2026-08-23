using System.Collections.Generic;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Caches the current selection as a hash set.
    /// Unity's selection properties allocate a new array on every access, which would be a per-row,
    /// per-repaint allocation if they were queried from the Hierarchy GUI callback.
    /// </summary>
    internal static class HierarchySelectionCache
    {
        private static readonly HashSet<RowId> s_Selected = new HashSet<RowId>();
        private static bool s_Valid;

        /// <summary>Marks the cache as out of date. Called when Unity's selection changes.</summary>
        internal static void Invalidate()
        {
            s_Valid = false;
        }

        /// <summary><c>true</c> when the identifier is part of the current selection.</summary>
        /// <param name="rowId">Identifier to test.</param>
        internal static bool Contains(RowId rowId)
        {
            if (!s_Valid)
                Rebuild();
            return s_Selected.Contains(rowId);
        }

        private static void Rebuild()
        {
            s_Selected.Clear();
            var ids = RowId.GetSelection();
            for (int i = 0; i < ids.Length; i++)
                s_Selected.Add(ids[i]);
            s_Valid = true;
        }
    }
}
