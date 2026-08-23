using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Draws the color decoration for a single Hierarchy row.
    /// </summary>
    /// <remarks>
    /// This is the plugin's only hot path. It performs one dictionary lookup, a handful of struct
    /// operations and at most three draw calls. Nothing is allocated per row: the label text and its
    /// measured width are cached, the <see cref="GUIContent"/> and <see cref="GUIStyle"/> instances are
    /// reused, and rounded shapes are drawn from Unity's built-in white texture.
    /// Exceptions are swallowed and reported once; repeated failures disable drawing for the session so
    /// the Console can never be flooded from a repaint loop.
    /// </remarks>
    internal static class HierarchyRowRenderer
    {
        private const float MarkerEdgePadding = 4f;
        private const float BarWidthFactor = 0.42f;
        private const float MinBarWidth = 3f;
        private const float BarVerticalInset = 2f;
        private const float HoverEmphasis = 1.6f;
        private const int MaxReportedFailures = 3;

        private static readonly Dictionary<RowId, RowLabel> s_LabelCache = new Dictionary<RowId, RowLabel>(64);
        private static readonly GUIContent s_LabelContent = new GUIContent();

        private static GUIStyle s_LabelStyle;
        private static bool s_LabelStyleProSkin;
        private static int s_FailureCount;
        private static bool s_DrawingDisabled;

        private struct RowLabel
        {
            public string Name;
            public float Width;
        }

        /// <summary>Screen-space position of the last context click inside the Hierarchy window.</summary>
        internal static Vector2 LastContextClickScreenPosition { get; private set; }

        /// <summary><c>true</c> when a context click position has been recorded in this session.</summary>
        internal static bool HasContextClickPosition { get; private set; }

        /// <summary>Drops cached row labels. Called when the Hierarchy content changes.</summary>
        internal static void InvalidateLabels()
        {
            s_LabelCache.Clear();
        }

        /// <summary>Re-enables drawing after it was disabled by repeated failures.</summary>
        internal static void ResetFailureState()
        {
            s_FailureCount = 0;
            s_DrawingDisabled = false;
        }

        /// <summary>Invoked for every visible Hierarchy row.</summary>
        /// <param name="rowId">Identifier of the row's object.</param>
        /// <param name="rowRect">Row rectangle in Hierarchy window space.</param>
        internal static void OnRowGUI(RowId rowId, Rect rowRect)
        {
            var currentEvent = Event.current;
            if (currentEvent == null)
                return;

            if (currentEvent.type == EventType.ContextClick || currentEvent.type == EventType.MouseDown)
            {
                LastContextClickScreenPosition = GUIUtility.GUIToScreenPoint(currentEvent.mousePosition);
                HasContextClickPosition = true;
            }

            if (s_DrawingDisabled || currentEvent.type != EventType.Repaint)
                return;

            try
            {
                // The color lookup runs first because it is the cheapest test and it rejects almost every
                // row. Touching the store, which may have to be loaded, is deferred until a row is known
                // to be colored.
                if (!HierarchyColorService.TryGetRowColor(rowId, out Color32 assigned))
                    return;

                var store = HierarchyColorService.Store;
                if (store == null || !store.Appearance.Enabled)
                    return;

                Draw(rowId, rowRect, assigned, store.Appearance, currentEvent);
            }
            catch (Exception exception)
            {
                s_FailureCount++;
                if (s_FailureCount <= MaxReportedFailures)
                {
                    StudioLog.ExceptionOnce("row-draw-" + s_FailureCount, exception,
                        "Drawing a Hierarchy color failed.");
                }

                if (s_FailureCount > MaxReportedFailures)
                {
                    s_DrawingDisabled = true;
                    StudioLog.WarnOnce("row-draw-disabled",
                        "Hierarchy color drawing was disabled for this session after repeated failures. " +
                        "Re-enable it from " + UiStrings.MenuRootTools + UiStrings.MenuSettings + ".");
                }
            }
        }

        private static void Draw(RowId rowId, Rect rowRect, Color32 assigned, AppearanceSettings settings,
            Event currentEvent)
        {
            var decorations = settings.Decorations;
            if (decorations == HierarchyDecorations.None)
                return;

            if (HierarchySelectionCache.Contains(rowId))
            {
                switch (settings.SelectedRowBehavior)
                {
                    case SelectedRowBehavior.Hide:
                        return;
                    case SelectedRowBehavior.MarkerOnly:
                        decorations &= HierarchyDecorations.Marker;
                        break;
                }
            }

            bool hovered = settings.HoverBehavior != HoverBehavior.Ignore &&
                           rowRect.Contains(currentEvent.mousePosition);

            if ((decorations & HierarchyDecorations.RowTint) != 0)
                DrawRowTint(rowRect, assigned, settings, hovered);

            if ((decorations & HierarchyDecorations.LabelColor) != 0)
                DrawLabel(rowId, rowRect, assigned, settings);

            if ((decorations & HierarchyDecorations.Marker) != 0)
                DrawMarker(rowRect, assigned, settings);
        }

        private static void DrawRowTint(Rect rowRect, Color32 assigned, AppearanceSettings settings, bool hovered)
        {
            if (hovered && settings.HoverBehavior == HoverBehavior.Suppress)
                return;

            float opacity = settings.TintOpacity;
            if (hovered && settings.HoverBehavior == HoverBehavior.Emphasize)
                opacity = Mathf.Min(opacity * HoverEmphasis, AppearanceSettings.MaxTintOpacity);

            Rect tintRect = settings.TintScope == TintScope.FullRow
                ? new Rect(0f, rowRect.y, rowRect.xMax, rowRect.height)
                : rowRect;

            Color tint = assigned;
            tint.a = opacity;
            EditorGUI.DrawRect(tintRect, tint);
        }

        private static void DrawMarker(Rect rowRect, Color32 assigned, AppearanceSettings settings)
        {
            float size = settings.MarkerSize;
            Color color = assigned;
            color.a = 1f;

            Rect markerRect;
            float radius;
            switch (settings.MarkerShape)
            {
                case MarkerShape.Bar:
                {
                    float width = Mathf.Max(MinBarWidth, size * BarWidthFactor);
                    float height = Mathf.Max(1f, rowRect.height - BarVerticalInset * 2f);
                    markerRect = new Rect(MarkerX(rowRect, width, settings), rowRect.y + BarVerticalInset, width, height);
                    radius = width * 0.5f;
                    break;
                }
                case MarkerShape.Square:
                    markerRect = new Rect(MarkerX(rowRect, size, settings),
                        rowRect.y + (rowRect.height - size) * 0.5f, size, size);
                    radius = 0f;
                    break;
                default:
                    markerRect = new Rect(MarkerX(rowRect, size, settings),
                        rowRect.y + (rowRect.height - size) * 0.5f, size, size);
                    radius = size * 0.5f;
                    break;
            }

            EditorCompat.DrawRoundedRect(markerRect, color, radius);
        }

        private static float MarkerX(Rect rowRect, float width, AppearanceSettings settings)
        {
            return settings.MarkerPlacement == MarkerPlacement.BeforeIcon
                ? rowRect.x - width - 1f
                : rowRect.xMax - width - MarkerEdgePadding;
        }

        private static void DrawLabel(RowId rowId, Rect rowRect, Color32 assigned, AppearanceSettings settings)
        {
            if (!TryGetLabel(rowId, out RowLabel label))
                return;

            float offset = settings.LabelOffset;
            float available = rowRect.width - offset;
            if (available <= 1f)
                return;

            var labelRect = new Rect(rowRect.x + offset, rowRect.y, Mathf.Min(label.Width, available), rowRect.height);

            if (settings.LabelFillsBackground)
                EditorGUI.DrawRect(labelRect, settings.CurrentRowBackground(EditorGUIUtility.isProSkin));

            Color textColor = assigned;
            textColor.a = 1f;
            float brightness = settings.LabelBrightness;
            if (!Mathf.Approximately(brightness, 1f))
            {
                textColor.r = Mathf.Clamp01(textColor.r * brightness);
                textColor.g = Mathf.Clamp01(textColor.g * brightness);
                textColor.b = Mathf.Clamp01(textColor.b * brightness);
            }

            var style = EnsureLabelStyle();
            style.normal.textColor = textColor;
            s_LabelContent.text = label.Name;
            UnityEngine.GUI.Label(labelRect, s_LabelContent, style);
        }

        private static bool TryGetLabel(RowId rowId, out RowLabel label)
        {
            if (s_LabelCache.TryGetValue(rowId, out label))
                return label.Name != null;

            var target = rowId.ToObject();
            label = new RowLabel { Name = target != null ? target.name : null };
            if (label.Name != null)
            {
                var style = EnsureLabelStyle();
                s_LabelContent.text = label.Name;
                label.Width = style.CalcSize(s_LabelContent).x;
            }

            s_LabelCache[rowId] = label;
            return label.Name != null;
        }

        private static GUIStyle EnsureLabelStyle()
        {
            bool proSkin = EditorGUIUtility.isProSkin;
            if (s_LabelStyle == null || s_LabelStyleProSkin != proSkin)
            {
                s_LabelStyleProSkin = proSkin;
                s_LabelCache.Clear();
                s_LabelStyle = new GUIStyle(EditorStyles.label)
                {
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(0, 0, 0, 0),
                    alignment = TextAnchor.MiddleLeft,
                    clipping = TextClipping.Clip
                };
            }

            return s_LabelStyle;
        }
    }
}
