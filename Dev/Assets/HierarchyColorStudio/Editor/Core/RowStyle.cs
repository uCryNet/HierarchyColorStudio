using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// A style prepared for drawing. It is built once when the assignment index is rebuilt and read on
    /// every repaint, so the Hierarchy callback never parses a color or hashes an icon name.
    /// </summary>
    internal readonly struct RowStyle
    {
        /// <summary>The normalized style.</summary>
        internal readonly HierarchyStyle Style;

        /// <summary>Handle of the style's icon in <see cref="BuiltinIcons"/>, or <c>-1</c> for none.</summary>
        internal readonly int IconHandle;

        private RowStyle(HierarchyStyle style, int iconHandle)
        {
            Style = style;
            IconHandle = iconHandle;
        }

        /// <summary>Prepares a style for drawing.</summary>
        /// <param name="style">Style to prepare.</param>
        internal static RowStyle From(HierarchyStyle style)
        {
            var normalized = style.Normalized();
            return new RowStyle(normalized, BuiltinIcons.GetHandle(normalized.Icon));
        }
    }

    /// <summary>
    /// What a styled row draws, decided from its style, the project's appearance settings and whether the
    /// row is selected. Kept free of any GUI call so the rules can be tested directly.
    /// </summary>
    internal struct DecorationPlan
    {
        /// <summary>Draw the background wash in <see cref="HierarchyStyle.Color"/>.</summary>
        internal bool Background;

        /// <summary>Re-draw the object's name in <see cref="TextColor"/>.</summary>
        internal bool Text;

        /// <summary>Opaque color of the re-drawn name.</summary>
        internal Color TextColor;

        /// <summary>Draw the marker in <see cref="HierarchyStyle.Color"/>.</summary>
        internal bool Marker;

        /// <summary>Shape of the marker.</summary>
        internal MarkerShape MarkerShape;

        /// <summary>Draw the style's icon.</summary>
        internal bool Icon;

        /// <summary><c>true</c> when the plan draws nothing.</summary>
        internal bool IsEmpty => !Background && !Text && !Marker && !Icon;

        /// <summary>Decides what a styled row draws.</summary>
        /// <param name="style">Normalized style of the row.</param>
        /// <param name="settings">Project appearance settings.</param>
        /// <param name="selected"><c>true</c> when the row is part of the selection.</param>
        /// <remarks>
        /// A style whose elements are all at <see cref="StyleOverride.Default"/> — every color assigned by
        /// version 1.0 — produces exactly the decorations version 1.0 drew. The icon is never suppressed on
        /// selected rows, because it stands in for Unity's own icon, which Unity keeps showing there too.
        /// </remarks>
        internal static DecorationPlan Resolve(in HierarchyStyle style, AppearanceSettings settings, bool selected)
        {
            var decorations = settings.Decorations;
            var plan = new DecorationPlan
            {
                Background = style.HasColor && Shows(style.Background, decorations, HierarchyDecorations.RowTint),
                Marker = style.HasColor && Shows(style.Marker, decorations, HierarchyDecorations.Marker),
                MarkerShape = style.Marker == StyleOverride.On ? style.MarkerShape : settings.MarkerShape,
                Icon = style.HasIcon
            };

            if (style.Text == StyleOverride.On)
            {
                plan.Text = true;
                plan.TextColor = Opaque(style.TextColor);
            }
            else if (style.Text == StyleOverride.Default && style.HasColor &&
                     (decorations & HierarchyDecorations.LabelColor) != 0)
            {
                plan.Text = true;
                plan.TextColor = settings.TextColorSource == TextColorSource.Custom
                    ? Opaque(settings.CustomTextColor)
                    : ApplyBrightness(Opaque(style.Color), settings.LabelBrightness);
            }

            if (selected)
            {
                switch (settings.SelectedRowBehavior)
                {
                    case SelectedRowBehavior.Hide:
                        plan.Background = false;
                        plan.Text = false;
                        plan.Marker = false;
                        break;
                    case SelectedRowBehavior.MarkerOnly:
                        plan.Background = false;
                        plan.Text = false;
                        break;
                }
            }

            return plan;
        }

        private static bool Shows(StyleOverride value, HierarchyDecorations decorations, HierarchyDecorations element)
        {
            return value == StyleOverride.On || (value == StyleOverride.Default && (decorations & element) != 0);
        }

        private static Color Opaque(Color color)
        {
            color.a = 1f;
            return color;
        }

        private static Color ApplyBrightness(Color color, float brightness)
        {
            if (Mathf.Approximately(brightness, 1f))
                return color;

            color.r = Mathf.Clamp01(color.r * brightness);
            color.g = Mathf.Clamp01(color.g * brightness);
            color.b = Mathf.Clamp01(color.b * brightness);
            return color;
        }
    }
}
