using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Isolates the Editor APIs whose shape differs between the supported Unity releases, so the rest of
    /// the plugin is written against a single surface.
    /// </summary>
    /// <remarks>
    /// The version boundaries are declared once, as <c>versionDefines</c> in the plugin's assembly
    /// definition:
    /// <list type="bullet">
    /// <item><description><c>HCS_ENTITY_ID_API</c> — Unity replaced 32-bit instance ids with
    /// <c>UnityEngine.EntityId</c> and hard-deprecated the previous API, including the Hierarchy GUI
    /// callback.</description></item>
    /// <item><description><c>HCS_PREFAB_STAGE_ASSET_PATH</c> — Unity 6 reinstated
    /// <c>PrefabStage.assetPath</c> and deprecated <c>PrefabStage.prefabAssetPath</c>, reversing the
    /// 2020.1 change.</description></item>
    /// </list>
    /// </remarks>
    internal static class EditorCompat
    {
        private static Action<RowId, Rect> s_RowHandler;
        private static Action s_UndoRedoCallback;

        /// <summary>
        /// Subscribes to the Hierarchy row GUI callback, replacing any previous subscription so a domain
        /// reload cannot produce duplicate handlers.
        /// </summary>
        /// <param name="handler">Invoked for every visible Hierarchy row.</param>
        internal static void SubscribeHierarchyRowGUI(Action<RowId, Rect> handler)
        {
            s_RowHandler = handler;

#if HCS_ENTITY_ID_API
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI -= OnHierarchyRowGUI;
            if (handler != null)
                EditorApplication.hierarchyWindowItemByEntityIdOnGUI += OnHierarchyRowGUI;
#else
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyRowGUI;
            if (handler != null)
                EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyRowGUI;
#endif
        }

#if HCS_ENTITY_ID_API
        private static void OnHierarchyRowGUI(EntityId entityId, Rect rowRect)
        {
            s_RowHandler?.Invoke(new RowId(entityId), rowRect);
        }
#else
        private static void OnHierarchyRowGUI(int instanceId, Rect rowRect)
        {
            s_RowHandler?.Invoke(new RowId(instanceId), rowRect);
        }
#endif

        /// <summary>Returns the asset path of the prefab a Prefab Mode stage is editing.</summary>
        /// <param name="stage">Prefab stage to query.</param>
        internal static string GetPrefabStageAssetPath(PrefabStage stage)
        {
            if (stage == null)
                return string.Empty;

#if HCS_PREFAB_STAGE_ASSET_PATH
            return stage.assetPath ?? string.Empty;
#else
            return stage.prefabAssetPath ?? string.Empty;
#endif
        }

        /// <summary>Subscribes to Unity's undo/redo notification, using the newest available API.</summary>
        /// <param name="callback">Invoked after an undo or a redo has been applied.</param>
        internal static void SubscribeUndoRedo(Action callback)
        {
            s_UndoRedoCallback = callback;

#if UNITY_2022_2_OR_NEWER
            Undo.undoRedoEvent -= OnUndoRedoEvent;
            if (callback != null)
                Undo.undoRedoEvent += OnUndoRedoEvent;
#else
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            if (callback != null)
                Undo.undoRedoPerformed += OnUndoRedoPerformed;
#endif
        }

#if UNITY_2022_2_OR_NEWER
        private static void OnUndoRedoEvent(in UndoRedoInfo info)
        {
            s_UndoRedoCallback?.Invoke();
        }
#else
        private static void OnUndoRedoPerformed()
        {
            s_UndoRedoCallback?.Invoke();
        }
#endif

        /// <summary>Draws a filled rectangle with optional corner rounding, without allocating a texture.</summary>
        /// <param name="rect">Rectangle to fill.</param>
        /// <param name="color">Fill color, alpha blended.</param>
        /// <param name="cornerRadius">Corner radius in points. Zero draws a sharp rectangle.</param>
        internal static void DrawRoundedRect(Rect rect, Color color, float cornerRadius)
        {
            if (rect.width <= 0f || rect.height <= 0f || color.a <= 0f)
                return;

            if (cornerRadius <= 0f)
            {
                EditorGUI.DrawRect(rect, color);
                return;
            }

            UnityEngine.GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill,
                false, 0f, color, Vector4.zero, cornerRadius);
        }

        /// <summary>Requests a repaint of the Hierarchy window.</summary>
        internal static void RepaintHierarchy()
        {
            EditorApplication.RepaintHierarchyWindow();
        }
    }
}
