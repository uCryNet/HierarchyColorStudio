using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Draws the decoration of a single Hierarchy row.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the plugin's only hot path. A row without a style costs one dictionary lookup, unless a
    /// Hierarchy mode or a folder or prefab icon is active, in which case it also costs a cached
    /// classification and at most two rectangle draws. Nothing is allocated per row: the label text and its
    /// measured width are cached, the <see cref="GUIContent"/> and <see cref="GUIStyle"/> instances are
    /// reused, styles arrive pre-parsed from the index, icons are resolved through a handle, and shapes
    /// are drawn from Unity's built-in white texture or from masks generated once.
    /// </para>
    /// <para>
    /// Layers are drawn back to front: the cover over Unity's own icon, the zebra stripe, the background
    /// wash, the re-drawn name, the icon and finally the marker. Everything translucent therefore sits on
    /// one consistent base, whichever combination of features is enabled.
    /// </para>
    /// <para>
    /// Exceptions are swallowed and reported once; repeated failures disable drawing for the session so
    /// the Console can never be flooded from a repaint loop.
    /// </para>
    /// </remarks>
    internal static class HierarchyRowRenderer
    {
        private const float HoverEmphasis = 1.6f;
        private const float IconSize = 16f;
        private const float RowEndIconGap = 2f;
        private const float InactiveIconAlpha = 0.4f;
        private const int MaxReportedFailures = 3;

        private static readonly Dictionary<RowId, RowLabel> s_LabelCache = new Dictionary<RowId, RowLabel>(64);
        private static readonly GUIContent s_LabelContent = new GUIContent();

        private static GUIStyle s_LabelStyle;
        private static bool s_LabelStyleProSkin;
        private static int s_FailureCount;
        private static bool s_DrawingDisabled;

        // Handles of the folder and prefab icons, refreshed only when the setting's string changes.
        private static string s_FolderIconName;
        private static int s_FolderIconHandle = -1;
        private static string s_PrefabIconName;
        private static int s_PrefabIconHandle = -1;

        private struct RowLabel
        {
            public string Name;
            public float Width;
        }

        /// <summary>
        /// The translucent layers drawn over a row's base color. Kept together so the name area, which
        /// <see cref="AppearanceSettings.LabelFillsBackground"/> repaints opaque, can receive the same layers.
        /// </summary>
        private struct RowLayers
        {
            public Color Base;
            public bool Zebra;
            public Rect ZebraRect;
            public Color ZebraColor;
            public bool Tint;
            public Rect TintRect;
            public Color TintColor;

            public void DrawOverlays(Rect area)
            {
                if (Zebra)
                    DrawClipped(ZebraRect, area, ZebraColor);
                if (Tint)
                    DrawClipped(TintRect, area, TintColor);
            }

            private static void DrawClipped(Rect layer, Rect area, Color color)
            {
                float xMin = Mathf.Max(layer.xMin, area.xMin);
                float xMax = Mathf.Min(layer.xMax, area.xMax);
                if (xMax > xMin)
                    EditorGUI.DrawRect(new Rect(xMin, area.y, xMax - xMin, area.height), color);
            }
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

        /// <summary>Returns the row parity used by Zebra mode: <c>true</c> for every second row.</summary>
        /// <param name="rowRect">Row rectangle supplied by the Hierarchy callback.</param>
        internal static bool IsStripedRow(Rect rowRect)
        {
            if (rowRect.height <= 0f)
                return false;

            // Rows are laid out from the top of the tree view at a fixed height, so the y position is a row
            // index. Rounding absorbs fractional offsets of high-density displays.
            int index = Mathf.FloorToInt(rowRect.y / rowRect.height + 0.5f);
            return (index & 1) == 1;
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
                // The style lookup runs first because it is the cheapest test. Rows without a style only
                // continue when a setting decorates every row.
                bool styled = HierarchyColorService.TryGetRowStyle(rowId, out RowStyle style);

                var store = HierarchyColorService.Store;
                if (store == null || !store.Appearance.Enabled)
                    return;

                var settings = store.Appearance;
                if (!styled && !settings.AffectsEveryRow)
                    return;

                Draw(rowId, rowRect, styled, style, settings, currentEvent);
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

        private static void Draw(RowId rowId, Rect rowRect, bool styled, RowStyle rowStyle,
            AppearanceSettings settings, Event currentEvent)
        {
            bool selected = HierarchySelectionCache.Contains(rowId);
            var plan = styled ? DecorationPlan.Resolve(rowStyle.Style, settings, selected) : default;

            Texture objectIcon = null;
            bool coverObjectIcon = false;
            bool dimIcon = false;
            ResolveObjectIcon(rowId, rowStyle, plan, settings, ref objectIcon, ref coverObjectIcon, ref dimIcon);

            bool zebra = settings.ZebraEnabled && !selected && IsStripedRow(rowRect);
            if (plan.IsEmpty && !coverObjectIcon && !zebra)
                return;

            var fullRowRect = new Rect(0f, rowRect.y, rowRect.xMax, rowRect.height);
            bool coversSomething = coverObjectIcon || (plan.Text && settings.LabelFillsBackground);
            var layers = new RowLayers
            {
                Base = coversSomething
                    ? HierarchyRowBackground.Get(settings, selected, fullRowRect.Contains(currentEvent.mousePosition))
                    : default,
                Zebra = zebra,
                ZebraRect = fullRowRect,
                ZebraColor = zebra ? ZebraColor(settings) : default
            };

            if (plan.Background)
                PrepareTint(rowRect, rowStyle.Style.Color, settings, currentEvent, ref layers);

            var iconRect = new Rect(rowRect.x, rowRect.y + (rowRect.height - IconSize) * 0.5f, IconSize, IconSize);
            if (coverObjectIcon)
                EditorGUI.DrawRect(iconRect, layers.Base);

            if (layers.Zebra)
                EditorGUI.DrawRect(layers.ZebraRect, layers.ZebraColor);

            if (layers.Tint)
                EditorGUI.DrawRect(layers.TintRect, layers.TintColor);

            if (plan.Text)
                DrawLabel(rowId, rowRect, plan.TextColor, settings, layers);

            if (objectIcon != null)
                DrawIcon(iconRect, objectIcon, dimIcon);

            Rect markerRect = default;
            if (plan.Marker)
                markerRect = MarkerShapes.GetRect(rowRect, plan.MarkerShape, settings.MarkerSize, settings.MarkerPlacement);

            if (plan.Icon && settings.IconPlacement == IconPlacement.RowEnd)
                DrawRowEndIcon(rowId, rowRect, rowStyle, plan, settings, markerRect);

            if (plan.Marker)
                MarkerShapes.Draw(markerRect, plan.MarkerShape, rowStyle.Style.Color);
        }

        /// <summary>
        /// Decides what replaces Unity's own row icon: the style's icon, a folder or prefab icon, or —
        /// in Minimal mode — nothing at all, which hides Unity's generic GameObject icon.
        /// </summary>
        private static void ResolveObjectIcon(RowId rowId, RowStyle rowStyle, DecorationPlan plan,
            AppearanceSettings settings, ref Texture icon, ref bool cover, ref bool dim)
        {
            bool styleIcon = plan.Icon && settings.IconPlacement == IconPlacement.ObjectIcon;
            bool rowEffects = settings.MinimalEnabled || settings.FolderIcon.Length > 0 || settings.PrefabIcon.Length > 0;
            if (!styleIcon && !rowEffects)
                return;

            var info = HierarchyRowClassifier.Classify(rowId);
            dim = info.Inactive;

            if (styleIcon)
            {
                icon = BuiltinIcons.GetTexture(rowStyle.IconHandle);
                cover = icon != null;
                if (cover)
                    return;
            }

            switch (info.Kind)
            {
                case RowKind.Folder:
                    icon = BuiltinIcons.GetTexture(CachedHandle(settings.FolderIcon, ref s_FolderIconName, ref s_FolderIconHandle));
                    break;
                case RowKind.PrefabRoot:
                    icon = BuiltinIcons.GetTexture(CachedHandle(settings.PrefabIcon, ref s_PrefabIconName, ref s_PrefabIconHandle));
                    break;
            }

            if (icon != null)
            {
                cover = true;
                return;
            }

            // Minimal mode hides the generic GameObject icon, which every ordinary row repeats and which
            // therefore carries no information. Prefab icons and icons chosen in the Inspector stay.
            cover = settings.MinimalEnabled && (info.Kind == RowKind.GameObject || info.Kind == RowKind.Folder);
        }

        private static int CachedHandle(string name, ref string cachedName, ref int cachedHandle)
        {
            if (!ReferenceEquals(name, cachedName))
            {
                cachedName = name;
                cachedHandle = BuiltinIcons.GetHandle(name);
            }

            return cachedHandle;
        }

        private static Color ZebraColor(AppearanceSettings settings)
        {
            // Lighten in the dark theme and darken in the light theme, so the stripe always reads as a
            // gentle step away from the background rather than as a color.
            return EditorGUIUtility.isProSkin
                ? new Color(1f, 1f, 1f, settings.ZebraOpacity)
                : new Color(0f, 0f, 0f, settings.ZebraOpacity);
        }

        private static void PrepareTint(Rect rowRect, Color32 color, AppearanceSettings settings, Event currentEvent,
            ref RowLayers layers)
        {
            bool hovered = settings.HoverBehavior != HoverBehavior.Ignore && rowRect.Contains(currentEvent.mousePosition);
            if (hovered && settings.HoverBehavior == HoverBehavior.Suppress)
                return;

            float opacity = settings.TintOpacity;
            if (hovered && settings.HoverBehavior == HoverBehavior.Emphasize)
                opacity = Mathf.Min(opacity * HoverEmphasis, AppearanceSettings.MaxTintOpacity);

            Color tint = color;
            tint.a = opacity;

            layers.Tint = true;
            layers.TintColor = tint;
            layers.TintRect = settings.TintScope == TintScope.FullRow
                ? new Rect(0f, rowRect.y, rowRect.xMax, rowRect.height)
                : rowRect;
        }

        private static void DrawIcon(Rect rect, Texture icon, bool dim)
        {
            if (!dim)
            {
                UnityEngine.GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit, true);
                return;
            }

            // Unity draws the icons of inactive GameObjects faded, and a replacement must follow suit.
            var previous = UnityEngine.GUI.color;
            UnityEngine.GUI.color = new Color(previous.r, previous.g, previous.b, previous.a * InactiveIconAlpha);
            UnityEngine.GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit, true);
            UnityEngine.GUI.color = previous;
        }

        private static void DrawRowEndIcon(RowId rowId, Rect rowRect, RowStyle rowStyle, DecorationPlan plan,
            AppearanceSettings settings, Rect markerRect)
        {
            var icon = BuiltinIcons.GetTexture(rowStyle.IconHandle);
            if (icon == null)
                return;

            float right = rowRect.xMax - MarkerShapes.EdgePadding;
            if (plan.Marker && settings.MarkerPlacement == MarkerPlacement.RowEnd)
                right = markerRect.xMin - RowEndIconGap;

            var rect = new Rect(right - IconSize, rowRect.y + (rowRect.height - IconSize) * 0.5f, IconSize, IconSize);
            DrawIcon(rect, icon, HierarchyRowClassifier.Classify(rowId).Inactive);
        }

        private static void DrawLabel(RowId rowId, Rect rowRect, Color textColor, AppearanceSettings settings,
            RowLayers layers)
        {
            if (!TryGetLabel(rowId, out RowLabel label))
                return;

            float offset = settings.LabelOffset;
            float available = rowRect.width - offset;
            if (available <= 1f)
                return;

            var labelRect = new Rect(rowRect.x + offset, rowRect.y, Mathf.Min(label.Width, available), rowRect.height);

            if (settings.LabelFillsBackground)
            {
                // Hide Unity's own label under the row's base color, then restore the translucent layers
                // the fill covered, so the name sits on the same background as the rest of the row.
                EditorGUI.DrawRect(labelRect, layers.Base);
                layers.DrawOverlays(labelRect);
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
