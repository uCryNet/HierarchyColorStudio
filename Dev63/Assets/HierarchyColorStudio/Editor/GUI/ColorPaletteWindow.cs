using UnityEditor;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Compact color palette shown as a dropdown next to the mouse pointer.
    /// </summary>
    /// <remarks>
    /// The palette is self-contained on purpose. A dropdown closes as soon as it loses focus, so opening
    /// Unity's modal color picker from here would discard the user's choice. Custom colors are therefore
    /// entered with channel sliders and a validated hexadecimal field, both of which keep keyboard focus
    /// inside the dropdown.
    /// </remarks>
    internal sealed class ColorPaletteWindow : EditorWindow
    {
        private const float WindowWidth = 288f;
        private const float SwatchSize = 30f;
        private const float SwatchSpacing = 3f;
        private const int SwatchesPerRow = 8;
        private const float PaddingHorizontal = 8f;
        private const float PaddingVertical = 6f;
        private const float MaxWindowHeight = 460f;

        private GameObject[] m_Targets;
        private Color m_CustomColor = Color.white;
        private string m_HexInput = string.Empty;
        private bool m_HexValid = true;
        private bool m_CustomExpanded;
        private Vector2 m_ScrollPosition;

        /// <summary>Opens the palette as a dropdown at a screen position.</summary>
        /// <param name="screenPosition">Anchor position in screen space.</param>
        /// <param name="targets">GameObjects the palette will act on.</param>
        internal static void ShowAt(Vector2 screenPosition, GameObject[] targets)
        {
            if (targets == null || targets.Length == 0)
                return;

            var window = CreateInstance<ColorPaletteWindow>();
            window.m_Targets = targets;
            window.m_CustomColor = LastUsedColor.Color;
            window.m_HexInput = ColorHex.ToDisplayHex(LastUsedColor.Color);
            window.ShowAsDropDown(new Rect(screenPosition, Vector2.zero), window.CalculateSize());
        }

        private Vector2 CalculateSize()
        {
            var store = HierarchyColorService.Store;
            int presetCount = Mathf.Max(1, store.Presets.Count);
            int rows = Mathf.CeilToInt(presetCount / (float)SwatchesPerRow);
            float height = PaddingVertical * 2f
                           + EditorGUIUtility.singleLineHeight * 2f + 8f
                           + rows * (SwatchSize + SwatchSpacing)
                           + EditorGUIUtility.singleLineHeight * 3f + 24f;
            return new Vector2(WindowWidth, Mathf.Min(height, MaxWindowHeight));
        }

        private void OnGUI()
        {
            var store = HierarchyColorService.Store;
            if (store == null)
            {
                Close();
                return;
            }

            int targetCount = CountLiveTargets();
            if (targetCount == 0)
            {
                Close();
                return;
            }

            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandHeight(true)))
            {
                GUILayout.Space(PaddingVertical);
                DrawHeader(targetCount);
                DrawScopeSelector();
                StudioStyles.DrawSeparator(2f, 4f);

                using (var scroll = new EditorGUILayout.ScrollViewScope(m_ScrollPosition))
                {
                    m_ScrollPosition = scroll.scrollPosition;
                    DrawPresetGrid(store);
                    DrawCustomSection(store);
                }

                StudioStyles.DrawSeparator(2f, 4f);
                DrawFooter();
                GUILayout.Space(PaddingVertical);
            }
        }

        private void DrawHeader(int targetCount)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(PaddingHorizontal);
                GUILayout.Label(UiStrings.SelectionHeader(targetCount), EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
            }
        }

        private void DrawScopeSelector()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(PaddingHorizontal);
                GUILayout.Label(UiStrings.LabelApplyScopePopup, GUILayout.Width(56f));
                var scope = (ApplyScope)EditorGUILayout.EnumPopup(LastUsedColor.Scope);
                if (scope != LastUsedColor.Scope)
                    LastUsedColor.Scope = scope;
                GUILayout.Space(PaddingHorizontal);
            }
        }

        private void DrawPresetGrid(HierarchyColorStore store)
        {
            var presets = store.Presets;
            if (presets.Count == 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(PaddingHorizontal);
                    GUILayout.Label(UiStrings.HintNoPresets, StudioStyles.Hint);
                }

                return;
            }

            int column = 0;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(PaddingHorizontal);

            for (int i = 0; i < presets.Count; i++)
            {
                var preset = presets[i];
                if (preset == null)
                    continue;

                if (column == SwatchesPerRow)
                {
                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(SwatchSpacing);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(PaddingHorizontal);
                    column = 0;
                }

                var rect = GUILayoutUtility.GetRect(SwatchSize, SwatchSize,
                    GUILayout.Width(SwatchSize), GUILayout.Height(SwatchSize));
                DrawSwatchButton(rect, preset);
                GUILayout.Space(SwatchSpacing);
                column++;
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4f);
        }

        private void DrawSwatchButton(Rect rect, ColorPreset preset)
        {
            StudioStyles.DrawSwatch(rect, preset.Color);
            if (UnityEngine.GUI.Button(rect, new GUIContent(string.Empty,
                    preset.Name + "  " + ColorHex.ToDisplayHex(preset.Color)), GUIStyle.none))
            {
                ApplyColor(preset.Color, preset.Id);
            }
        }

        private void DrawCustomSection(HierarchyColorStore store)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(PaddingHorizontal);
                m_CustomExpanded = EditorGUILayout.Foldout(m_CustomExpanded, UiStrings.LabelCustomColor, true);
            }

            if (!m_CustomExpanded)
                return;

            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(PaddingHorizontal + 12f);
                    var previewRect = GUILayoutUtility.GetRect(SwatchSize, SwatchSize,
                        GUILayout.Width(SwatchSize), GUILayout.Height(SwatchSize));
                    StudioStyles.DrawSwatch(previewRect, m_CustomColor);

                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        string typed = EditorGUILayout.TextField(m_HexInput);
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_HexInput = typed;
                            m_HexValid = ColorHex.TryParse(typed, out Color32 parsed);
                            if (m_HexValid)
                                m_CustomColor = parsed;
                        }

                        if (!m_HexValid)
                            GUILayout.Label(UiStrings.HintInvalidHex, StudioStyles.Hint);
                    }

                    GUILayout.Space(PaddingHorizontal);
                }

                DrawChannelSlider("R", ref m_CustomColor.r);
                DrawChannelSlider("G", ref m_CustomColor.g);
                DrawChannelSlider("B", ref m_CustomColor.b);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(PaddingHorizontal + 12f);
                    if (GUILayout.Button(UiStrings.ButtonApply, GUILayout.Width(70f)))
                        ApplyColor(m_CustomColor, null);

                    if (GUILayout.Button(UiStrings.ButtonSaveAsPreset, EditorStyles.miniButton))
                        SaveCustomAsPreset(store);

                    GUILayout.Space(PaddingHorizontal);
                }
            }
        }

        private void DrawChannelSlider(string label, ref float channel)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(PaddingHorizontal + 12f);
                GUILayout.Label(label, GUILayout.Width(14f));
                EditorGUI.BeginChangeCheck();
                float value = GUILayout.HorizontalSlider(channel, 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                {
                    channel = value;
                    m_HexInput = ColorHex.ToDisplayHex(m_CustomColor);
                    m_HexValid = true;
                }

                GUILayout.Space(PaddingHorizontal);
            }
        }

        private void DrawFooter()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(PaddingHorizontal);
                using (new EditorGUI.DisabledScope(!HierarchyColorService.AnyHasColor(m_Targets)))
                {
                    if (GUILayout.Button(UiStrings.ButtonClear, GUILayout.Width(70f)))
                    {
                        HierarchyColorService.Clear(m_Targets, LastUsedColor.Scope);
                        Close();
                        GUIUtility.ExitGUI();
                    }
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(UiStrings.ButtonOpenStudio, EditorStyles.miniButton))
                {
                    Close();
                    HierarchyColorStudioWindow.Open();
                    GUIUtility.ExitGUI();
                }

                GUILayout.Space(PaddingHorizontal);
            }
        }

        private void ApplyColor(Color color, string presetId)
        {
            HierarchyColorService.Assign(m_Targets, color, presetId, LastUsedColor.Scope);
            Close();
            GUIUtility.ExitGUI();
        }

        private void SaveCustomAsPreset(HierarchyColorStore store)
        {
            UndoScope.Record(store, UiStrings.UndoEditPresets);
            store.Presets.Add(new ColorPreset(ColorHex.ToDisplayHex(m_CustomColor), m_CustomColor));
            HierarchyColorService.NotifySettingsChanged();
        }

        private int CountLiveTargets()
        {
            if (m_Targets == null)
                return 0;

            int count = 0;
            for (int i = 0; i < m_Targets.Length; i++)
            {
                if (m_Targets[i] != null)
                    count++;
            }

            return count;
        }
    }
}
