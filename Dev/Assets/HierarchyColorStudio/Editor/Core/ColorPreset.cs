using System;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// A named, reusable style that can be applied to Hierarchy rows. A preset stores the complete visual
    /// configuration — main color, background, text color, marker and icon — and applying it gives the
    /// target exactly that look.
    /// </summary>
    /// <remarks>
    /// Presets written by version 1.0 hold a color only. Their style elements are at the default, which
    /// follows the display mode, so they keep looking the way they always did.
    /// </remarks>
    [Serializable]
    public sealed class ColorPreset
    {
        [SerializeField] private string m_Id;
        [SerializeField] private string m_Name;
        [SerializeField] private string m_Color;
        [SerializeField] private StyleElements m_Elements;

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
            : this(null, name, HierarchyStyle.FromColor(color))
        {
        }

        /// <summary>Creates a preset with the supplied name and style.</summary>
        /// <param name="name">Display name shown in the UI.</param>
        /// <param name="style">Style applied by the preset.</param>
        public ColorPreset(string name, HierarchyStyle style)
            : this(null, name, style)
        {
        }

        /// <summary>Creates a preset that keeps an existing identifier, used when importing a color set.</summary>
        /// <param name="id">Identifier to preserve. A new one is generated when this is empty.</param>
        /// <param name="name">Display name shown in the UI.</param>
        /// <param name="color">Color applied by the preset.</param>
        internal ColorPreset(string id, string name, Color32 color)
            : this(id, name, HierarchyStyle.FromColor(color))
        {
        }

        /// <summary>Creates a preset that keeps an existing identifier, used when importing a color set.</summary>
        /// <param name="id">Identifier to preserve. A new one is generated when this is empty.</param>
        /// <param name="name">Display name shown in the UI.</param>
        /// <param name="style">Style applied by the preset.</param>
        internal ColorPreset(string id, string name, HierarchyStyle style)
        {
            m_Id = string.IsNullOrEmpty(id) ? NewId() : id;
            m_Name = name ?? string.Empty;
            Style = style;
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

        /// <summary>
        /// Main color applied by this preset. White when the preset has no main color — see
        /// <see cref="HierarchyStyle.HasColor"/> on <see cref="Style"/>. Setting it gives the preset a main
        /// color and leaves the other elements unchanged.
        /// </summary>
        public Color32 Color
        {
            get => ColorHex.TryParse(m_Color, out var parsed) ? parsed : (Color32)UnityEngine.Color.white;
            set => m_Color = ColorHex.ToHex(value, true);
        }

        /// <summary>The complete style applied by this preset.</summary>
        public HierarchyStyle Style
        {
            get => m_Elements.ToStyle(m_Color);
            set
            {
                var style = value.Normalized();
                m_Color = style.HasColor ? ColorHex.ToHex(style.Color, true) : string.Empty;
                m_Elements = StyleElements.FromStyle(style);
            }
        }

        /// <summary>Raw stored hexadecimal value, kept for diff-friendly serialization. Empty when the preset has no main color.</summary>
        public string Hex => m_Color;

        /// <summary>Creates an independent copy of this preset, including its identifier.</summary>
        public ColorPreset Clone()
        {
            return new ColorPreset { m_Id = Id, m_Name = Name, m_Color = m_Color, m_Elements = m_Elements };
        }

        /// <summary>Repairs missing or invalid fields so a hand-edited file can never break the UI.</summary>
        /// <remarks>
        /// An empty color means "no main color", which a preset made of an icon or a text color alone
        /// legitimately has. A color that is present but unparsable is replaced with white, as it always was.
        /// </remarks>
        internal void Sanitize()
        {
            if (string.IsNullOrEmpty(m_Id))
                m_Id = NewId();
            if (m_Name == null)
                m_Name = string.Empty;

            if (string.IsNullOrEmpty(m_Color))
                m_Color = string.Empty;
            else if (!ColorHex.TryParse(m_Color, out var parsed))
                m_Color = ColorHex.ToHex(UnityEngine.Color.white, true);
            else
                m_Color = ColorHex.ToHex(parsed, true);

            m_Elements.Repair();
        }

        /// <summary>
        /// Restores schema 1 meaning: a color is mandatory and there are no style elements. Called when a
        /// schema 1 file is upgraded, before the schema 2 rules apply.
        /// </summary>
        internal void ResetElementsAddedInVersion2()
        {
            if (!ColorHex.TryParse(m_Color, out _))
                m_Color = ColorHex.ToHex(UnityEngine.Color.white, true);
            m_Elements = default;
        }

        private static string NewId()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
