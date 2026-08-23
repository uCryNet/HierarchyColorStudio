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
    public enum MarkerShape
    {
        /// <summary>A filled circle.</summary>
        Dot = 0,

        /// <summary>A rounded vertical bar.</summary>
        Bar = 1,

        /// <summary>A sharp-cornered square.</summary>
        Square = 2
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
