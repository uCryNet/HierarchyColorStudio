using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>What Unity draws as the icon of a Hierarchy row, as far as icon replacement is concerned.</summary>
    internal enum RowKind : byte
    {
        /// <summary>Unity's icon carries information and is never replaced or hidden: a scene header, a
        /// missing prefab, an object with an icon chosen in the Inspector, an added prefab override.</summary>
        Keep = 0,

        /// <summary>An ordinary GameObject, drawn with Unity's generic GameObject icon.</summary>
        GameObject = 1,

        /// <summary>A grouping object: it has children and no component besides its Transform.</summary>
        Folder = 2,

        /// <summary>The root of a prefab instance, drawn with one of Unity's prefab icons.</summary>
        PrefabRoot = 3
    }

    /// <summary>Classification of one row, cached between repaints.</summary>
    internal readonly struct RowInfo
    {
        /// <summary>What Unity draws as the row's icon.</summary>
        internal readonly RowKind Kind;

        /// <summary><c>true</c> when the GameObject is inactive in the Hierarchy, which Unity draws dimmed.</summary>
        internal readonly bool Inactive;

        internal RowInfo(RowKind kind, bool inactive)
        {
            Kind = kind;
            Inactive = inactive;
        }
    }

    /// <summary>
    /// Classifies Hierarchy rows for the features that replace or hide Unity's own row icon: Minimal mode,
    /// folder and prefab icons, and style icons.
    /// </summary>
    /// <remarks>
    /// Classifying a row touches the GameObject — its components, children and prefab status — so results
    /// are cached per row and dropped whenever the Hierarchy, a component or a prefab connection may have
    /// changed. Only rows that are actually drawn are ever classified, so the cost is bounded by the
    /// window's height, not by the size of the scene. Nothing is subscribed until the first row is
    /// classified: a project that never uses these features pays nothing for them.
    /// </remarks>
    internal static class HierarchyRowClassifier
    {
        private static readonly Dictionary<RowId, RowInfo> s_Cache = new Dictionary<RowId, RowInfo>(128);
        private static readonly List<Component> s_Components = new List<Component>(4);

        private static bool s_Subscribed;

        /// <summary>Drops every cached classification.</summary>
        internal static void Invalidate()
        {
            s_Cache.Clear();
        }

        /// <summary>Returns the classification of a row, computing it on first use.</summary>
        /// <param name="rowId">Identifier of the row's object.</param>
        internal static RowInfo Classify(RowId rowId)
        {
            if (s_Cache.TryGetValue(rowId, out RowInfo info))
                return info;

            EnsureSubscribed();
            info = ClassifyUncached(rowId.ToObject() as GameObject);
            s_Cache[rowId] = info;
            return info;
        }

        /// <summary>Classifies a GameObject without using the cache.</summary>
        /// <param name="gameObject">GameObject to classify, or <c>null</c> for a row that is not one.</param>
        internal static RowInfo ClassifyUncached(GameObject gameObject)
        {
            if (gameObject == null)
                return new RowInfo(RowKind.Keep, false);

            bool inactive = !gameObject.activeInHierarchy;

            // An icon picked in the Inspector is the user's own decision and always wins.
            if (EditorGUIUtility.GetIconForObject(gameObject) != null)
                return new RowInfo(RowKind.Keep, inactive);

            if (PrefabUtility.IsAnyPrefabInstanceRoot(gameObject))
            {
                // A broken prefab connection is shown by Unity's icon and must stay visible.
                var kind = PrefabUtility.IsPrefabAssetMissing(gameObject) ? RowKind.Keep : RowKind.PrefabRoot;
                return new RowInfo(kind, inactive);
            }

            // Unity marks a GameObject added to a prefab instance with an overlay on its icon.
            if (PrefabUtility.IsAddedGameObjectOverride(gameObject))
                return new RowInfo(RowKind.Keep, inactive);

            if (gameObject.transform.childCount > 0 && HasOnlyTransform(gameObject))
                return new RowInfo(RowKind.Folder, inactive);

            return new RowInfo(RowKind.GameObject, inactive);
        }

        private static bool HasOnlyTransform(GameObject gameObject)
        {
            // Missing scripts are returned as null entries, so an object with one is not a folder.
            gameObject.GetComponents(s_Components);
            bool onlyTransform = s_Components.Count == 1;
            s_Components.Clear();
            return onlyTransform;
        }

        private static void EnsureSubscribed()
        {
            if (s_Subscribed)
                return;

            s_Subscribed = true;
            ObjectChangeEvents.changesPublished -= OnObjectChangesPublished;
            ObjectChangeEvents.changesPublished += OnObjectChangesPublished;
        }

        private static void OnObjectChangesPublished(ref ObjectChangeEventStream stream)
        {
            // Components, prefab connections, active states and Inspector icons all arrive here. The cache
            // only holds visible rows, so dropping all of it is cheaper than working out what changed.
            if (stream.length > 0)
                Invalidate();
        }
    }
}
