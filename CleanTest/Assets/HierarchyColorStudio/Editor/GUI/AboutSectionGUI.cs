using UnityEditor;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Draws the product version, the author's details and the documentation shortcut. Shared by the
    /// Color Studio window and the Project Settings page so both surfaces stay identical.
    /// </summary>
    /// <remarks>
    /// The website and the support address are selectable text rather than buttons that call
    /// <c>Application.OpenURL</c>. Opening a browser or a mail client would make the plugin start an
    /// external process, which the license and the documentation both state that it never does.
    /// Selectable text lets the reader copy either value without giving up that guarantee.
    /// </remarks>
    internal static class AboutSectionGUI
    {
        /// <summary>Draws the section's contents.</summary>
        internal static void Draw()
        {
            EditorGUILayout.LabelField(UiStrings.AboutProduct, StudioStyles.SectionHeader);
            DrawSelectableRow(UiStrings.LabelWebsite, UiStrings.AuthorWebsite);
            DrawSelectableRow(UiStrings.LabelSupport, UiStrings.SupportEmail);

            StudioStyles.DrawSeparator();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(UiStrings.ButtonDocumentation, GUILayout.Width(160f)))
                    DocumentationLocator.Open();

                GUILayout.FlexibleSpace();
            }
        }

        private static void DrawSelectableRow(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth));
                EditorGUILayout.SelectableLabel(value, EditorStyles.label,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
        }
    }
}
