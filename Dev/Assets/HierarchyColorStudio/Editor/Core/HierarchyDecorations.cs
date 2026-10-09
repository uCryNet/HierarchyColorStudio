using System;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Visual decorations that can be combined to build a Hierarchy display mode.
    /// </summary>
    [Flags]
    public enum HierarchyDecorations
    {
        /// <summary>No decoration is drawn.</summary>
        None = 0,

        /// <summary>A small colored marker is drawn on the row.</summary>
        Marker = 1 << 0,

        /// <summary>A translucent color wash is drawn across the row.</summary>
        RowTint = 1 << 1,

        /// <summary>The GameObject name is re-drawn using the assigned color.</summary>
        LabelColor = 1 << 2
    }

    /// <summary>Shape used when drawing the row marker.</summary>
    /// <remarks>
    /// The values are persisted as integers, so existing values never change meaning and new shapes are
    /// only ever appended.
    /// </remarks>
    public enum MarkerShape
    {
        /// <summary>A filled circle.</summary>
        Dot = 0,

        /// <summary>A rounded vertical bar.</summary>
        Bar = 1,

        /// <summary>A sharp-cornered square.</summary>
        Square = 2,

        /// <summary>An unfilled circle. Lighter than a dot, for secondary or temporary objects.</summary>
        Ring = 3,

        /// <summary>A square standing on one corner, the shape of Unity's own gizmo icons.</summary>
        Diamond = 4,

        /// <summary>An upward-pointing triangle, the conventional shape for "needs attention".</summary>
        Triangle = 5
    }

    /// <summary>
    /// Overall look of the Hierarchy window, applied to every row rather than to styled rows only.
    /// The two modes are independent and can be combined.
    /// </summary>
    [Flags]
    public enum HierarchyMode
    {
        /// <summary>Unity's Hierarchy unchanged, plus the decorations of styled rows.</summary>
        Default = 0,

        /// <summary>Subtle alternating row shading, so long flat lists stay easy to follow.</summary>
        Zebra = 1 << 0,

        /// <summary>Unity's generic GameObject icon is hidden, so the icons that carry meaning stand out.</summary>
        Minimal = 1 << 1
    }

    /// <summary>
    /// How one element of a style — its background, text or marker — relates to the project-wide
    /// <see cref="HierarchyDecorations"/> display mode.
    /// </summary>
    /// <remarks>
    /// <see cref="Default"/> is zero on purpose: a field missing from data written by an older version
    /// deserializes to it, which reproduces the older version's behaviour exactly.
    /// </remarks>
    public enum StyleOverride
    {
        /// <summary>Follow the project's display mode.</summary>
        Default = 0,

        /// <summary>Never draw this element for the style, whatever the display mode says.</summary>
        Off = 1,

        /// <summary>Always draw this element for the style, using the style's own value.</summary>
        On = 2
    }

    /// <summary>Color in which the display mode re-draws the name of a colored object.</summary>
    /// <remarks>
    /// Applies to every style whose text follows the display mode, including colors applied from the
    /// palette. A style with its own text color keeps it.
    /// </remarks>
    public enum TextColorSource
    {
        /// <summary>The object's own color, scaled by the text brightness. The behaviour of version 1.0.</summary>
        ObjectColor = 0,

        /// <summary>One color for the names of all colored objects.</summary>
        Custom = 1
    }

    /// <summary>Where the icon of a style is drawn.</summary>
    public enum IconPlacement
    {
        /// <summary>In place of the GameObject's own icon, at the start of the row.</summary>
        ObjectIcon = 0,

        /// <summary>At the right edge of the row, next to a row-end marker.</summary>
        RowEnd = 1
    }

    /// <summary>Where the row marker is placed.</summary>
    public enum MarkerPlacement
    {
        /// <summary>At the right edge of the Hierarchy row.</summary>
        RowEnd = 0,

        /// <summary>Immediately before the GameObject icon.</summary>
        BeforeIcon = 1
    }

    /// <summary>Horizontal extent of the row tint.</summary>
    public enum TintScope
    {
        /// <summary>The tint spans the whole row, ignoring indentation.</summary>
        FullRow = 0,

        /// <summary>The tint starts at the GameObject icon, following indentation.</summary>
        FromIndent = 1
    }

    /// <summary>How decorations behave on rows that are part of the current selection.</summary>
    public enum SelectedRowBehavior
    {
        /// <summary>Draw every enabled decoration.</summary>
        DrawAll = 0,

        /// <summary>Draw only the marker so Unity's selection highlight stays readable.</summary>
        MarkerOnly = 1,

        /// <summary>Draw nothing on selected rows.</summary>
        Hide = 2
    }

    /// <summary>How decorations react to the mouse hovering a row.</summary>
    public enum HoverBehavior
    {
        /// <summary>Hovering does not change the decoration.</summary>
        Ignore = 0,

        /// <summary>Hovering strengthens the row tint.</summary>
        Emphasize = 1,

        /// <summary>Hovering hides the row tint so Unity's hover highlight stays visible.</summary>
        Suppress = 2
    }

    /// <summary>Scope used when a color operation is applied to a GameObject.</summary>
    public enum ApplyScope
    {
        /// <summary>Only the targeted GameObjects.</summary>
        SelectionOnly = 0,

        /// <summary>The targeted GameObjects and their direct children.</summary>
        DirectChildren = 1,

        /// <summary>The targeted GameObjects and every descendant.</summary>
        AllDescendants = 2
    }
}
