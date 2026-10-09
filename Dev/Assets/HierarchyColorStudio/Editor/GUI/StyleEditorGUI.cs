using System;
using UnityEditor;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Draws one row per style element — color, background, text, marker and icon — so the elements read as
    /// independent dimensions. Shared by the Selection section and the preset editor.
    /// </summary>
    /// <remarks>
    /// The drawer only reports what changed. The caller applies the change, so the selection goes through
    /// <see cref="HierarchyColorService.ModifyStyle"/> and a preset through its own undo step. Every
    /// "Default" option names what the project's display mode currently makes of it, so the effect of a
    /// choice is visible before it is made.
    /// </remarks>
    internal static class StyleEditorGUI
    {
        private const float ToggleWidth = 16f;
        private const float IconButtonWidth = 64f;
        private const float ClearButtonWidth = 22f;
        private const int FirstShapeIndex = 2;

        private static readonly GUIContent s_ColorLabel = new GUIContent(UiStrings.LabelStyleColor, UiStrings.TooltipStyleColor);
        private static readonly GUIContent s_BackgroundLabel = new GUIContent(UiStrings.LabelStyleBackground, UiStrings.TooltipStyleBackground);
        private static readonly GUIContent s_TextLabel = new GUIContent(UiStrings.LabelStyleText, UiStrings.TooltipStyleText);
        private static readonly GUIContent s_MarkerLabel = new GUIContent(UiStrings.LabelStyleMarker, UiStrings.TooltipStyleMarker);
        private static readonly GUIContent s_IconLabel = new GUIContent(UiStrings.LabelStyleIcon, UiStrings.TooltipStyleIcon);
        private static readonly GUIContent s_IconContent = new GUIContent();
        private static readonly GUIContent s_ChooseContent = new GUIContent(UiStrings.ButtonChooseIcon, UiStrings.TooltipChooseIcon);
        private static readonly GUIContent s_RemoveIconContent = new GUIContent("×", UiStrings.TooltipRemoveIcon);
        private static readonly GUIContent[] s_BackgroundOptions = new GUIContent[3];
        private static readonly GUIContent[] s_TextOptions = new GUIContent[3];
        private static GUIContent[] s_MarkerOptions;

        /// <summary>The outcome of one <see cref="Draw"/> call.</summary>
        internal struct Result
        {
            /// <summary>Elements the user changed. <see cref="StyleFields.None"/> when nothing changed.</summary>
            public StyleFields Changed;

            /// <summary>Style holding the new values of the changed elements.</summary>
            public HierarchyStyle Values;

            /// <summary><c>true</c> when the change comes from dragging a color, so undo steps may collapse.</summary>
            public bool Continuous;
        }

        /// <summary>Draws the style rows.</summary>
        /// <param name="style">Style to show. For a multi-selection, the style of its first object.</param>
        /// <param name="mixed">Elements whose values differ across the edited objects.</param>
        /// <param name="settings">Project appearance settings, used to describe the defaults.</param>
        /// <param name="onIconPicked">Receives an icon chosen in the picker popup, which closes after this call returns.</param>
        internal static Result Draw(HierarchyStyle style, StyleFields mixed, AppearanceSettings settings,
            Action<string> onIconPicked)
        {
            var result = new Result { Values = style };
            DrawColorRow(style, mixed, ref result);
            DrawBackgroundRow(style, mixed, settings, ref result);
            DrawTextRow(style, mixed, settings, ref result);
            DrawMarkerRow(style, mixed, settings, ref result);
            DrawIconRow(style, mixed, onIconPicked, ref result);
            EditorGUI.showMixedValue = false;
            return result;
        }

        private static void DrawColorRow(HierarchyStyle style, StyleFields mixed, ref Result result)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(s_ColorLabel);
                EditorGUI.showMixedValue = (mixed & StyleFields.Color) != 0;

                EditorGUI.BeginChangeCheck();
                bool hasColor = EditorGUILayout.Toggle(style.HasColor, GUILayout.Width(ToggleWidth));
                if (EditorGUI.EndChangeCheck())
                {
                    result.Changed |= StyleFields.Color;
                    result.Values.HasColor = hasColor;
                    result.Values.Color = style.HasColor ? style.Color : LastUsedColor.Color;
                }

                using (new EditorGUI.DisabledScope(!style.HasColor && !hasColor))
                {
                    EditorGUI.BeginChangeCheck();
                    Color picked = EditorGUILayout.ColorField(GUIContent.none,
                        style.HasColor ? (Color)style.Color : (Color)LastUsedColor.Color, true, false, false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        result.Changed |= StyleFields.Color;
                        result.Values.HasColor = true;
                        result.Values.Color = picked;
                        result.Continuous = true;
                    }
                }
            }
        }

        private static void DrawBackgroundRow(HierarchyStyle style, StyleFields mixed, AppearanceSettings settings,
            ref Result result)
        {
            bool byDefault = (settings.Decorations & HierarchyDecorations.RowTint) != 0;
            SetOptions(s_BackgroundOptions, byDefault ? UiStrings.OptionDefaultShown : UiStrings.OptionDefaultHidden,
                UiStrings.OptionHide, UiStrings.OptionShow);

            EditorGUI.showMixedValue = (mixed & StyleFields.Background) != 0;
            EditorGUI.BeginChangeCheck();
            int index = EditorGUILayout.Popup(s_BackgroundLabel, (int)style.Background, s_BackgroundOptions);
            if (EditorGUI.EndChangeCheck())
            {
                result.Changed |= StyleFields.Background;
                result.Values.Background = (StyleOverride)index;
            }
        }

        private static void DrawTextRow(HierarchyStyle style, StyleFields mixed, AppearanceSettings settings,
            ref Result result)
        {
            bool colorsByDefault = (settings.Decorations & HierarchyDecorations.LabelColor) != 0;
            SetOptions(s_TextOptions, colorsByDefault ? UiStrings.OptionDefaultColored : UiStrings.OptionDefaultUnity,
                UiStrings.OptionUnityText, UiStrings.OptionCustom);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.showMixedValue = (mixed & StyleFields.Text) != 0;
                EditorGUI.BeginChangeCheck();
                int index = EditorGUILayout.Popup(s_TextLabel, (int)style.Text, s_TextOptions);
                if (EditorGUI.EndChangeCheck())
                {
                    result.Changed |= StyleFields.Text;
                    result.Values.Text = (StyleOverride)index;
                    result.Values.TextColor = style.HasTextColor ? style.TextColor : EffectiveTextColor(style, settings);
                }

                // The field shows the color the name is drawn in even when it is not editable, so the
                // effect of "Default" and "Unity Color" is visible too.
                using (new EditorGUI.DisabledScope(style.Text != StyleOverride.On))
                {
                    EditorGUI.BeginChangeCheck();
                    Color picked = EditorGUILayout.ColorField(GUIContent.none, EffectiveTextColor(style, settings),
                        true, false, false, GUILayout.Width(60f));
                    if (EditorGUI.EndChangeCheck())
                    {
                        result.Changed |= StyleFields.Text;
                        result.Values.Text = StyleOverride.On;
                        result.Values.TextColor = picked;
                        result.Continuous = true;
                    }
                }
            }
        }

        private static void DrawMarkerRow(HierarchyStyle style, StyleFields mixed, AppearanceSettings settings,
            ref Result result)
        {
            EnsureMarkerOptions();
            bool byDefault = (settings.Decorations & HierarchyDecorations.Marker) != 0;
            s_MarkerOptions[0].text = byDefault
                ? UiStrings.OptionDefaultPrefix + ObjectNames.NicifyVariableName(settings.MarkerShape.ToString()) + ")"
                : UiStrings.OptionDefaultHidden;

            int index = style.Marker == StyleOverride.On ? FirstShapeIndex + (int)style.MarkerShape : (int)style.Marker;

            EditorGUI.showMixedValue = (mixed & StyleFields.Marker) != 0;
            EditorGUI.BeginChangeCheck();
            index = EditorGUILayout.Popup(s_MarkerLabel, index, s_MarkerOptions);
            if (EditorGUI.EndChangeCheck())
            {
                result.Changed |= StyleFields.Marker;
                if (index >= FirstShapeIndex)
                {
                    result.Values.Marker = StyleOverride.On;
                    result.Values.MarkerShape = (MarkerShape)(index - FirstShapeIndex);
                }
                else
                {
                    result.Values.Marker = (StyleOverride)index;
                    result.Values.MarkerShape = MarkerShape.Dot;
                }
            }
        }

        private static void DrawIconRow(HierarchyStyle style, StyleFields mixed, Action<string> onIconPicked,
            ref Result result)
        {
            bool isMixed = (mixed & StyleFields.Icon) != 0;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(s_IconLabel);

                var icon = isMixed || !style.HasIcon ? null : BuiltinIcons.GetTexture(style.Icon);
                s_IconContent.image = icon;
                s_IconContent.text = isMixed ? "—" : style.HasIcon ? BuiltinIcons.GetLabel(style.Icon) : UiStrings.OptionNone;
                s_IconContent.tooltip = style.HasIcon && !isMixed ? style.Icon : string.Empty;
                if (style.HasIcon && !isMixed && icon == null)
                    s_IconContent.text += UiStrings.SuffixIconMissing;

                EditorGUILayout.LabelField(s_IconContent, GUILayout.MinWidth(40f), GUILayout.Height(EditorGUIUtility.singleLineHeight));

                var buttonRect = GUILayoutUtility.GetRect(s_ChooseContent, EditorStyles.miniButton,
                    GUILayout.Width(IconButtonWidth));
                if (UnityEngine.GUI.Button(buttonRect, s_ChooseContent, EditorStyles.miniButton))
                    IconPickerPopup.Show(buttonRect, isMixed ? string.Empty : style.Icon, onIconPicked);

                using (new EditorGUI.DisabledScope(!style.HasIcon && !isMixed))
                {
                    if (GUILayout.Button(s_RemoveIconContent, EditorStyles.miniButton, GUILayout.Width(ClearButtonWidth)))
                    {
                        result.Changed |= StyleFields.Icon;
                        result.Values.Icon = string.Empty;
                    }
                }
            }
        }

        /// <summary>The color a row's name is drawn in with this style, for display in the text color field.</summary>
        private static Color EffectiveTextColor(HierarchyStyle style, AppearanceSettings settings)
        {
            var plan = DecorationPlan.Resolve(style.Normalized(), settings, false);
            return plan.Text ? plan.TextColor : EditorStyles.label.normal.textColor;
        }

        private static void SetOptions(GUIContent[] options, string first, string second, string third)
        {
            if (options[0] == null)
            {
                for (int i = 0; i < options.Length; i++)
                    options[i] = new GUIContent();
            }

            options[0].text = first;
            options[1].text = second;
            options[2].text = third;
        }

        private static void EnsureMarkerOptions()
        {
            if (s_MarkerOptions != null)
                return;

            var shapes = (MarkerShape[])Enum.GetValues(typeof(MarkerShape));
            s_MarkerOptions = new GUIContent[FirstShapeIndex + shapes.Length];
            s_MarkerOptions[0] = new GUIContent();
            s_MarkerOptions[1] = new GUIContent(UiStrings.OptionNone);
            for (int i = 0; i < shapes.Length; i++)
                s_MarkerOptions[FirstShapeIndex + (int)shapes[i]] = new GUIContent(ObjectNames.NicifyVariableName(shapes[i].ToString()));
        }
    }
}
