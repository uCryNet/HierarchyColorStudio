using System;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// A single persisted "this object has this color" record.
    /// The key is either a <see cref="UnityEditor.GlobalObjectId"/> string or a session-scoped
    /// instance key for objects that do not have a stable file identifier yet.
    /// </summary>
    [Serializable]
    public sealed class ColorAssignment
    {
        /// <summary>Prefix used by keys that are only valid for the current Editor session.</summary>
        public const string SessionKeyPrefix = "Session:";

        [SerializeField] private string m_Key;
        [SerializeField] private string m_Color;
        [SerializeField] private string m_PresetId;

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
        {
            m_Key = key ?? string.Empty;
            m_Color = ColorHex.ToHex(color, true);
            m_PresetId = presetId ?? string.Empty;
        }

        /// <summary>Identity key of the target GameObject.</summary>
        public string Key
        {
            get => m_Key ?? string.Empty;
            internal set => m_Key = value ?? string.Empty;
        }

        /// <summary>Assigned color.</summary>
        public Color32 Color
        {
            get => ColorHex.TryParse(m_Color, out var parsed) ? parsed : (Color32)UnityEngine.Color.magenta;
            internal set => m_Color = ColorHex.ToHex(value, true);
        }

        /// <summary>Identifier of the preset the color came from, or an empty string for custom colors.</summary>
        public string PresetId
        {
            get => m_PresetId ?? string.Empty;
            internal set => m_PresetId = value ?? string.Empty;
        }

        /// <summary><c>true</c> when this key is only meaningful inside the current Editor session.</summary>
        public bool IsSessionScoped => Key.StartsWith(SessionKeyPrefix, StringComparison.Ordinal);

        /// <summary><c>true</c> when the record contains a usable key and color.</summary>
        internal bool IsValid()
        {
            return !string.IsNullOrEmpty(m_Key) && ColorHex.TryParse(m_Color, out _);
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
