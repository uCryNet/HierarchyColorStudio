using UnityEditor;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Theme-aware styles, swatch drawing and the procedurally generated product icon.
    /// Every color is either taken from <see cref="EditorStyles"/> or derived from
    /// <see cref="EditorGUIUtility.isProSkin"/>, so the UI is correct in both Editor themes.
    /// </summary>
    internal static class StudioStyles
    {
        private const int IconSize = 16;
        private const float SwatchCornerRadius = 3f;

        private static GUIStyle s_SectionHeader;
        private static GUIStyle s_Hint;
        private static GUIStyle s_SwatchButton;
        private static GUIStyle s_MonospaceLabel;
        private static Texture2D s_ProductIcon;
        private static bool s_CachedProSkin;

        /// <summary>Bold header used above a settings section.</summary>
        internal static GUIStyle SectionHeader
        {
            get
            {
                EnsureThemeUpToDate();
                return s_SectionHeader ?? (s_SectionHeader = new GUIStyle(EditorStyles.boldLabel)
                {
                    margin = new RectOffset(0, 0, 2, 2)
                });
            }
        }

        /// <summary>Dimmed, word-wrapped style used for inline hints.</summary>
        internal static GUIStyle Hint
        {
            get
            {
                EnsureThemeUpToDate();
                if (s_Hint == null)
                {
                    s_Hint = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
                    var color = s_Hint.normal.textColor;
                    color.a = 0.75f;
                    s_Hint.normal.textColor = color;
                }

                return s_Hint;
            }
        }

        /// <summary>Flat button style used behind color swatches.</summary>
        internal static GUIStyle SwatchButton
        {
            get
            {
                EnsureThemeUpToDate();
                return s_SwatchButton ?? (s_SwatchButton = new GUIStyle(UnityEngine.GUI.skin.box)
                {
                    margin = new RectOffset(1, 1, 1, 1),
                    padding = new RectOffset(0, 0, 0, 0)
                });
            }
        }

        /// <summary>Label style used for hexadecimal values.</summary>
        internal static GUIStyle MonospaceLabel
        {
            get
            {
                EnsureThemeUpToDate();
                return s_MonospaceLabel ?? (s_MonospaceLabel = new GUIStyle(EditorStyles.label)
                {
                    font = EditorStyles.miniLabel.font,
                    alignment = TextAnchor.MiddleLeft
                });
            }
        }

        /// <summary>
        /// The product icon, generated in code as three overlapping translucent discs.
        /// Drawing it procedurally keeps the package free of binary image assets.
        /// </summary>
        internal static Texture2D ProductIcon
        {
            get
            {
                if (s_ProductIcon == null)
                    s_ProductIcon = CreateProductIcon();
                return s_ProductIcon;
            }
        }

        /// <summary>Border color that reads correctly in both Editor themes.</summary>
        internal static Color BorderColor =>
            EditorGUIUtility.isProSkin ? new Color(0f, 0f, 0f, 0.45f) : new Color(0f, 0f, 0f, 0.22f);

        /// <summary>Draws a color swatch with a subtle border.</summary>
        /// <param name="rect">Rectangle to fill.</param>
        /// <param name="color">Swatch color.</param>
        /// <param name="selected">Draws a highlight ring when <c>true</c>.</param>
        internal static void DrawSwatch(Rect rect, Color color, bool selected = false)
        {
            var opaque = color;
            opaque.a = 1f;
            EditorCompat.DrawRoundedRect(rect, opaque, SwatchCornerRadius);

            var border = selected ? EditorStyles.label.normal.textColor : BorderColor;
            DrawOutline(rect, border, selected ? 2f : 1f);
        }

        /// <summary>Draws a one-pixel separator line.</summary>
        /// <param name="topSpacing">Space above the line.</param>
        /// <param name="bottomSpacing">Space below the line.</param>
        internal static void DrawSeparator(float topSpacing = 4f, float bottomSpacing = 4f)
        {
            GUILayout.Space(topSpacing);
            var rect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, BorderColor);
            GUILayout.Space(bottomSpacing);
        }

        /// <summary>Draws a rectangular outline of the requested thickness.</summary>
        /// <param name="rect">Rectangle to outline.</param>
        /// <param name="color">Outline color.</param>
        /// <param name="thickness">Outline thickness in points.</param>
        internal static void DrawOutline(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private static void EnsureThemeUpToDate()
        {
            bool proSkin = EditorGUIUtility.isProSkin;
            if (s_CachedProSkin == proSkin && s_SectionHeader != null)
                return;

            s_CachedProSkin = proSkin;
            s_SectionHeader = null;
            s_Hint = null;
            s_SwatchButton = null;
            s_MonospaceLabel = null;
        }

        private static Texture2D CreateProductIcon()
        {
            var texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false)
            {
                name = "HierarchyColorStudioIcon",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color32[IconSize * IconSize];
            var discColors = new[]
            {
                new Color(0.20f, 0.60f, 0.86f),
                new Color(0.18f, 0.80f, 0.44f),
                new Color(0.90f, 0.49f, 0.13f)
            };
            var discCenters = new[]
            {
                new Vector2(5.6f, 6.0f),
                new Vector2(10.4f, 6.0f),
                new Vector2(8.0f, 10.4f)
            };
            const float radius = 4.2f;

            for (int y = 0; y < IconSize; y++)
            {
                for (int x = 0; x < IconSize; x++)
                {
                    var point = new Vector2(x + 0.5f, y + 0.5f);
                    var accumulated = new Color(0f, 0f, 0f, 0f);
                    for (int disc = 0; disc < discCenters.Length; disc++)
                    {
                        float distance = Vector2.Distance(point, discCenters[disc]);
                        float coverage = Mathf.Clamp01(radius - distance);
                        if (coverage <= 0f)
                            continue;

                        float alpha = coverage * 0.85f;
                        accumulated = Color.Lerp(accumulated, discColors[disc], alpha);
                        accumulated.a = Mathf.Max(accumulated.a, alpha);
                    }

                    pixels[y * IconSize + x] = accumulated;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }
    }
}
