using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Wires the plugin into the Editor's event loop.
    /// </summary>
    /// <remarks>
    /// Every subscription is removed before it is added, so a domain reload — or a second call from a
    /// different entry point — can never produce duplicate callbacks. No state is assumed to survive a
    /// reload: the store and every cache are rebuilt lazily on first use.
    /// </remarks>
    [InitializeOnLoad]
    internal static class HierarchyColorStudioBootstrap
    {
        private static int s_LastResolvedCount = -1;

        static HierarchyColorStudioBootstrap()
        {
            Install();
        }

        /// <summary>Installs every Editor event hook, replacing any previous subscription.</summary>
        internal static void Install()
        {
            EditorCompat.SubscribeHierarchyRowGUI(HierarchyRowRenderer.OnRowGUI);

            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;

            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;

            EditorApplication.quitting -= OnQuitting;
            EditorApplication.quitting += OnQuitting;

            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;

            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;

            EditorSceneManager.sceneClosed -= OnSceneClosed;
            EditorSceneManager.sceneClosed += OnSceneClosed;

            EditorSceneManager.newSceneCreated -= OnNewSceneCreated;
            EditorSceneManager.newSceneCreated += OnNewSceneCreated;

            EditorSceneManager.sceneSaved -= OnSceneSaved;
            EditorSceneManager.sceneSaved += OnSceneSaved;

            PrefabStage.prefabStageOpened -= OnPrefabStageOpened;
            PrefabStage.prefabStageOpened += OnPrefabStageOpened;

            PrefabStage.prefabStageClosing -= OnPrefabStageClosing;
            PrefabStage.prefabStageClosing += OnPrefabStageClosing;

            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;

            EditorCompat.SubscribeUndoRedo(OnUndoRedo);
        }

        private static void OnEditorUpdate()
        {
            HierarchyColorStoreProvider.Tick();

            var index = HierarchyColorService.Index;
            if (!index.IsDirty)
                return;

            index.RebuildIfNeeded(HierarchyColorService.Store);
            if (index.ResolvedCount != s_LastResolvedCount)
            {
                s_LastResolvedCount = index.ResolvedCount;
                EditorCompat.RepaintHierarchy();
            }
        }

        private static void OnHierarchyChanged()
        {
            HierarchyColorService.InvalidateResolution();
            HierarchyRowRenderer.InvalidateLabels();
        }

        private static void OnSelectionChanged()
        {
            HierarchySelectionCache.Invalidate();
            UndoScope.EndContinuous();
        }

        private static void OnUndoRedo()
        {
            HierarchyColorService.InvalidateAll();
            HierarchyColorStoreProvider.MarkChanged();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            HierarchyColorService.InvalidateResolution();
        }

        private static void OnSceneClosed(Scene scene)
        {
            HierarchyColorService.InvalidateResolution();
        }

        private static void OnNewSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode)
        {
            HierarchyColorService.InvalidateResolution();
        }

        private static void OnSceneSaved(Scene scene)
        {
            HierarchyColorService.ReconcileAfterSceneSave(scene);
            HierarchyColorService.InvalidateResolution();
        }

        private static void OnPrefabStageOpened(PrefabStage stage)
        {
            HierarchyColorService.InvalidateResolution();
        }

        private static void OnPrefabStageClosing(PrefabStage stage)
        {
            HierarchyColorService.InvalidateResolution();
        }

        private static void OnBeforeAssemblyReload()
        {
            HierarchyColorStoreProvider.Flush();
        }

        private static void OnQuitting()
        {
            HierarchyColorStoreProvider.Flush();
        }
    }
}
