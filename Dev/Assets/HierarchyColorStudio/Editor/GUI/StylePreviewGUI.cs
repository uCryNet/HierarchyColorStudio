using UnityEditor;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Draws compact previews of a <see cref="HierarchyStyle"/>: a swatch for grids and lists, and a
    /// miniature Hierarchy row that shows how the background, text, marker and icon combine.
    /// </summary>
    /// <remarks>
    /// The preview row decides what to draw with <see cref="DecorationPlan.Resolve"/>, the same rules the
    /// Hierarchy uses, and draws markers with <see cref="MarkerShapes"/>, so it cannot disagree with the
    /// real row.
    /// </remarks>
    internal static class StylePreviewGUI
    {
        private const float IconSize = 16f;
        private const float TextBadgeSize = 11f;
        private const string GenericIconName = "GameObject Icon";

        private static readonly GUIContent s_Content = new GUIContent();
        private static GUIStyle s_BadgeStyle;
        private static GUIStyle s_RowLabelStyle;

        /// <summary>
        /// Draws a swatch: the main color, or an empty frame when there is none, with the icon on top and a
        /// small letter in the text color when the style sets one.
        /// </summary>
        /// <param name="rect">Swatch rectangle.</param>
        /// <param name="style">Style to show.</param>
        /// <param name="selected">Draws a highlight ring when <c>true</c>.</param>
        internal static void DrawSwatch(Rect rect, HierarchyStyle style, bool selected = false)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            if (style.HasColor)
            {
                StudioStyles.DrawSwatch(rect, style.Color, selected);
            }
            else
            {
                StudioStyles.DrawOutline(rect, selected ? EditorStyles.label.normal.textColor : StudioStyles.BorderColor,
                    selected ? 2f : 1f);
            }

            if (style.HasIcon)
            {
                var icon = BuiltinIcons.GetTexture(style.Icon);
                if (icon != null)
                {
                    float size = Mathf.Min(IconSize, Mathf.Min(rect.width, rect.height) - 2f);
                    var iconRect = new Rect(rect.center.x - size * 0.5f, rect.center.y - size * 0.5f, size, size);
                    UnityEngine.GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
                }
            }

            if (style.HasTextColor && rect.width >= TextBadgeSize * 1.5f)
            {
                var badge = new Rect(rect.xMax - TextBadgeSize - 1f, rect.yMax - TextBadgeSize - 1f, TextBadgeSize, TextBadgeSize);
                Color32 text = style.TextColor;
                bool lightText = (text.r * 299 + text.g * 587 + text.b * 114) / 1000 > 140;
                EditorCompat.DrawRoundedRect(badge, lightText ? new Color(0f, 0f, 0f, 0.75f) : new Color(1f, 1f, 1f, 0.85f), 2f);
                var badgeStyle = EnsureBadgeStyle();
                badgeStyle.normal.textColor = new Color32(text.r, text.g, text.b, 255);
                s_Content.text = "A";
                badgeStyle.Draw(badge, s_Content, false, false, false, false);
            }
        }

        /// <summary>Draws a miniature Hierarchy row with the style applied.</summary>
        /// <param name="rect">Row rectangle, normally one line high.</param>
        /// <param name="style">Style to show.</param>
        /// <param name="settings">Project appearance settings.</param>
        /// <param name="label">Object name shown on the row.</param>
        internal static void DrawRow(Rect rect, HierarchyStyle style, AppearanceSettings settings, string label)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            EditorGUI.DrawRect(rect, settings.CurrentRowBackground(EditorGUIUtility.isProSkin));
            StudioStyles.DrawOutline(rect, StudioStyles.BorderColor, 1f);

            var normalized = style.Normalized();
            var plan = DecorationPlan.Resolve(normalized, settings, false);
            var rowRect = new Rect(rect.x + 18f, rect.y, rect.width - 18f, rect.height);

            if (plan.Background)
            {
                Color tint = normalized.Color;
                tint.a = settings.TintOpacity;
                EditorGUI.DrawRect(settings.TintScope == TintScope.FullRow ? rect : rowRect, tint);
            }

            var iconRect = new Rect(rowRect.x, rowRect.y + (rowRect.height - IconSize) * 0.5f, IconSize, IconSize);
            Texture objectIcon = plan.Icon && settings.IconPlacement == IconPlacement.ObjectIcon
                ? BuiltinIcons.GetTexture(normalized.Icon)
                : null;
            if (objectIcon == null && !settings.MinimalEnabled)
                objectIcon = BuiltinIcons.GetTexture(GenericIconName);
            if (objectIcon != null)
                UnityEngine.GUI.DrawTexture(iconRect, objectIcon, ScaleMode.ScaleToFit, true);

            var labelStyle = EnsureRowLabelStyle();
            labelStyle.normal.textColor = plan.Text ? plan.TextColor : EditorStyles.label.normal.textColor;
            s_Content.text = label;
            var labelRect = new Rect(rowRect.x + settings.LabelOffset, rowRect.y, Mathf.Max(0f, rowRect.width - settings.LabelOffset), rowRect.height);
            labelStyle.Draw(labelRect, s_Content, false, false, false, false);

            Rect markerRect = default;
            if (plan.Marker)
                markerRect = MarkerShapes.GetRect(rowRect, plan.MarkerShape, settings.MarkerSize, settings.MarkerPlacement);

            if (plan.Icon && settings.IconPlacement == IconPlacement.RowEnd)
            {
                var icon = BuiltinIcons.GetTexture(normalized.Icon);
                if (icon != null)
                {
                    float right = rowRect.xMax - MarkerShapes.EdgePadding;
                    if (plan.Marker && settings.MarkerPlacement == MarkerPlacement.RowEnd)
                        right = markerRect.xMin - 2f;
                    UnityEngine.GUI.DrawTexture(new Rect(right - IconSize, iconRect.y, IconSize, IconSize), icon,
                        ScaleMode.ScaleToFit, true);
                }
            }

            if (plan.Marker)
                MarkerShapes.Draw(markerRect, plan.MarkerShape, normalized.Color);
        }

        private static GUIStyle EnsureBadgeStyle()
        {
            return s_BadgeStyle ?? (s_BadgeStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                fontSize = 9
            });
        }

        private static GUIStyle EnsureRowLabelStyle()
        {
            return s_RowLabelStyle ?? (s_RowLabelStyle = new GUIStyle(EditorStyles.label)
            {
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            });
        }
    }
}
