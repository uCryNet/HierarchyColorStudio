using System;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// The serialized form of every style element except the main color, shared by
    /// <see cref="ColorAssignment"/> and <see cref="ColorPreset"/>.
    /// </summary>
    /// <remarks>
    /// Schema 1 records have no such block. Unity then leaves every field at its zero value, and the zero
    /// value of every field means "behave like schema 1": follow the display mode, no text color, no
    /// icon. Upgrading a file therefore never changes how an existing color is drawn.
    /// The main color stays outside this block, where schema 1 stored it, so its field never moves.
    /// </remarks>
    [Serializable]
    internal struct StyleElements
    {
        [SerializeField] private StyleOverride m_Background;
        [SerializeField] private StyleOverride m_Text;
        [SerializeField] private string m_TextColor;
        [SerializeField] private StyleOverride m_Marker;
        [SerializeField] private MarkerShape m_MarkerShape;
        [SerializeField] private string m_Icon;

        /// <summary>Combines these elements with a stored main color into a complete style.</summary>
        /// <param name="colorHex">Stored main color, or an empty string for none.</param>
        internal HierarchyStyle ToStyle(string colorHex)
        {
            var style = new HierarchyStyle
            {
                HasColor = ColorHex.TryParse(colorHex, out Color32 color),
                Color = color,
                Background = m_Background,
                Text = m_Text,
                Marker = m_Marker,
                MarkerShape = m_MarkerShape,
                Icon = m_Icon ?? string.Empty
            };

            if (style.Text == StyleOverride.On)
            {
                if (ColorHex.TryParse(m_TextColor, out Color32 textColor))
                    style.TextColor = textColor;
                else
                    style.Text = StyleOverride.Default;
            }

            return style.Normalized();
        }

        /// <summary>Builds the serialized elements of a style. The main color is stored by the owner.</summary>
        /// <param name="style">Style to store.</param>
        internal static StyleElements FromStyle(HierarchyStyle style)
        {
            var normalized = style.Normalized();
            return new StyleElements
            {
                m_Background = normalized.Background,
                m_Text = normalized.Text,
                m_TextColor = normalized.Text == StyleOverride.On ? ColorHex.ToHex(normalized.TextColor, true) : string.Empty,
                m_Marker = normalized.Marker,
                m_MarkerShape = normalized.MarkerShape,
                m_Icon = normalized.Icon ?? string.Empty
            };
        }

        /// <summary>
        /// Drops values that cannot be used — an unknown enum value, an unparsable text color — and puts the
        /// rest in canonical form.
        /// </summary>
        /// <returns><c>true</c> when unusable data was dropped. Canonicalization alone does not count.</returns>
        internal bool Repair()
        {
            bool repaired = false;

            if (!IsKnown(m_Background))
            {
                m_Background = StyleOverride.Default;
                repaired = true;
            }

            if (!IsKnown(m_Text))
            {
                m_Text = StyleOverride.Default;
                repaired = true;
            }

            if (m_Text == StyleOverride.On && !ColorHex.TryParse(m_TextColor, out _))
            {
                m_Text = StyleOverride.Default;
                repaired = true;
            }

            if (!IsKnown(m_Marker))
            {
                m_Marker = StyleOverride.Default;
                repaired = true;
            }

            if (!HierarchyStyle.IsKnownShape(m_MarkerShape))
            {
                if (m_Marker == StyleOverride.On)
                    m_Marker = StyleOverride.Default;
                m_MarkerShape = MarkerShape.Dot;
                repaired = true;
            }

            this = FromStyle(ToStyle(string.Empty));
            return repaired;
        }

        private static bool IsKnown(StyleOverride value)
        {
            return value >= StyleOverride.Default && value <= StyleOverride.On;
        }
    }
}
