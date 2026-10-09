using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Main product window: selection actions, preset management, appearance settings and maintenance.
    /// </summary>
    internal sealed class HierarchyColorStudioWindow : EditorWindow
    {
        private const float MinWindowWidth = 380f;
        private const float MinWindowHeight = 420f;
        private const int MaxListedSelection = 12;
        private const float SwatchWidth = 26f;

        private readonly PresetSectionGUI m_PresetSection = new PresetSectionGUI();
        private readonly List<GameObject> m_Selection = new List<GameObject>(16);

        private Vector2 m_ScrollPosition;
        private bool m_SelectionExpanded = true;
        private bool m_PresetsExpanded = true;
        private bool m_AppearanceExpanded = true;
        private bool m_MaintenanceExpanded;
        private bool m_AboutExpanded;

        /// <summary>Opens or focuses the window.</summary>
        internal static void Open()
        {
            var window = GetWindow<HierarchyColorStudioWindow>();
            window.Show();
            window.Focus();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent(UiStrings.WindowTitleShort, StudioStyles.ProductIcon, UiStrings.ProductName);
            minSize = new Vector2(MinWindowWidth, MinWindowHeight);

            HierarchyColorService.Changed -= Repaint;
            HierarchyColorService.Changed += Repaint;
            Selection.selectionChanged -= Repaint;
            Selection.selectionChanged += Repaint;
        }

        private void OnDisable()
        {
            HierarchyColorService.Changed -= Repaint;
            Selection.selectionChanged -= Repaint;
        }

        private void OnGUI()
        {
            var store = HierarchyColorService.Store;
            if (store == null)
            {
                EditorGUILayout.HelpBox("The configuration could not be loaded.", MessageType.Error);
                return;
            }

            CacheSelection();
            DrawToolbar(store);

            using (var scroll = new EditorGUILayout.ScrollViewScope(m_ScrollPosition))
            {
                m_ScrollPosition = scroll.scrollPosition;

                if (!store.Appearance.Enabled)
                    EditorGUILayout.HelpBox(UiStrings.HintDisabled, MessageType.Info);

                if (HierarchyColorService.HasSessionScopedAssignments)
                    EditorGUILayout.HelpBox(UiStrings.HintSessionScoped, MessageType.Info);

                DrawSection(UiStrings.SectionSelection, ref m_SelectionExpanded, () => DrawSelectionSection(store));
                DrawSection(UiStrings.SectionPresets, ref m_PresetsExpanded,
                    () => m_PresetSection.Draw(store, m_Selection));
                DrawSection(UiStrings.SectionAppearance, ref m_AppearanceExpanded, () => AppearanceSectionGUI.Draw(store));
                DrawSection(UiStrings.SectionMaintenance, ref m_MaintenanceExpanded, () => DrawMaintenanceSection(store));
                DrawSection(UiStrings.SectionAbout, ref m_AboutExpanded, AboutSectionGUI.Draw);
            }

            DrawFooter();
        }

        private void CacheSelection()
        {
            m_Selection.Clear();
            var selected = Selection.gameObjects;
            for (int i = 0; i < selected.Length; i++)
            {
                if (selected[i] != null)
                    m_Selection.Add(selected[i]);
            }
        }

        private void DrawToolbar(HierarchyColorStore store)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUI.BeginChangeCheck();
                bool enabled = GUILayout.Toggle(store.Appearance.Enabled,
                    new GUIContent(UiStrings.LabelEnabled, UiStrings.TooltipEnabled),
                    EditorStyles.toolbarButton, GUILayout.Width(150f));
                if (EditorGUI.EndChangeCheck())
                {
                    UndoScope.Record(store, UiStrings.UndoEditAppearance);
                    store.Appearance.Enabled = enabled;
                    HierarchyColorService.NotifySettingsChanged();
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(UiStrings.ButtonDocumentation, EditorStyles.toolbarButton, GUILayout.Width(110f)))
                    DocumentationLocator.Open();
            }
        }

        private void DrawSection(string title, ref bool expanded, System.Action body)
        {
            EditorGUILayout.Space(4f);
            expanded = EditorGUILayout.Foldout(expanded, title, true, EditorStyles.foldoutHeader);
            if (!expanded)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                body();
            }
        }

        private void DrawSelectionSection(HierarchyColorStore store)
        {
            if (m_Selection.Count == 0)
            {
                EditorGUILayout.LabelField(UiStrings.HintNoSelection, StudioStyles.Hint);
                return;
            }

            EditorGUILayout.LabelField(UiStrings.SelectionHeader(m_Selection.Count), StudioStyles.SectionHeader);

            var style = GatherSelectionStyle(out StyleFields mixed);
            var scope = LastUsedColor.Scope;
            var settings = store.Appearance;

            var result = StyleEditorGUI.Draw(style, mixed, settings, OnSelectionIconPicked);
            if (result.Changed != StyleFields.None)
                HierarchyColorService.ModifyStyle(m_Selection, result.Changed, result.Values, scope, result.Continuous);

            var previewRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 2f);
            previewRect = EditorGUI.PrefixLabel(previewRect, new GUIContent(UiStrings.LabelPreview));
            StylePreviewGUI.DrawRow(previewRect, style, settings, m_Selection[0].name);

            EditorGUILayout.Space(2f);
            EditorGUI.BeginChangeCheck();
            var newScope = (ApplyScope)EditorGUILayout.EnumPopup(
                new GUIContent(UiStrings.LabelApplyScopePopup, UiStrings.TooltipApplyScope), scope);
            if (EditorGUI.EndChangeCheck())
                LastUsedColor.Scope = newScope;

            if (mixed != StyleFields.None)
                EditorGUILayout.LabelField(UiStrings.HintMixedColors, StudioStyles.Hint);

            EditorGUILayout.Space(2f);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!HierarchyColorService.AnyHasStyle(m_Selection)))
                {
                    if (GUILayout.Button(new GUIContent(UiStrings.ButtonClear, UiStrings.TooltipClearStyle), GUILayout.Width(90f)))
                        HierarchyColorService.Clear(m_Selection, newScope);
                }

                if (GUILayout.Button(UiStrings.MenuApplyLastColor, GUILayout.Width(130f)))
                    HierarchyColorService.ApplyLastUsed(m_Selection, newScope);

                using (new EditorGUI.DisabledScope(mixed != StyleFields.None || style.IsEmpty))
                {
                    if (GUILayout.Button(new GUIContent(UiStrings.ButtonSaveStyleAsPreset, UiStrings.TooltipSaveStyleAsPreset)))
                        SaveSelectionStyleAsPreset(store, style);
                }

                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space(4f);
            DrawSelectionList();
        }

        private HierarchyStyle GatherSelectionStyle(out StyleFields mixed)
        {
            mixed = StyleFields.None;
            HierarchyColorService.TryGetStyle(m_Selection[0], out HierarchyStyle first);
            for (int i = 1; i < m_Selection.Count; i++)
            {
                HierarchyColorService.TryGetStyle(m_Selection[i], out HierarchyStyle other);
                mixed |= first.Differences(other);
                if (mixed == StyleFields.All)
                    break;
            }

            return first;
        }

        private static void OnSelectionIconPicked(string icon)
        {
            // The picker closes before the next frame, and the selection cannot change while it is open.
            HierarchyColorService.ModifyStyle(Selection.gameObjects, StyleFields.Icon,
                new HierarchyStyle { Icon = icon }, LastUsedColor.Scope);
        }

        private static void SaveSelectionStyleAsPreset(HierarchyColorStore store, HierarchyStyle style)
        {
            UndoScope.Record(store, UiStrings.UndoEditPresets);
            store.Presets.Add(new ColorPreset(UiStrings.NewPresetName, style));
            HierarchyColorService.NotifySettingsChanged();
        }

        private void DrawSelectionList()
        {
            int shown = Mathf.Min(m_Selection.Count, MaxListedSelection);
            for (int i = 0; i < shown; i++)
            {
                var target = m_Selection[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    var swatchRect = GUILayoutUtility.GetRect(SwatchWidth, EditorGUIUtility.singleLineHeight - 2f,
                        GUILayout.Width(SwatchWidth));
                    if (HierarchyColorService.TryGetStyle(target, out HierarchyStyle assigned))
                        StylePreviewGUI.DrawSwatch(swatchRect, assigned);
                    else
                        StudioStyles.DrawOutline(swatchRect, StudioStyles.BorderColor, 1f);

                    EditorGUILayout.LabelField(target.name);
                }
            }

            if (m_Selection.Count > shown)
                EditorGUILayout.LabelField("… and " + (m_Selection.Count - shown) + " more", StudioStyles.Hint);
        }

        private void DrawMaintenanceSection(HierarchyColorStore store)
        {
            EditorGUILayout.LabelField(
                UiStrings.AssignmentSummary(HierarchyColorService.StoredAssignmentCount,
                    HierarchyColorService.ResolvedAssignmentCount), StudioStyles.Hint);

            EditorGUILayout.Space(2f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent(UiStrings.ButtonSelectColored, UiStrings.TooltipSelectColored)))
                    HierarchyColorService.SelectColoredObjects();

                if (GUILayout.Button(new GUIContent(UiStrings.ButtonPruneMissing, UiStrings.TooltipPruneMissing)))
                    HierarchyColorService.RemoveMissingAssignments();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(UiStrings.MenuExport))
                    ColorTransfer.ExportWithDialog();

                if (GUILayout.Button(UiStrings.MenuImport))
                    ColorTransfer.ImportWithDialog();
            }

            EditorGUILayout.Space(2f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(UiStrings.ButtonClearAll))
                    MaintenanceActions.ClearAllWithConfirmation();

                if (GUILayout.Button(UiStrings.ButtonResetAll))
                    MaintenanceActions.FactoryResetWithConfirmation();
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Stored in " + HierarchyColorStoreFile.RelativePath, StudioStyles.Hint);

            using (new EditorGUI.DisabledScope(!HierarchyColorStoreProvider.HasUnsavedChanges))
            {
                if (GUILayout.Button(UiStrings.ButtonSaveNow, EditorStyles.miniButton, GUILayout.Width(90f)))
                    HierarchyColorStoreProvider.Flush();
            }
        }

        private void DrawFooter()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(UiStrings.ProductName + "  " + UiStrings.Version, EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label(UiStrings.Vendor, EditorStyles.miniLabel);
            }
        }
    }
}
