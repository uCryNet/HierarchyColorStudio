using UnityEditor;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Draws the appearance controls. The same code backs both the Color Studio window and the
    /// Project Settings page, so the two can never drift apart.
    /// </summary>
    /// <remarks>
    /// Controls write into local variables first. The store is snapshotted for undo only when a change
    /// is detected, and the new values are written afterwards, which keeps the undo state correct.
    /// </remarks>
    internal static class AppearanceSectionGUI
    {
        private static bool s_AdvancedExpanded;

        /// <summary>Draws every appearance control and applies changes with undo support.</summary>
        /// <param name="store">Store whose appearance settings are edited.</param>
        internal static void Draw(HierarchyColorStore store)
        {
            if (store == null)
                return;

            var settings = store.Appearance;

            bool enabled = settings.Enabled;
            var decorations = settings.Decorations;
            var markerShape = settings.MarkerShape;
            var markerPlacement = settings.MarkerPlacement;
            float markerSize = settings.MarkerSize;
            float tintOpacity = settings.TintOpacity;
            var tintScope = settings.TintScope;
            float labelBrightness = settings.LabelBrightness;
            bool labelFills = settings.LabelFillsBackground;
            var selectedBehavior = settings.SelectedRowBehavior;
            var hoverBehavior = settings.HoverBehavior;
            var applyScope = settings.DefaultApplyScope;
            float labelOffset = settings.LabelOffset;
            Color rowDark = settings.RowBackgroundDark;
            Color rowLight = settings.RowBackgroundLight;
            bool debugLogging = settings.DebugLogging;

            bool changed;
            EditorGUI.BeginChangeCheck();

            enabled = EditorGUILayout.Toggle(
                new GUIContent(UiStrings.LabelEnabled, UiStrings.TooltipEnabled), enabled);

            using (new EditorGUI.DisabledScope(!enabled))
            {
                decorations = (HierarchyDecorations)EditorGUILayout.EnumFlagsField(
                    new GUIContent(UiStrings.LabelDecorations, UiStrings.TooltipDecorations), decorations);

                EditorGUILayout.Space(2f);

                using (new EditorGUI.DisabledScope((decorations & HierarchyDecorations.Marker) == 0))
                {
                    markerShape = (MarkerShape)EditorGUILayout.EnumPopup(UiStrings.LabelMarkerShape, markerShape);
                    markerPlacement = (MarkerPlacement)EditorGUILayout.EnumPopup(
                        UiStrings.LabelMarkerPlacement, markerPlacement);
                    markerSize = EditorGUILayout.Slider(
                        new GUIContent(UiStrings.LabelMarkerSize, UiStrings.TooltipMarkerSize),
                        markerSize, AppearanceSettings.MinMarkerSize, AppearanceSettings.MaxMarkerSize);
                }

                EditorGUILayout.Space(2f);

                using (new EditorGUI.DisabledScope((decorations & HierarchyDecorations.RowTint) == 0))
                {
                    tintOpacity = EditorGUILayout.Slider(
                        new GUIContent(UiStrings.LabelTintOpacity, UiStrings.TooltipTintOpacity),
                        tintOpacity, AppearanceSettings.MinTintOpacity, AppearanceSettings.MaxTintOpacity);
                    tintScope = (TintScope)EditorGUILayout.EnumPopup(UiStrings.LabelTintScope, tintScope);
                }

                EditorGUILayout.Space(2f);

                using (new EditorGUI.DisabledScope((decorations & HierarchyDecorations.LabelColor) == 0))
                {
                    labelBrightness = EditorGUILayout.Slider(
                        new GUIContent(UiStrings.LabelLabelBrightness, UiStrings.TooltipLabelBrightness),
                        labelBrightness, AppearanceSettings.MinLabelBrightness, AppearanceSettings.MaxLabelBrightness);
                    labelFills = EditorGUILayout.Toggle(
                        new GUIContent(UiStrings.LabelLabelFill, UiStrings.TooltipLabelFill), labelFills);
                }

                EditorGUILayout.Space(2f);

                selectedBehavior = (SelectedRowBehavior)EditorGUILayout.EnumPopup(
                    new GUIContent(UiStrings.LabelSelectedBehavior, UiStrings.TooltipSelectedBehavior),
                    selectedBehavior);
                hoverBehavior = (HoverBehavior)EditorGUILayout.EnumPopup(
                    new GUIContent(UiStrings.LabelHoverBehavior, UiStrings.TooltipHoverBehavior), hoverBehavior);
                applyScope = (ApplyScope)EditorGUILayout.EnumPopup(
                    new GUIContent(UiStrings.LabelApplyScope, UiStrings.TooltipApplyScope), applyScope);

                // The foldout itself is deliberately outside the change check: expanding a section is not
                // a data change and must not create an undo step.
                changed = EditorGUI.EndChangeCheck();

                EditorGUILayout.Space(4f);
                s_AdvancedExpanded = EditorGUILayout.Foldout(s_AdvancedExpanded, UiStrings.SectionAdvanced, true);
                if (s_AdvancedExpanded)
                {
                    EditorGUI.BeginChangeCheck();
                    using (new EditorGUI.IndentLevelScope())
                    {
                        labelOffset = EditorGUILayout.Slider(
                            new GUIContent(UiStrings.LabelLabelOffset, UiStrings.TooltipLabelOffset),
                            labelOffset, 0f, 40f);
                        rowDark = EditorGUILayout.ColorField(UiStrings.LabelRowBackgroundDark, rowDark);
                        rowLight = EditorGUILayout.ColorField(UiStrings.LabelRowBackgroundLight, rowLight);
                        debugLogging = EditorGUILayout.Toggle(
                            new GUIContent(UiStrings.LabelDebugLogging, UiStrings.TooltipDebugLogging), debugLogging);
                    }

                    changed |= EditorGUI.EndChangeCheck();
                }
            }

            if (changed)
            {
                UndoScope.RecordContinuous(store, UiStrings.UndoEditAppearance);
                settings.Enabled = enabled;
                settings.Decorations = decorations;
                settings.MarkerShape = markerShape;
                settings.MarkerPlacement = markerPlacement;
                settings.MarkerSize = markerSize;
                settings.TintOpacity = tintOpacity;
                settings.TintScope = tintScope;
                settings.LabelBrightness = labelBrightness;
                settings.LabelFillsBackground = labelFills;
                settings.SelectedRowBehavior = selectedBehavior;
                settings.HoverBehavior = hoverBehavior;
                settings.DefaultApplyScope = applyScope;
                settings.LabelOffset = labelOffset;
                settings.RowBackgroundDark = rowDark;
                settings.RowBackgroundLight = rowLight;
                settings.DebugLogging = debugLogging;
                settings.Sanitize();
                HierarchyColorService.NotifySettingsChanged();
            }

            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(UiStrings.ButtonResetAppearance, EditorStyles.miniButton, GUILayout.Width(140f)))
                {
                    UndoScope.Record(store, UiStrings.UndoEditAppearance);
                    store.ResetAppearance();
                    HierarchyColorService.NotifySettingsChanged();
                }

                GUILayout.FlexibleSpace();
            }
        }
    }
}
