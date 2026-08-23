using System;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// A named, reusable color that can be applied to Hierarchy rows.
    /// </summary>
    [Serializable]
    public sealed class ColorPreset
    {
        [SerializeField] private string m_Id;
        [SerializeField] private string m_Name;
        [SerializeField] private string m_Color;

        /// <summary>Creates an empty preset. Required for Unity serialization.</summary>
        public ColorPreset()
        {
            m_Id = NewId();
            m_Name = string.Empty;
            m_Color = ColorHex.ToHex(UnityEngine.Color.white, true);
        }

        /// <summary>Creates a preset with the supplied name and color.</summary>
        /// <param name="name">Display name shown in the UI.</param>
        /// <param name="color">Color applied by the preset.</param>
        public ColorPreset(string name, Color32 color)
        {
            m_Id = NewId();
            m_Name = name ?? string.Empty;
            m_Color = ColorHex.ToHex(color, true);
        }

        /// <summary>Creates a preset that keeps an existing identifier, used when importing a color set.</summary>
        /// <param name="id">Identifier to preserve. A new one is generated when this is empty.</param>
        /// <param name="name">Display name shown in the UI.</param>
        /// <param name="color">Color applied by the preset.</param>
        internal ColorPreset(string id, string name, Color32 color)
        {
            m_Id = string.IsNullOrEmpty(id) ? NewId() : id;
            m_Name = name ?? string.Empty;
            m_Color = ColorHex.ToHex(color, true);
        }

        /// <summary>Stable identifier used by assignments to remember their source preset.</summary>
        public string Id
        {
            get
            {
                if (string.IsNullOrEmpty(m_Id))
                    m_Id = NewId();
                return m_Id;
            }
        }

        /// <summary>Display name shown in the UI.</summary>
        public string Name
        {
            get => m_Name ?? string.Empty;
            set => m_Name = value ?? string.Empty;
        }

        /// <summary>Color applied by this preset.</summary>
        public Color32 Color
        {
            get => ColorHex.TryParse(m_Color, out var parsed) ? parsed : (Color32)UnityEngine.Color.white;
            set => m_Color = ColorHex.ToHex(value, true);
        }

        /// <summary>Raw stored hexadecimal value, kept for diff-friendly serialization.</summary>
        public string Hex => m_Color;

        /// <summary>Creates an independent copy of this preset, including its identifier.</summary>
        public ColorPreset Clone()
        {
            return new ColorPreset { m_Id = Id, m_Name = Name, m_Color = m_Color };
        }

        /// <summary>Repairs missing or invalid fields so a hand-edited file can never break the UI.</summary>
        internal void Sanitize()
        {
            if (string.IsNullOrEmpty(m_Id))
                m_Id = NewId();
            if (m_Name == null)
                m_Name = string.Empty;
            if (!ColorHex.TryParse(m_Color, out var parsed))
                m_Color = ColorHex.ToHex(UnityEngine.Color.white, true);
            else
                m_Color = ColorHex.ToHex(parsed, true);
        }

        private static string NewId()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
