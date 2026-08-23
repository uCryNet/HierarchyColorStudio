using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Maps Hierarchy instance ids to assigned colors.
    /// </summary>
    /// <remarks>
    /// The Hierarchy GUI callback runs for every visible row of every repaint, so it may only perform a
    /// dictionary lookup. All identity resolution happens here, in two cached stages that are rebuilt
    /// from the Editor update loop:
    /// <list type="number">
    /// <item><description>Parsing persisted keys into identifiers — invalidated when the store changes.</description></item>
    /// <item><description>Resolving identifiers to instance ids — invalidated when the open scenes, the
    /// Prefab Mode stage or the Hierarchy content change.</description></item>
    /// </list>
    /// Only identifiers that belong to a currently loaded scene, or to the prefab that is open in Prefab
    /// Mode, take part in resolution, so the cost scales with what the user can actually see rather than
    /// with the size of the project.
    /// </remarks>
    internal sealed class AssignmentIndex
    {
        private struct ParsedAssignment
        {
            public GlobalObjectId Id;
            public string Guid;
            public Color32 Color;
            public RowId SessionRowId;
            public bool IsSessionScoped;
            public int StoreIndex;
        }

        private readonly List<ParsedAssignment> m_Parsed = new List<ParsedAssignment>(64);
        private readonly Dictionary<RowId, int> m_RowToParsed = new Dictionary<RowId, int>(64);
        private readonly HashSet<string> m_ActiveSceneGuids = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<GlobalObjectId> m_ResolveIds = new List<GlobalObjectId>(64);
        private readonly List<int> m_ResolveParsedIndices = new List<int>(64);
        private readonly Dictionary<string, int> m_PrefabKeyLookup = new Dictionary<string, int>(32, StringComparer.Ordinal);

        private bool m_ParseDirty = true;
        private bool m_ResolveDirty = true;
        private string m_PrefabStageGuid;

        /// <summary>Number of assignments that currently resolve to a visible GameObject.</summary>
        internal int ResolvedCount => m_RowToParsed.Count;

        /// <summary><c>true</c> when at least one visible assignment uses a session-scoped key.</summary>
        internal bool HasSessionScopedAssignments { get; private set; }

        /// <summary>Marks the persisted data as changed.</summary>
        internal void InvalidateData()
        {
            m_ParseDirty = true;
            m_ResolveDirty = true;
        }

        /// <summary>Marks the scene, prefab stage or Hierarchy content as changed.</summary>
        internal void InvalidateResolution()
        {
            m_ResolveDirty = true;
        }

        /// <summary><c>true</c> when a rebuild is pending.</summary>
        internal bool IsDirty => m_ParseDirty || m_ResolveDirty;

        /// <summary>Looks up the color of a Hierarchy row. This is the only method called during repaint.</summary>
        /// <param name="rowId">Object identifier supplied by the Hierarchy GUI callback.</param>
        /// <param name="color">Receives the assigned color.</param>
        /// <returns><c>true</c> when the row has an assigned color.</returns>
        internal bool TryGetColor(RowId rowId, out Color32 color)
        {
            if (m_RowToParsed.TryGetValue(rowId, out int parsedIndex) && parsedIndex < m_Parsed.Count)
            {
                color = m_Parsed[parsedIndex].Color;
                return true;
            }

            color = default;
            return false;
        }

        /// <summary>Rebuilds whichever cache stage is out of date.</summary>
        /// <param name="store">Store holding the persisted assignments.</param>
        internal void RebuildIfNeeded(HierarchyColorStore store)
        {
            if (store == null)
                return;

            try
            {
                if (m_ParseDirty)
                {
                    Parse(store);
                    m_ParseDirty = false;
                    m_ResolveDirty = true;
                }

                if (m_ResolveDirty)
                {
                    Resolve();
                    m_ResolveDirty = false;
                }
            }
            catch (Exception exception)
            {
                m_ParseDirty = false;
                m_ResolveDirty = false;
                m_RowToParsed.Clear();
                StudioLog.ExceptionOnce("index-rebuild", exception, "Could not rebuild the Hierarchy color index.");
            }
        }

        /// <summary>Clears every cached mapping.</summary>
        internal void Clear()
        {
            m_Parsed.Clear();
            m_RowToParsed.Clear();
            m_ActiveSceneGuids.Clear();
            m_ParseDirty = true;
            m_ResolveDirty = true;
        }

        /// <summary>Enumerates the identifiers of every currently resolved assignment.</summary>
        internal IEnumerable<RowId> ResolvedRowIds => m_RowToParsed.Keys;

        /// <summary>Maps a resolved instance id back to its index in the store's assignment list.</summary>
        /// <param name="rowId">Identifier of a resolved GameObject.</param>
        /// <param name="storeIndex">Receives the index in <see cref="HierarchyColorStore.Assignments"/>.</param>
        /// <returns><c>true</c> when the identifier is resolved.</returns>
        internal bool TryGetStoreIndex(RowId rowId, out int storeIndex)
        {
            storeIndex = -1;
            if (!m_RowToParsed.TryGetValue(rowId, out int parsedIndex) || parsedIndex >= m_Parsed.Count)
                return false;

            storeIndex = m_Parsed[parsedIndex].StoreIndex;
            return true;
        }

        /// <summary>
        /// Collects the store indices of assignments that belong to a loaded scene but no longer resolve
        /// to a GameObject. Assignments from scenes that are not open are never reported.
        /// </summary>
        /// <param name="store">Store holding the persisted assignments.</param>
        /// <param name="staleStoreIndices">Receives the store indices that can safely be removed.</param>
        internal void CollectStaleAssignments(HierarchyColorStore store, List<int> staleStoreIndices)
        {
            staleStoreIndices.Clear();
            if (store == null)
                return;

            RebuildIfNeeded(store);

            var resolvedParsed = new HashSet<int>();
            foreach (var pair in m_RowToParsed)
                resolvedParsed.Add(pair.Value);

            for (int i = 0; i < m_Parsed.Count; i++)
            {
                if (resolvedParsed.Contains(i))
                    continue;

                var parsed = m_Parsed[i];
                bool inActiveContext = parsed.IsSessionScoped
                    ? parsed.SessionRowId.ToObject() == null
                    : m_ActiveSceneGuids.Contains(parsed.Guid) || parsed.Guid == m_PrefabStageGuid;

                if (inActiveContext)
                    staleStoreIndices.Add(parsed.StoreIndex);
            }

            staleStoreIndices.Sort();
        }

        private void Parse(HierarchyColorStore store)
        {
            m_Parsed.Clear();
            var assignments = store.Assignments;

            for (int i = 0; i < assignments.Count; i++)
            {
                var assignment = assignments[i];
                if (assignment == null || !assignment.IsValid())
                    continue;

                if (ColorAssignment.TryReadSessionRowId(assignment.Key, out RowId sessionRowId))
                {
                    m_Parsed.Add(new ParsedAssignment
                    {
                        Color = assignment.Color,
                        SessionRowId = sessionRowId,
                        IsSessionScoped = true,
                        StoreIndex = i,
                        Guid = string.Empty
                    });
                    continue;
                }

                if (!ObjectIdentity.TryParseGlobalId(assignment.Key, out var id))
                    continue;

                m_Parsed.Add(new ParsedAssignment
                {
                    Id = id,
                    Guid = ObjectIdentity.GetAssetGuid(id),
                    Color = assignment.Color,
                    StoreIndex = i
                });
            }
        }

        private void Resolve()
        {
            m_RowToParsed.Clear();
            HasSessionScopedAssignments = false;

            CollectActiveContexts();

            if (m_Parsed.Count == 0)
                return;

            ResolveSceneAssignments();
            ResolvePrefabStageAssignments();
            ResolveSessionScopedAssignments();
        }

        private void CollectActiveContexts()
        {
            m_ActiveSceneGuids.Clear();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
                    continue;

                string guid = AssetDatabase.AssetPathToGUID(scene.path);
                if (!ObjectIdentity.IsEmptyGuid(guid))
                    m_ActiveSceneGuids.Add(guid);
            }

            m_PrefabStageGuid = null;
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            string stagePath = EditorCompat.GetPrefabStageAssetPath(stage);
            if (!string.IsNullOrEmpty(stagePath))
            {
                string guid = AssetDatabase.AssetPathToGUID(stagePath);
                if (!ObjectIdentity.IsEmptyGuid(guid))
                    m_PrefabStageGuid = guid;
            }
        }

        private void ResolveSceneAssignments()
        {
            m_ResolveIds.Clear();
            m_ResolveParsedIndices.Clear();

            for (int i = 0; i < m_Parsed.Count; i++)
            {
                var parsed = m_Parsed[i];
                if (parsed.IsSessionScoped || !m_ActiveSceneGuids.Contains(parsed.Guid))
                    continue;

                m_ResolveIds.Add(parsed.Id);
                m_ResolveParsedIndices.Add(i);
            }

            if (m_ResolveIds.Count == 0)
                return;

            var ids = m_ResolveIds.ToArray();
            var resolved = new RowId[ids.Length];
            RowId.Resolve(ids, resolved);

            for (int i = 0; i < resolved.Length; i++)
            {
                if (!resolved[i].IsValid)
                    continue;
                m_RowToParsed[resolved[i]] = m_ResolveParsedIndices[i];
            }
        }

        private void ResolvePrefabStageAssignments()
        {
            if (string.IsNullOrEmpty(m_PrefabStageGuid))
                return;

            m_PrefabKeyLookup.Clear();
            for (int i = 0; i < m_Parsed.Count; i++)
            {
                var parsed = m_Parsed[i];
                if (!parsed.IsSessionScoped && parsed.Guid == m_PrefabStageGuid)
                    m_PrefabKeyLookup[parsed.Id.ToString()] = i;
            }

            if (m_PrefabKeyLookup.Count == 0)
                return;

            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            var root = stage != null ? stage.prefabContentsRoot : null;
            if (root == null)
                return;

            // Prefab Mode edits a preview scene, so persisted asset identifiers do not resolve to the
            // objects on screen. The contents of a single prefab are small enough to walk directly.
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                var gameObject = transforms[i].gameObject;
                string key = GlobalObjectId.GetGlobalObjectIdSlow(gameObject).ToString();
                if (m_PrefabKeyLookup.TryGetValue(key, out int parsedIndex))
                    m_RowToParsed[RowId.Of(gameObject)] = parsedIndex;
            }
        }

        private void ResolveSessionScopedAssignments()
        {
            for (int i = 0; i < m_Parsed.Count; i++)
            {
                var parsed = m_Parsed[i];
                if (!parsed.IsSessionScoped)
                    continue;

                if (parsed.SessionRowId.ToObject() == null)
                    continue;

                m_RowToParsed[parsed.SessionRowId] = i;
                HasSessionScopedAssignments = true;
            }
        }
    }
}
