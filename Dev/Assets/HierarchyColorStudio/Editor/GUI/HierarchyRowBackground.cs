using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// The opaque color Unity paints behind a Hierarchy row, needed wherever the plugin must cover
    /// something Unity has already drawn — the row icon it replaces or hides, or the name it re-draws.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unity does not expose these colors, so the selected and hovered values are the Editor theme's
    /// constants: the hover colors are the theme's <c>-unity-object-tree-hovered-color</c>, verified in
    /// Unity 6000.3 and 6000.5, and the selection colors are the theme's highlight colors. The plain row
    /// color is the user's <see cref="AppearanceSettings.RowBackgroundDark"/> /
    /// <see cref="AppearanceSettings.RowBackgroundLight"/>, which can be corrected under <c>Advanced</c>
    /// if a future theme changes it.
    /// </para>
    /// <para>
    /// Unity paints a selected row blue only while the tree has keyboard focus: the Editor is the active
    /// application, the Hierarchy is the focused window, and no text field in it is being edited.
    /// Otherwise the selection is grey. The same conditions are evaluated here with public API only; the
    /// window is recognised by its type name rather than by reflecting on Unity's internal class.
    /// </para>
    /// </remarks>
    internal static class HierarchyRowBackground
    {
        private const string HierarchyWindowTypeName = "SceneHierarchyWindow";

        private static readonly Color s_SelectedDark = new Color32(0x2C, 0x5D, 0x87, 0xFF);
        private static readonly Color s_SelectedLight = new Color32(0x3A, 0x72, 0xB0, 0xFF);
        private static readonly Color s_SelectedUnfocusedDark = new Color32(0x4D, 0x4D, 0x4D, 0xFF);
        private static readonly Color s_SelectedUnfocusedLight = new Color32(0xAE, 0xAE, 0xAE, 0xFF);
        private static readonly Color s_HoverDark = new Color32(0x44, 0x44, 0x44, 0xFF);
        private static readonly Color s_HoverLight = new Color32(0xB2, 0xB2, 0xB2, 0xFF);

        private static EditorWindow s_LastFocusedWindow;
        private static bool s_HierarchyFocused;

        /// <summary>Returns the color Unity paints behind a row in its current state.</summary>
        /// <param name="settings">Appearance settings supplying the plain row color.</param>
        /// <param name="selected"><c>true</c> when the row is part of the selection.</param>
        /// <param name="hovered"><c>true</c> when the mouse is over the row.</param>
        internal static Color Get(AppearanceSettings settings, bool selected, bool hovered)
        {
            bool proSkin = EditorGUIUtility.isProSkin;
            if (selected)
            {
                if (IsHierarchyFocused())
                    return proSkin ? s_SelectedDark : s_SelectedLight;
                return proSkin ? s_SelectedUnfocusedDark : s_SelectedUnfocusedLight;
            }

            if (hovered)
                return proSkin ? s_HoverDark : s_HoverLight;

            return settings.CurrentRowBackground(proSkin);
        }

        private static bool IsHierarchyFocused()
        {
            if (!InternalEditorUtility.isApplicationActive || GUIUtility.keyboardControl == 0 ||
                EditorGUIUtility.editingTextField)
                return false;

            // The focused window changes rarely; its type is only inspected when it does.
            var focused = EditorWindow.focusedWindow;
            if (!ReferenceEquals(focused, s_LastFocusedWindow))
            {
                s_LastFocusedWindow = focused;
                s_HierarchyFocused = focused != null && focused.GetType().Name == HierarchyWindowTypeName;
            }

            return s_HierarchyFocused;
        }
    }
}
