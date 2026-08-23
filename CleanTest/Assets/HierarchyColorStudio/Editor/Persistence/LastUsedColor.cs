using UnityEditor;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Remembers the most recently applied color and apply scope.
    /// This is per-user convenience state rather than project data, so it lives in
    /// <see cref="EditorPrefs"/> and is intentionally not part of the versioned project file.
    /// </summary>
    internal static class LastUsedColor
    {
        private const string ColorKey = "CryNet.HierarchyColorStudio.LastColor";
        private const string PresetKey = "CryNet.HierarchyColorStudio.LastPreset";
        private const string ScopeKey = "CryNet.HierarchyColorStudio.ApplyScope";
        private const string DefaultHex = "3498DBFF";

        /// <summary>The most recently applied color.</summary>
        internal static Color32 Color
        {
            get => ColorHex.TryParse(EditorPrefs.GetString(ColorKey, DefaultHex), out var parsed)
                ? parsed
                : (Color32)new Color(0.204f, 0.596f, 0.859f);
            private set => EditorPrefs.SetString(ColorKey, ColorHex.ToHex(value, true));
        }

        /// <summary>Identifier of the preset the last color came from, or an empty string.</summary>
        internal static string PresetId
        {
            get => EditorPrefs.GetString(PresetKey, string.Empty);
            private set => EditorPrefs.SetString(PresetKey, value ?? string.Empty);
        }

        /// <summary>Apply scope currently selected in the color palette.</summary>
        internal static ApplyScope Scope
        {
            get
            {
                int stored = EditorPrefs.GetInt(ScopeKey, (int)ApplyScope.SelectionOnly);
                return stored >= (int)ApplyScope.SelectionOnly && stored <= (int)ApplyScope.AllDescendants
                    ? (ApplyScope)stored
                    : ApplyScope.SelectionOnly;
            }
            set => EditorPrefs.SetInt(ScopeKey, (int)value);
        }

        /// <summary>Records the color that was just applied.</summary>
        /// <param name="color">Applied color.</param>
        /// <param name="presetId">Identifier of the source preset, or an empty string.</param>
        internal static void Set(Color32 color, string presetId)
        {
            Color = color;
            PresetId = presetId;
        }
    }
}
