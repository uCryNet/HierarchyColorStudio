using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Public entry point of Hierarchy Color Studio. Assigns, reads and clears Hierarchy colors,
    /// with full support for Unity's Undo system and multi-selection.
    /// </summary>
    public static class HierarchyColorService
    {
        /// <summary>Number of GameObjects above which bulk operations ask for confirmation.</summary>
        public const int LargeOperationThreshold = 250;

        private static readonly AssignmentIndex s_Index = new AssignmentIndex();
        private static readonly List<GameObject> s_TargetBuffer = new List<GameObject>(64);
        private static readonly List<GameObject> s_SingleBuffer = new List<GameObject>(1);
        private static readonly List<GameObject> s_AcceptedBuffer = new List<GameObject>(64);
        private static readonly List<string> s_KeyBuffer = new List<string>(64);
        private static readonly HashSet<RowId> s_DedupeBuffer = new HashSet<RowId>();
        private static readonly List<int> s_StaleBuffer = new List<int>(32);

        /// <summary>Raised whenever assignments, presets or appearance settings change.</summary>
        public static event Action Changed;

        /// <summary>Number of colors stored in the project, including entries in scenes that are not open.</summary>
        public static int StoredAssignmentCount => Store.Assignments.Count;

        /// <summary>Number of colors that currently resolve to a GameObject in the open scenes.</summary>
        public static int ResolvedAssignmentCount
        {
            get
            {
                s_Index.RebuildIfNeeded(Store);
                return s_Index.ResolvedCount;
            }
        }

        /// <summary>
        /// <c>true</c> when at least one visible color belongs to a GameObject that has not been saved to
        /// a scene yet, so its color is only remembered for the current Editor session.
        /// </summary>
        public static bool HasSessionScopedAssignments
        {
            get
            {
                s_Index.RebuildIfNeeded(Store);
                return s_Index.HasSessionScopedAssignments;
            }
        }

        /// <summary>The color presets configured for this project, in display order.</summary>
        public static IReadOnlyList<ColorPreset> Presets => Store.Presets;

        /// <summary>Finds a preset by its display name, ignoring case.</summary>
        /// <param name="name">Preset name to look for.</param>
        /// <param name="preset">Receives the matching preset.</param>
        /// <returns><c>true</c> when a preset with that name exists.</returns>
        public static bool TryGetPreset(string name, out ColorPreset preset)
        {
            preset = null;
            if (string.IsNullOrEmpty(name))
                return false;

            var presets = Store.Presets;
            for (int i = 0; i < presets.Count; i++)
            {
                if (presets[i] != null &&
                    string.Equals(presets[i].Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    preset = presets[i];
                    return true;
                }
            }

            return false;
        }

        /// <summary>Writes the project's colors and presets to a JSON file.</summary>
        /// <param name="absolutePath">Absolute destination path.</param>
        /// <returns><c>true</c> when the file was written.</returns>
        public static bool ExportColors(string absolutePath)
        {
            return ColorTransfer.Export(absolutePath);
        }

        /// <summary>Reads colors and presets from a JSON file produced by <see cref="ExportColors"/>.</summary>
        /// <param name="absolutePath">Absolute source path.</param>
        /// <param name="replace">When <c>true</c> existing colors and presets are discarded first.</param>
        /// <returns>The number of imported colors, or <c>-1</c> when the file could not be read.</returns>
        public static int ImportColors(string absolutePath, bool replace)
        {
            return ColorTransfer.Import(absolutePath, replace);
        }

        /// <summary>Writes pending changes to the project's settings file immediately.</summary>
        public static void SaveNow()
        {
            HierarchyColorStoreProvider.Flush();
        }

        internal static HierarchyColorStore Store => HierarchyColorStoreProvider.Store;

        internal static AssignmentIndex Index => s_Index;

        /// <summary>Reads the color assigned to a GameObject.</summary>
        /// <param name="target">GameObject to query.</param>
        /// <param name="color">Receives the assigned color.</param>
        /// <returns><c>true</c> when the GameObject has an assigned color.</returns>
        public static bool TryGetColor(GameObject target, out Color color)
        {
            color = UnityEngine.Color.white;
            if (target == null)
                return false;

            s_Index.RebuildIfNeeded(Store);
            if (!s_Index.TryGetColor(RowId.Of(target), out Color32 stored))
                return false;

            color = stored;
            return true;
        }

        /// <summary>Reads the color of a Hierarchy row. Used by the Hierarchy renderer.</summary>
        /// <param name="rowId">Identifier of the row's object.</param>
        /// <param name="color">Receives the assigned color.</param>
        /// <returns><c>true</c> when the row has an assigned color.</returns>
        internal static bool TryGetRowColor(RowId rowId, out Color32 color)
        {
            return s_Index.TryGetColor(rowId, out color);
        }

        /// <summary>Assigns a color to a single GameObject.</summary>
        /// <param name="target">GameObject to color.</param>
        /// <param name="color">Color to assign.</param>
        public static void Assign(GameObject target, Color color)
        {
            if (target == null)
                return;

            s_SingleBuffer.Clear();
            s_SingleBuffer.Add(target);
            Assign(s_SingleBuffer, color, null, ApplyScope.SelectionOnly);
        }

        /// <summary>Assigns a color to several GameObjects as a single undoable operation.</summary>
        /// <param name="targets">GameObjects to color.</param>
        /// <param name="color">Color to assign.</param>
        /// <param name="presetId">Optional identifier of the preset the color came from.</param>
        /// <param name="scope">Whether children are included.</param>
        /// <param name="continuousEdit">
        /// When <c>true</c> consecutive calls collapse into one undo step, for live color picker dragging.
        /// </param>
        /// <returns>The number of GameObjects that were changed.</returns>
        public static int Assign(IReadOnlyList<GameObject> targets, Color color, string presetId = null,
            ApplyScope scope = ApplyScope.SelectionOnly, bool continuousEdit = false)
        {
            var expanded = ExpandTargets(targets, scope);
            if (expanded.Count == 0)
                return 0;

            if (!ObjectIdentity.TryBuildKeys(expanded, s_KeyBuffer, s_AcceptedBuffer))
                return 0;

            var store = Store;
            if (continuousEdit)
                UndoScope.RecordContinuous(store, UiStrings.UndoAssignColor);
            else
                UndoScope.Record(store, UiStrings.UndoAssignColor);

            Color32 packed = color;
            string preset = presetId ?? string.Empty;
            for (int i = 0; i < s_KeyBuffer.Count; i++)
                store.SetAssignment(s_KeyBuffer[i], packed, preset);

            LastUsedColor.Set(color, preset);
            NotifyDataChanged();
            return s_KeyBuffer.Count;
        }

        /// <summary>Removes the assigned color from several GameObjects as a single undoable operation.</summary>
        /// <param name="targets">GameObjects to clear.</param>
        /// <param name="scope">Whether children are included.</param>
        /// <returns>The number of GameObjects that were changed.</returns>
        public static int Clear(IReadOnlyList<GameObject> targets, ApplyScope scope = ApplyScope.SelectionOnly)
        {
            var expanded = ExpandTargets(targets, scope);
            if (expanded.Count == 0)
                return 0;

            if (!ObjectIdentity.TryBuildKeys(expanded, s_KeyBuffer, s_AcceptedBuffer))
                return 0;

            var store = Store;
            int matches = 0;
            for (int i = 0; i < s_KeyBuffer.Count; i++)
            {
                if (store.IndexOfKey(s_KeyBuffer[i]) >= 0)
                    matches++;
            }

            if (matches == 0)
                return 0;

            UndoScope.Record(store, UiStrings.UndoClearColor);
            int removed = 0;
            for (int i = 0; i < s_KeyBuffer.Count; i++)
            {
                if (store.RemoveAssignment(s_KeyBuffer[i]))
                    removed++;
            }

            NotifyDataChanged();
            return removed;
        }

        /// <summary><c>true</c> when at least one of the supplied GameObjects has an assigned color.</summary>
        /// <param name="targets">GameObjects to test.</param>
        public static bool AnyHasColor(IReadOnlyList<GameObject> targets)
        {
            if (targets == null)
                return false;

            s_Index.RebuildIfNeeded(Store);
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null && s_Index.TryGetColor(RowId.Of(targets[i]), out _))
                    return true;
            }

            return false;
        }

        /// <summary>Removes every stored color from the project as a single undoable operation.</summary>
        /// <returns>The number of removed assignments.</returns>
        public static int ClearAll()
        {
            var store = Store;
            int count = store.Assignments.Count;
            if (count == 0)
                return 0;

            UndoScope.Record(store, UiStrings.UndoClearAllColors);
            store.Assignments.Clear();
            NotifyDataChanged();
            return count;
        }

        /// <summary>
        /// Removes stored colors whose GameObject no longer exists. Only scenes that are currently open
        /// are inspected, so colors belonging to closed scenes are never lost.
        /// </summary>
        /// <returns>The number of removed assignments.</returns>
        public static int RemoveMissingAssignments()
        {
            var store = Store;
            s_Index.CollectStaleAssignments(store, s_StaleBuffer);
            if (s_StaleBuffer.Count == 0)
                return 0;

            UndoScope.Record(store, UiStrings.UndoPrune);
            for (int i = s_StaleBuffer.Count - 1; i >= 0; i--)
            {
                int index = s_StaleBuffer[i];
                if (index >= 0 && index < store.Assignments.Count)
                    store.Assignments.RemoveAt(index);
            }

            NotifyDataChanged();
            return s_StaleBuffer.Count;
        }

        /// <summary>Selects every colored GameObject in the scenes that are currently open.</summary>
        /// <returns>The number of selected GameObjects.</returns>
        public static int SelectColoredObjects()
        {
            s_Index.RebuildIfNeeded(Store);

            var found = new List<UnityEngine.Object>();
            foreach (RowId rowId in s_Index.ResolvedRowIds)
            {
                var candidate = rowId.ToObject();
                if (candidate != null)
                    found.Add(candidate);
            }

            Selection.objects = found.ToArray();
            return found.Count;
        }

        /// <summary>Notifies the plugin that appearance settings or presets changed.</summary>
        internal static void NotifySettingsChanged()
        {
            HierarchyColorStoreProvider.MarkChanged();
            Changed?.Invoke();
            EditorCompat.RepaintHierarchy();
        }

        /// <summary>Notifies the plugin that the assignment table changed.</summary>
        internal static void NotifyDataChanged()
        {
            s_Index.InvalidateData();
            HierarchyColorStoreProvider.MarkChanged();
            Changed?.Invoke();
            EditorCompat.RepaintHierarchy();
        }

        /// <summary>Invalidates every cache after an undo, a reload or an external data change.</summary>
        internal static void InvalidateAll()
        {
            Store.InvalidateLookup();
            s_Index.InvalidateData();
            Changed?.Invoke();
            EditorCompat.RepaintHierarchy();
        }

        /// <summary>Invalidates the identity resolution after the Hierarchy content changed.</summary>
        internal static void InvalidateResolution()
        {
            s_Index.InvalidateResolution();
        }

        /// <summary>
        /// Re-keys assignments after a scene has been saved. Objects that were created before the save
        /// receive their final local file identifier at that moment, so session-scoped keys are promoted
        /// and changed identifiers are rewritten.
        /// </summary>
        /// <param name="scene">The scene that was saved.</param>
        internal static void ReconcileAfterSceneSave(Scene scene)
        {
            var store = Store;
            if (store.Assignments.Count == 0)
                return;

            s_Index.RebuildIfNeeded(store);

            var rowIds = new List<RowId>(s_Index.ResolvedRowIds);
            if (rowIds.Count == 0)
                return;

            s_TargetBuffer.Clear();
            var storeIndices = new List<int>(rowIds.Count);
            for (int i = 0; i < rowIds.Count; i++)
            {
                var gameObject = rowIds[i].ToObject() as GameObject;
                if (gameObject == null)
                    continue;
                if (scene.IsValid() && gameObject.scene.IsValid() && gameObject.scene != scene)
                    continue;
                if (!s_Index.TryGetStoreIndex(rowIds[i], out int storeIndex))
                    continue;

                s_TargetBuffer.Add(gameObject);
                storeIndices.Add(storeIndex);
            }

            if (s_TargetBuffer.Count == 0)
                return;

            if (!ObjectIdentity.TryBuildKeys(s_TargetBuffer, s_KeyBuffer, s_AcceptedBuffer))
                return;

            int rewritten = 0;
            for (int i = 0; i < s_KeyBuffer.Count && i < storeIndices.Count; i++)
            {
                int storeIndex = storeIndices[i];
                if (storeIndex < 0 || storeIndex >= store.Assignments.Count)
                    continue;

                var assignment = store.Assignments[storeIndex];
                if (assignment == null || assignment.Key == s_KeyBuffer[i])
                    continue;

                assignment.Key = s_KeyBuffer[i];
                rewritten++;
            }

            if (rewritten > 0)
            {
                StudioLog.Info(rewritten + " color assignment(s) were re-keyed after saving " + scene.name + ".");
                s_Index.InvalidateData();
                HierarchyColorStoreProvider.MarkChanged();
            }
        }

        private static List<GameObject> ExpandTargets(IReadOnlyList<GameObject> targets, ApplyScope scope)
        {
            s_TargetBuffer.Clear();
            s_DedupeBuffer.Clear();
            if (targets == null)
                return s_TargetBuffer;

            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target == null)
                    continue;

                AddTarget(target);
                if (scope == ApplyScope.SelectionOnly)
                    continue;

                if (scope == ApplyScope.DirectChildren)
                {
                    var transform = target.transform;
                    for (int child = 0; child < transform.childCount; child++)
                        AddTarget(transform.GetChild(child).gameObject);
                }
                else
                {
                    var descendants = target.GetComponentsInChildren<Transform>(true);
                    for (int d = 0; d < descendants.Length; d++)
                        AddTarget(descendants[d].gameObject);
                }
            }

            return s_TargetBuffer;
        }

        private static void AddTarget(GameObject target)
        {
            if (target != null && s_DedupeBuffer.Add(RowId.Of(target)))
                s_TargetBuffer.Add(target);
        }
    }
}
