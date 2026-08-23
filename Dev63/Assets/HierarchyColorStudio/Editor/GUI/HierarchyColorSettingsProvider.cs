using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Registers the plugin's page in Project Settings. It reuses the same drawers as the Color Studio
    /// window so both surfaces always show identical controls.
    /// </summary>
    internal static class HierarchyColorSettingsProvider
    {
        private const float ContentMargin = 10f;

        private static readonly PresetSectionGUI s_PresetSection = new PresetSectionGUI();
        private static readonly List<GameObject> s_Selection = new List<GameObject>(8);

        private static Vector2 s_ScrollPosition;

        [SettingsProvider]
        private static SettingsProvider CreateProvider()
        {
            return new SettingsProvider(UiStrings.ProjectSettingsPath, SettingsScope.Project)
            {
                label = UiStrings.ProductName,
                guiHandler = OnGUI,
                keywords = new HashSet<string>
                {
                    "hierarchy", "color", "colour", "marker", "preset", "tint", "studio"
                }
            };
        }

        private static void OnGUI(string searchContext)
        {
            var store = HierarchyColorService.Store;
            if (store == null)
            {
                EditorGUILayout.HelpBox("The configuration could not be loaded.", MessageType.Error);
                return;
            }

            CacheSelection();

            using (var scroll = new EditorGUILayout.ScrollViewScope(s_ScrollPosition))
            {
                s_ScrollPosition = scroll.scrollPosition;

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(ContentMargin);
                    using (new EditorGUILayout.VerticalScope())
                    {
                        GUILayout.Space(ContentMargin);

                        EditorGUILayout.LabelField(UiStrings.SectionAppearance, StudioStyles.SectionHeader);
                        AppearanceSectionGUI.Draw(store);

                        StudioStyles.DrawSeparator(10f, 8f);

                        EditorGUILayout.LabelField(UiStrings.SectionPresets, StudioStyles.SectionHeader);
                        s_PresetSection.Draw(store, s_Selection);

                        StudioStyles.DrawSeparator(10f, 8f);

                        EditorGUILayout.LabelField(UiStrings.SectionMaintenance, StudioStyles.SectionHeader);
                        DrawMaintenance();

                        StudioStyles.DrawSeparator(10f, 8f);

                        EditorGUILayout.LabelField(UiStrings.SectionAbout, StudioStyles.SectionHeader);
                        AboutSectionGUI.Draw();

                        GUILayout.Space(ContentMargin);
                    }

                    GUILayout.Space(ContentMargin);
                }
            }
        }

        private static void DrawMaintenance()
        {
            EditorGUILayout.LabelField(
                UiStrings.AssignmentSummary(HierarchyColorService.StoredAssignmentCount,
                    HierarchyColorService.ResolvedAssignmentCount), StudioStyles.Hint);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(UiStrings.ButtonOpenStudio, GUILayout.Width(160f)))
                    HierarchyColorStudioWindow.Open();

                if (GUILayout.Button(new GUIContent(UiStrings.ButtonPruneMissing, UiStrings.TooltipPruneMissing),
                        GUILayout.Width(180f)))
                    HierarchyColorService.RemoveMissingAssignments();

                GUILayout.FlexibleSpace();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(UiStrings.ButtonClearAll, GUILayout.Width(160f)))
                    MaintenanceActions.ClearAllWithConfirmation();

                if (GUILayout.Button(UiStrings.ButtonResetAll, GUILayout.Width(180f)))
                    MaintenanceActions.FactoryResetWithConfirmation();

                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Stored in " + HierarchyColorStoreFile.RelativePath, StudioStyles.Hint);
        }

        private static void CacheSelection()
        {
            s_Selection.Clear();
            var selected = Selection.gameObjects;
            for (int i = 0; i < selected.Length; i++)
            {
                if (selected[i] != null)
                    s_Selection.Add(selected[i]);
            }
        }
    }
}
