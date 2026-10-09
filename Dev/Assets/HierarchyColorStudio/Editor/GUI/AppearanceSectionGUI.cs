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
        private const float IconButtonWidth = 64f;
        private const float ClearButtonWidth = 22f;
        private const float ColorFieldWidth = 60f;

        private static readonly GUIContent s_TextColorLabel = new GUIContent(UiStrings.LabelTextColor, UiStrings.TooltipTextColor);
        private static readonly GUIContent s_ModeLabel = new GUIContent(UiStrings.LabelHierarchyMode, UiStrings.TooltipHierarchyMode);
        private static readonly GUIContent[] s_ModeOptions =
        {
            new GUIContent(UiStrings.ModeDefault),
            new GUIContent(UiStrings.ModeZebra),
            new GUIContent(UiStrings.ModeMinimal),
            new GUIContent(UiStrings.ModeMinimalZebra)
        };

        // Popup order: Default, Zebra, Minimal, Minimal + Zebra — the flag values 0, 1, 2 and 3.
        private static readonly HierarchyMode[] s_ModeValues =
        {
            HierarchyMode.Default,
            HierarchyMode.Zebra,
            HierarchyMode.Minimal,
            HierarchyMode.Minimal | HierarchyMode.Zebra
        };

        private static readonly GUIContent s_FolderIconLabel = new GUIContent(UiStrings.LabelFolderIcon, UiStrings.TooltipFolderIcon);
        private static readonly GUIContent s_PrefabIconLabel = new GUIContent(UiStrings.LabelPrefabIcon, UiStrings.TooltipPrefabIcon);
        private static readonly GUIContent s_ChooseContent = new GUIContent(UiStrings.ButtonChooseIcon, UiStrings.TooltipChooseIcon);
        private static readonly GUIContent s_RemoveIconContent = new GUIContent("×", UiStrings.TooltipRemoveIcon);
        private static readonly GUIContent s_IconValue = new GUIContent();
        private static readonly System.Action<string> s_SetFolderIcon = SetFolderIcon;
        private static readonly System.Action<string> s_SetPrefabIcon = SetPrefabIcon;

        private static bool s_AdvancedExpanded;

        /// <summary>Draws every appearance control and applies changes with undo support.</summary>
        /// <param name="store">Store whose appearance settings are edited.</param>
        internal static void Draw(HierarchyColorStore store)
        {
            if (store == null)
                return;

            var settings = store.Appearance;

            bool enabled = settings.Enabled;
            var mode = settings.Mode;
            float zebraOpacity = settings.ZebraOpacity;
            var iconPlacement = settings.IconPlacement;
            var decorations = settings.Decorations;
            var markerShape = settings.MarkerShape;
            var markerPlacement = settings.MarkerPlacement;
            float markerSize = settings.MarkerSize;
            float tintOpacity = settings.TintOpacity;
            var tintScope = settings.TintScope;
            float labelBrightness = settings.LabelBrightness;
            var textColorSource = settings.TextColorSource;
            Color customTextColor = settings.CustomTextColor;
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
                int modeIndex = EditorGUILayout.Popup(s_ModeLabel, ModeIndex(mode), s_ModeOptions);
                mode = s_ModeValues[modeIndex];
                EditorGUILayout.LabelField(ModeHint(mode), StudioStyles.Hint);

                using (new EditorGUI.DisabledScope((mode & HierarchyMode.Zebra) == 0))
                {
                    zebraOpacity = EditorGUILayout.Slider(
                        new GUIContent(UiStrings.LabelZebraOpacity, UiStrings.TooltipZebraOpacity),
                        zebraOpacity, AppearanceSettings.MinZebraOpacity, AppearanceSettings.MaxZebraOpacity);
                }

                EditorGUILayout.Space(4f);

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
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        textColorSource = (TextColorSource)EditorGUILayout.EnumPopup(s_TextColorLabel, textColorSource);
                        using (new EditorGUI.DisabledScope(textColorSource != TextColorSource.Custom))
                        {
                            customTextColor = EditorGUILayout.ColorField(GUIContent.none, customTextColor,
                                true, false, false, GUILayout.Width(ColorFieldWidth));
                        }
                    }

                    using (new EditorGUI.DisabledScope(textColorSource != TextColorSource.ObjectColor))
                    {
                        labelBrightness = EditorGUILayout.Slider(
                            new GUIContent(UiStrings.LabelLabelBrightness, UiStrings.TooltipLabelBrightness),
                            labelBrightness, AppearanceSettings.MinLabelBrightness, AppearanceSettings.MaxLabelBrightness);
                    }
                }

                // A style with its own text color re-draws the name whatever the display mode says, so the
                // fill option stays available on its own.
                labelFills = EditorGUILayout.Toggle(
                    new GUIContent(UiStrings.LabelLabelFill, UiStrings.TooltipLabelFill), labelFills);

                EditorGUILayout.Space(2f);

                iconPlacement = (IconPlacement)EditorGUILayout.EnumPopup(
                    new GUIContent(UiStrings.LabelIconPlacement, UiStrings.TooltipIconPlacement), iconPlacement);

                changed = EditorGUI.EndChangeCheck();

                // Folder and prefab icons are chosen in a popup, which reports back after this frame; each
                // pick is recorded as its own undo step by its callback.
                DrawIconSetting(s_FolderIconLabel, settings.FolderIcon, s_SetFolderIcon);
                DrawIconSetting(s_PrefabIconLabel, settings.PrefabIcon, s_SetPrefabIcon);

                EditorGUILayout.Space(2f);
                EditorGUI.BeginChangeCheck();

                selectedBehavior = (SelectedRowBehavior)EditorGUILayout.EnumPopup(
                    new GUIContent(UiStrings.LabelSelectedBehavior, UiStrings.TooltipSelectedBehavior),
                    selectedBehavior);
                hoverBehavior = (HoverBehavior)EditorGUILayout.EnumPopup(
                    new GUIContent(UiStrings.LabelHoverBehavior, UiStrings.TooltipHoverBehavior), hoverBehavior);
                applyScope = (ApplyScope)EditorGUILayout.EnumPopup(
                    new GUIContent(UiStrings.LabelApplyScope, UiStrings.TooltipApplyScope), applyScope);

                // The foldout itself is deliberately outside the change check: expanding a section is not
                // a data change and must not create an undo step.
                changed |= EditorGUI.EndChangeCheck();

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
                settings.Mode = mode;
                settings.ZebraOpacity = zebraOpacity;
                settings.IconPlacement = iconPlacement;
                settings.Decorations = decorations;
                settings.MarkerShape = markerShape;
                settings.MarkerPlacement = markerPlacement;
                settings.MarkerSize = markerSize;
                settings.TintOpacity = tintOpacity;
                settings.TintScope = tintScope;
                settings.LabelBrightness = labelBrightness;
                settings.TextColorSource = textColorSource;
                settings.CustomTextColor = customTextColor;
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

        private static int ModeIndex(HierarchyMode mode)
        {
            for (int i = 0; i < s_ModeValues.Length; i++)
            {
                if (s_ModeValues[i] == mode)
                    return i;
            }

            return 0;
        }

        private static string ModeHint(HierarchyMode mode)
        {
            switch (mode)
            {
                case HierarchyMode.Zebra: return UiStrings.HintModeZebra;
                case HierarchyMode.Minimal: return UiStrings.HintModeMinimal;
                case HierarchyMode.Minimal | HierarchyMode.Zebra: return UiStrings.HintModeMinimalZebra;
                default: return UiStrings.HintModeDefault;
            }
        }

        private static void DrawIconSetting(GUIContent label, string icon, System.Action<string> onPicked)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);

                bool hasIcon = !string.IsNullOrEmpty(icon);
                var texture = hasIcon ? BuiltinIcons.GetTexture(icon) : null;
                s_IconValue.image = texture;
                s_IconValue.text = hasIcon
                    ? BuiltinIcons.GetLabel(icon) + (texture == null ? UiStrings.SuffixIconMissing : string.Empty)
                    : UiStrings.OptionUnityIcon;
                s_IconValue.tooltip = hasIcon ? icon : string.Empty;
                EditorGUILayout.LabelField(s_IconValue, GUILayout.MinWidth(40f), GUILayout.Height(EditorGUIUtility.singleLineHeight));

                var buttonRect = GUILayoutUtility.GetRect(s_ChooseContent, EditorStyles.miniButton, GUILayout.Width(IconButtonWidth));
                if (UnityEngine.GUI.Button(buttonRect, s_ChooseContent, EditorStyles.miniButton))
                    IconPickerPopup.Show(buttonRect, icon, onPicked);

                using (new EditorGUI.DisabledScope(!hasIcon))
                {
                    if (GUILayout.Button(s_RemoveIconContent, EditorStyles.miniButton, GUILayout.Width(ClearButtonWidth)))
                        onPicked(string.Empty);
                }
            }
        }

        private static void SetFolderIcon(string icon)
        {
            var store = HierarchyColorService.Store;
            if (store.Appearance.FolderIcon == BuiltinIcons.NormalizeName(icon))
                return;

            UndoScope.Record(store, UiStrings.UndoEditAppearance);
            store.Appearance.FolderIcon = icon;
            HierarchyColorService.NotifySettingsChanged();
        }

        private static void SetPrefabIcon(string icon)
        {
            var store = HierarchyColorService.Store;
            if (store.Appearance.PrefabIcon == BuiltinIcons.NormalizeName(icon))
                return;

            UndoScope.Record(store, UiStrings.UndoEditAppearance);
            store.Appearance.PrefabIcon = icon;
            HierarchyColorService.NotifySettingsChanged();
        }
    }
}
