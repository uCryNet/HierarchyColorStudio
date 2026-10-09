using System;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>Selects individual elements of a <see cref="HierarchyStyle"/>, for edits that change only some of them.</summary>
    [Flags]
    public enum StyleFields
    {
        /// <summary>No element.</summary>
        None = 0,

        /// <summary><see cref="HierarchyStyle.HasColor"/> and <see cref="HierarchyStyle.Color"/>.</summary>
        Color = 1 << 0,

        /// <summary><see cref="HierarchyStyle.Background"/>.</summary>
        Background = 1 << 1,

        /// <summary><see cref="HierarchyStyle.Text"/> and <see cref="HierarchyStyle.TextColor"/>.</summary>
        Text = 1 << 2,

        /// <summary><see cref="HierarchyStyle.Marker"/> and <see cref="HierarchyStyle.MarkerShape"/>.</summary>
        Marker = 1 << 3,

        /// <summary><see cref="HierarchyStyle.Icon"/>.</summary>
        Icon = 1 << 4,

        /// <summary>Every element.</summary>
        All = Color | Background | Text | Marker | Icon
    }

    /// <summary>
    /// The complete visual configuration of a Hierarchy row: what a preset stores and what an assignment
    /// applies. Its elements are independent, so a row can show a color, a text color, a marker and an
    /// icon in any combination.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Color"/> is the style's main color. The background wash and the marker are drawn in it,
    /// and so is the object's name while <see cref="Text"/> follows the display mode.
    /// </para>
    /// <para>
    /// Background, text and marker each carry a <see cref="StyleOverride"/>. At
    /// <see cref="StyleOverride.Default"/> the element follows the project's
    /// <see cref="AppearanceSettings.Decorations"/>, which is exactly how every color behaved before styles
    /// existed. The zero value of every field therefore reproduces that behaviour.
    /// </para>
    /// <para>
    /// The icon and the marker are separate on purpose: the icon says what an object is, the marker says how
    /// it is classified, and a row may show either, both or neither.
    /// </para>
    /// </remarks>
    public struct HierarchyStyle : IEquatable<HierarchyStyle>
    {
        /// <summary><c>true</c> when the style has a main color. A style without one can still show an icon or a text color.</summary>
        public bool HasColor;

        /// <summary>Main color, used by the background, the marker and a display-mode text color.</summary>
        public Color32 Color;

        /// <summary>Whether the background wash is drawn.</summary>
        public StyleOverride Background;

        /// <summary>How the object's name is drawn. <see cref="StyleOverride.On"/> uses <see cref="TextColor"/>.</summary>
        public StyleOverride Text;

        /// <summary>Explicit text color, used only when <see cref="Text"/> is <see cref="StyleOverride.On"/>.</summary>
        public Color32 TextColor;

        /// <summary>Whether the marker is drawn. <see cref="StyleOverride.On"/> uses <see cref="MarkerShape"/>.</summary>
        public StyleOverride Marker;

        /// <summary>Marker shape, used only when <see cref="Marker"/> is <see cref="StyleOverride.On"/>.</summary>
        public MarkerShape MarkerShape;

        /// <summary>Name of a Unity built-in Editor icon, or <c>null</c>/empty for no icon.</summary>
        public string Icon;

        /// <summary>A style with a main color and every other element following the display mode.</summary>
        /// <param name="color">Main color.</param>
        public static HierarchyStyle FromColor(Color32 color)
        {
            return new HierarchyStyle { HasColor = true, Color = color };
        }

        /// <summary><c>true</c> when the style names an icon.</summary>
        public bool HasIcon => !string.IsNullOrEmpty(Icon);

        /// <summary><c>true</c> when the style sets an explicit text color.</summary>
        public bool HasTextColor => Text == StyleOverride.On;

        /// <summary><c>true</c> when the style would draw nothing at all: no color, no text color and no icon.</summary>
        public bool IsEmpty => !HasColor && !HasIcon && !HasTextColor;

        /// <summary>
        /// Returns a copy in canonical form: values that the style does not use are reset, the icon name is
        /// trimmed, and out-of-range enum values fall back to their defaults.
        /// </summary>
        public HierarchyStyle Normalized()
        {
            var result = this;
            if (!result.HasColor)
                result.Color = default;

            result.Background = Sanitize(result.Background);
            result.Text = Sanitize(result.Text);
            result.Marker = Sanitize(result.Marker);

            if (result.Text != StyleOverride.On)
                result.TextColor = default;

            if (result.Marker != StyleOverride.On || !IsKnownShape(result.MarkerShape))
            {
                if (result.Marker == StyleOverride.On)
                    result.Marker = StyleOverride.Default;
                result.MarkerShape = MarkerShape.Dot;
            }

            result.Icon = BuiltinIcons.NormalizeName(result.Icon);
            return result;
        }

        /// <summary>Returns a copy of this style with the selected elements taken from <paramref name="source"/>.</summary>
        /// <param name="source">Style supplying the new values.</param>
        /// <param name="fields">Elements to copy.</param>
        public HierarchyStyle With(HierarchyStyle source, StyleFields fields)
        {
            var result = this;
            if ((fields & StyleFields.Color) != 0)
            {
                result.HasColor = source.HasColor;
                result.Color = source.Color;
            }

            if ((fields & StyleFields.Background) != 0)
                result.Background = source.Background;

            if ((fields & StyleFields.Text) != 0)
            {
                result.Text = source.Text;
                result.TextColor = source.TextColor;
            }

            if ((fields & StyleFields.Marker) != 0)
            {
                result.Marker = source.Marker;
                result.MarkerShape = source.MarkerShape;
            }

            if ((fields & StyleFields.Icon) != 0)
                result.Icon = source.Icon;

            return result;
        }

        /// <summary>
        /// Lists the elements in which two styles differ, comparing only values that are in use and ignoring
        /// alpha. Used to show mixed values for a multi-selection.
        /// </summary>
        /// <param name="other">Style to compare with.</param>
        public StyleFields Differences(HierarchyStyle other)
        {
            var a = Normalized();
            var b = other.Normalized();
            var differences = StyleFields.None;

            if (a.HasColor != b.HasColor || !SameColor(a.Color, b.Color))
                differences |= StyleFields.Color;
            if (a.Background != b.Background)
                differences |= StyleFields.Background;
            if (a.Text != b.Text || !SameColor(a.TextColor, b.TextColor))
                differences |= StyleFields.Text;
            if (a.Marker != b.Marker || a.MarkerShape != b.MarkerShape)
                differences |= StyleFields.Marker;
            if (!string.Equals(a.Icon ?? string.Empty, b.Icon ?? string.Empty, StringComparison.Ordinal))
                differences |= StyleFields.Icon;

            return differences;
        }

        /// <inheritdoc/>
        public bool Equals(HierarchyStyle other)
        {
            return Differences(other) == StyleFields.None;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is HierarchyStyle other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var n = Normalized();
            unchecked
            {
                int hash = n.HasColor ? PackColor(n.Color) : 0;
                hash = hash * 31 + (int)n.Background;
                hash = hash * 31 + (int)n.Text;
                hash = hash * 31 + PackColor(n.TextColor);
                hash = hash * 31 + (int)n.Marker;
                hash = hash * 31 + (int)n.MarkerShape;
                hash = hash * 31 + (n.Icon != null ? n.Icon.GetHashCode() : 0);
                return hash;
            }
        }

        /// <summary>Compares two styles for visual equality.</summary>
        public static bool operator ==(HierarchyStyle left, HierarchyStyle right)
        {
            return left.Equals(right);
        }

        /// <summary>Compares two styles for visual inequality.</summary>
        public static bool operator !=(HierarchyStyle left, HierarchyStyle right)
        {
            return !left.Equals(right);
        }

        private static StyleOverride Sanitize(StyleOverride value)
        {
            return value >= StyleOverride.Default && value <= StyleOverride.On ? value : StyleOverride.Default;
        }

        /// <summary><c>true</c> when the value is one of the declared marker shapes.</summary>
        /// <param name="shape">Shape to test.</param>
        internal static bool IsKnownShape(MarkerShape shape)
        {
            return shape >= MarkerShape.Dot && shape <= MarkerShape.Triangle;
        }

        // Alpha is ignored: every decoration draws the color opaque or at its own opacity, so two colors
        // that differ only in alpha look identical in the Hierarchy.
        private static bool SameColor(Color32 a, Color32 b)
        {
            return a.r == b.r && a.g == b.g && a.b == b.b;
        }

        private static int PackColor(Color32 color)
        {
            return (color.r << 16) | (color.g << 8) | color.b;
        }
    }
}
