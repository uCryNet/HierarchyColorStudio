using System;
using UnityEditor;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Draws the grid of Unity built-in icons used by every icon picker: the popup opened from the Color
    /// Studio window and the Project Settings page, and the inline section of the color palette.
    /// </summary>
    internal static class IconPickerGUI
    {
        /// <summary>Edge length of one grid cell.</summary>
        internal const float CellSize = 24f;

        private const float CellSpacing = 2f;
        private const float IconSize = 16f;

        private static GUIContent[] s_Contents;
        private static GUIContent s_NoneGlyph;
        private static GUIContent s_NoneContent;
        private static GUIStyle s_NoneStyle;

        /// <summary>
        /// Draws a "None" cell followed by the catalog icons that match <paramref name="search"/>.
        /// </summary>
        /// <param name="current">Name of the icon currently in use, highlighted in the grid.</param>
        /// <param name="search">Text filter matched against icon labels and names. Empty shows everything.</param>
        /// <param name="width">Width available to the grid.</param>
        /// <param name="showCategories">Draws a heading above each group of icons.</param>
        /// <param name="picked">Receives the chosen name: an icon name, or an empty string for "None".</param>
        /// <returns><c>true</c> when a cell was clicked.</returns>
        internal static bool DrawGrid(string current, string search, float width, bool showCategories, out string picked)
        {
            picked = null;
            EnsureContents();

            int columns = Mathf.Max(1, Mathf.FloorToInt((width + CellSpacing) / (CellSize + CellSpacing)));
            string currentName = BuiltinIcons.NormalizeName(current);
            bool filtering = !string.IsNullOrEmpty(search);
            var catalog = BuiltinIcons.Catalog;

            int column = 0;
            bool rowOpen = false;
            bool clicked = false;

            if (!filtering)
            {
                BeginRow(ref rowOpen);
                if (DrawCell(s_NoneContent, null, currentName.Length == 0))
                {
                    picked = string.Empty;
                    clicked = true;
                }

                column++;
            }

            var category = (IconCategory)(-1);
            for (int i = 0; i < catalog.Count; i++)
            {
                var entry = catalog[i];
                if (filtering && !Matches(entry, search))
                    continue;

                if (showCategories && !filtering && entry.Category != category)
                {
                    category = entry.Category;
                    EndRow(ref rowOpen);
                    if (i > 0)
                        GUILayout.Space(2f);
                    EditorGUILayout.LabelField(CategoryLabel(category), EditorStyles.miniBoldLabel);
                    column = 0;
                }

                if (column == columns)
                {
                    EndRow(ref rowOpen);
                    GUILayout.Space(CellSpacing);
                    column = 0;
                }

                BeginRow(ref rowOpen);
                bool selected = string.Equals(entry.Name, currentName, StringComparison.OrdinalIgnoreCase);
                if (DrawCell(s_Contents[i], BuiltinIcons.GetTexture(entry.Name), selected))
                {
                    picked = entry.Name;
                    clicked = true;
                }

                column++;
            }

            EndRow(ref rowOpen);
            return clicked;
        }

        private static bool Matches(BuiltinIcon entry, string search)
        {
            return entry.Label.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   entry.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void BeginRow(ref bool rowOpen)
        {
            if (rowOpen)
                return;

            EditorGUILayout.BeginHorizontal();
            rowOpen = true;
        }

        private static void EndRow(ref bool rowOpen)
        {
            if (!rowOpen)
                return;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            rowOpen = false;
        }

        /// <summary>Draws one cell. A cell without an icon shows the "None" glyph.</summary>
        /// <param name="content">Empty-text content carrying the cell's tooltip.</param>
        /// <param name="icon">Icon to show, or <c>null</c> for the "None" cell.</param>
        /// <param name="selected">Highlights the cell when <c>true</c>.</param>
        private static bool DrawCell(GUIContent content, Texture icon, bool selected)
        {
            var rect = GUILayoutUtility.GetRect(CellSize, CellSize, GUILayout.Width(CellSize), GUILayout.Height(CellSize));
            GUILayout.Space(CellSpacing);

            if (Event.current.type == EventType.Repaint)
            {
                bool hovered = rect.Contains(Event.current.mousePosition);
                if (selected || hovered)
                    EditorCompat.DrawRoundedRect(rect, selected ? SelectionColor : HoverColor, 3f);

                if (icon != null)
                {
                    var iconRect = new Rect(rect.center.x - IconSize * 0.5f, rect.center.y - IconSize * 0.5f, IconSize, IconSize);
                    UnityEngine.GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
                }
                else
                {
                    EnsureNoneStyle().Draw(rect, s_NoneGlyph, false, false, false, false);
                }
            }

            return UnityEngine.GUI.Button(rect, content, GUIStyle.none);
        }

        private static Color SelectionColor =>
            EditorGUIUtility.isProSkin ? new Color(0.24f, 0.48f, 0.9f, 0.55f) : new Color(0.24f, 0.48f, 0.9f, 0.35f);

        private static Color HoverColor =>
            EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.08f) : new Color(0f, 0f, 0f, 0.08f);

        private static void EnsureContents()
        {
            if (s_Contents != null)
                return;

            var catalog = BuiltinIcons.Catalog;
            s_Contents = new GUIContent[catalog.Count];
            for (int i = 0; i < catalog.Count; i++)
                s_Contents[i] = new GUIContent(string.Empty, catalog[i].Label);

            s_NoneGlyph = new GUIContent("∅");
            s_NoneContent = new GUIContent(string.Empty, UiStrings.TooltipNoIcon);
        }

        private static GUIStyle EnsureNoneStyle()
        {
            return s_NoneStyle ?? (s_NoneStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13
            });
        }

        private static string CategoryLabel(IconCategory category)
        {
            switch (category)
            {
                case IconCategory.Status: return "Status";
                case IconCategory.Rendering: return "Rendering";
                case IconCategory.Audio: return "Audio";
                case IconCategory.Physics: return "Physics";
                case IconCategory.Animation: return "Animation";
                case IconCategory.UserInterface: return "UI";
                case IconCategory.Navigation: return "Navigation & 2D";
                case IconCategory.Scripting: return "Scripting & Media";
                default: return "General";
            }
        }
    }

    /// <summary>
    /// Popup listing Unity's built-in icons, plus a field for any other built-in icon name.
    /// </summary>
    internal sealed class IconPickerPopup : PopupWindowContent
    {
        private const float Width = 300f;
        private const float Height = 380f;
        private const float Padding = 6f;
        private const string DarkPrefix = "d_";

        private readonly string m_Current;
        private readonly Action<string> m_OnPicked;

        private string m_Search = string.Empty;
        private string m_CustomName;
        private bool m_CustomMissing;
        private Vector2 m_Scroll;

        private IconPickerPopup(string current, Action<string> onPicked)
        {
            m_Current = BuiltinIcons.NormalizeName(current);
            m_OnPicked = onPicked;
            m_CustomName = m_Current;
        }

        /// <summary>Opens the picker below a control.</summary>
        /// <param name="activatorRect">Rectangle of the control that opens the picker, in GUI space.</param>
        /// <param name="current">Icon currently in use, or an empty string.</param>
        /// <param name="onPicked">Receives the chosen icon name, or an empty string for "None".</param>
        internal static void Show(Rect activatorRect, string current, Action<string> onPicked)
        {
            PopupWindow.Show(activatorRect, new IconPickerPopup(current, onPicked));
        }

        /// <inheritdoc/>
        public override Vector2 GetWindowSize()
        {
            return new Vector2(Width, Height);
        }

        /// <inheritdoc/>
        public override void OnGUI(Rect rect)
        {
            GUILayout.Space(Padding);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(Padding);
                m_Search = EditorGUILayout.TextField(m_Search, EditorStyles.toolbarSearchField);
                GUILayout.Space(Padding);
            }

            GUILayout.Space(4f);
            using (var scroll = new EditorGUILayout.ScrollViewScope(m_Scroll))
            {
                m_Scroll = scroll.scrollPosition;
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(Padding);
                    using (new EditorGUILayout.VerticalScope())
                    {
                        float gridWidth = rect.width - Padding * 2f - 16f;
                        if (IconPickerGUI.DrawGrid(m_Current, m_Search, gridWidth, true, out string picked))
                            Pick(picked);
                    }
                }
            }

            StudioStyles.DrawSeparator(2f, 2f);
            DrawCustomName();
            GUILayout.Space(Padding);
        }

        private void DrawCustomName()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(Padding);
                EditorGUILayout.LabelField(new GUIContent(UiStrings.LabelIconName, UiStrings.TooltipIconName),
                    GUILayout.Width(74f));

                EditorGUI.BeginChangeCheck();
                m_CustomName = EditorGUILayout.TextField(m_CustomName);
                if (EditorGUI.EndChangeCheck())
                    m_CustomMissing = false;

                if (GUILayout.Button(UiStrings.ButtonUse, EditorStyles.miniButton, GUILayout.Width(44f)))
                    PickCustomName();
                GUILayout.Space(Padding);
            }

            if (m_CustomMissing)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(Padding);
                    EditorGUILayout.LabelField(UiStrings.HintIconMissing, StudioStyles.Hint);
                }
            }
        }

        private void PickCustomName()
        {
            string name = BuiltinIcons.NormalizeName(m_CustomName);
            if (name.Length == 0)
            {
                Pick(string.Empty);
                return;
            }

            // A dark-theme name is stored without its prefix when the plain icon exists, so the row follows
            // the Editor theme like every other icon.
            if (name.StartsWith(DarkPrefix, StringComparison.Ordinal) && BuiltinIcons.Exists(name.Substring(DarkPrefix.Length)))
                name = name.Substring(DarkPrefix.Length);

            if (!BuiltinIcons.Exists(name))
            {
                m_CustomMissing = true;
                return;
            }

            Pick(name);
        }

        private void Pick(string name)
        {
            m_OnPicked?.Invoke(name ?? string.Empty);
            editorWindow.Close();
            GUIUtility.ExitGUI();
        }
    }
}
