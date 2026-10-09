using System;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// A single persisted "this object has this style" record.
    /// The key is either a <see cref="UnityEditor.GlobalObjectId"/> string or a session-scoped
    /// instance key for objects that do not have a stable file identifier yet.
    /// </summary>
    /// <remarks>
    /// Schema 1 stored a key, a color and a preset id. Schema 2 added the remaining style elements in
    /// <see cref="StyleElements"/>, whose zero value reproduces schema 1 behaviour, so a record written by
    /// version 1.0 keeps its exact meaning.
    /// </remarks>
    [Serializable]
    public sealed class ColorAssignment
    {
        /// <summary>Prefix used by keys that are only valid for the current Editor session.</summary>
        public const string SessionKeyPrefix = "Session:";

        [SerializeField] private string m_Key;
        [SerializeField] private string m_Color;
        [SerializeField] private string m_PresetId;
        [SerializeField] private StyleElements m_Elements;

        /// <summary>Creates an empty assignment. Required for Unity serialization.</summary>
        public ColorAssignment()
        {
            m_Key = string.Empty;
            m_Color = string.Empty;
            m_PresetId = string.Empty;
        }

        /// <summary>Creates an assignment for the supplied identity key.</summary>
        /// <param name="key">Identity key of the target GameObject.</param>
        /// <param name="color">Assigned color.</param>
        /// <param name="presetId">Optional identifier of the preset the color came from.</param>
        public ColorAssignment(string key, Color32 color, string presetId)
            : this(key, HierarchyStyle.FromColor(color), presetId)
        {
        }

        /// <summary>Creates an assignment for the supplied identity key.</summary>
        /// <param name="key">Identity key of the target GameObject.</param>
        /// <param name="style">Assigned style.</param>
        /// <param name="presetId">Optional identifier of the preset the style came from.</param>
        public ColorAssignment(string key, HierarchyStyle style, string presetId)
        {
            m_Key = key ?? string.Empty;
            m_PresetId = presetId ?? string.Empty;
            Style = style;
        }

        /// <summary>Identity key of the target GameObject.</summary>
        public string Key
        {
            get => m_Key ?? string.Empty;
            internal set => m_Key = value ?? string.Empty;
        }

        /// <summary>Assigned main color, or magenta when the assignment has none — see <see cref="HasColor"/>.</summary>
        public Color32 Color
        {
            get => ColorHex.TryParse(m_Color, out var parsed) ? parsed : (Color32)UnityEngine.Color.magenta;
            internal set => m_Color = ColorHex.ToHex(value, true);
        }

        /// <summary><c>true</c> when the assignment has a main color. A style can also consist of an icon or a text color alone.</summary>
        public bool HasColor => ColorHex.TryParse(m_Color, out _);

        /// <summary>Identifier of the preset the style came from, or an empty string for custom styles.</summary>
        public string PresetId
        {
            get => m_PresetId ?? string.Empty;
            internal set => m_PresetId = value ?? string.Empty;
        }

        /// <summary>The complete style of the assignment.</summary>
        public HierarchyStyle Style
        {
            get => m_Elements.ToStyle(m_Color);
            internal set
            {
                var style = value.Normalized();
                m_Color = style.HasColor ? ColorHex.ToHex(style.Color, true) : string.Empty;
                m_Elements = StyleElements.FromStyle(style);
            }
        }

        /// <summary><c>true</c> when this key is only meaningful inside the current Editor session.</summary>
        public bool IsSessionScoped => Key.StartsWith(SessionKeyPrefix, StringComparison.Ordinal);

        /// <summary><c>true</c> when the record contains a usable key and a style that draws something.</summary>
        internal bool IsValid()
        {
            return !string.IsNullOrEmpty(m_Key) && !Style.IsEmpty;
        }

        /// <summary>
        /// Drops unusable style data: an unparsable color becomes "no color" and invalid elements fall back
        /// to their defaults. A record left with nothing to draw is then rejected by <see cref="IsValid"/>.
        /// </summary>
        /// <returns><c>true</c> when unusable data was dropped.</returns>
        internal bool RepairStyle()
        {
            bool repaired = m_Elements.Repair();
            if (!string.IsNullOrEmpty(m_Color) && !ColorHex.TryParse(m_Color, out _))
            {
                m_Color = string.Empty;
                repaired = true;
            }

            return repaired;
        }

        /// <summary>
        /// Puts every element added in schema 2 at its schema 1 meaning. Called when a schema 1 file is
        /// upgraded, so the result never depends on how Unity initializes fields absent from the file.
        /// </summary>
        internal void ResetElementsAddedInVersion2()
        {
            m_Elements = default;
        }

        /// <summary>Builds the session-scoped key for a live object identifier.</summary>
        /// <param name="rowId">Identifier of the object.</param>
        internal static string MakeSessionKey(RowId rowId)
        {
            return SessionKeyPrefix + rowId.ToPersistString();
        }

        /// <summary>Reads the object identifier back out of a session-scoped key.</summary>
        /// <param name="key">Key to read.</param>
        /// <param name="rowId">Receives the parsed identifier.</param>
        internal static bool TryReadSessionRowId(string key, out RowId rowId)
        {
            rowId = default;
            if (string.IsNullOrEmpty(key) || !key.StartsWith(SessionKeyPrefix, StringComparison.Ordinal))
                return false;

            return RowId.TryParsePersistString(key.Substring(SessionKeyPrefix.Length), out rowId);
        }
    }
}
