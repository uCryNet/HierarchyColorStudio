using UnityEditor;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Geometry and drawing of row markers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Dot, Bar and Square are drawn exactly as in version 1.0: a rectangle over Unity's built-in white
    /// texture, with a corner radius for the round shapes. Their geometry is kept byte for byte, so an
    /// upgraded project shows identical markers.
    /// </para>
    /// <para>
    /// The ring uses the same rounded-rectangle drawing with a border width. The diamond and the triangle
    /// cannot be expressed as rounded rectangles, so they are drawn from small alpha masks generated in code
    /// on first use, with supersampled anti-aliasing and mipmaps for clean edges at any marker size. The
    /// package still contains no image files. The masks are destroyed before every assembly reload.
    /// </para>
    /// </remarks>
    internal static class MarkerShapes
    {
        /// <summary>Distance between a row-end marker and the right edge of the row.</summary>
        internal const float EdgePadding = 4f;

        private const float BarWidthFactor = 0.42f;
        private const float MinBarWidth = 3f;
        private const float BarVerticalInset = 2f;
        private const float RingThicknessFactor = 0.22f;
        private const float PointedShapeScale = 1.25f;
        private const int MaskSize = 64;
        private const int Supersampling = 4;

        private static Texture2D s_DiamondMask;
        private static Texture2D s_TriangleMask;

        /// <summary>Returns the rectangle a marker occupies on a row.</summary>
        /// <param name="rowRect">Row rectangle supplied by the Hierarchy callback.</param>
        /// <param name="shape">Marker shape.</param>
        /// <param name="size">Marker size in points.</param>
        /// <param name="placement">Marker placement.</param>
        internal static Rect GetRect(Rect rowRect, MarkerShape shape, float size, MarkerPlacement placement)
        {
            if (shape == MarkerShape.Bar)
            {
                float width = Mathf.Max(MinBarWidth, size * BarWidthFactor);
                float height = Mathf.Max(1f, rowRect.height - BarVerticalInset * 2f);
                return new Rect(MarkerX(rowRect, width, placement), rowRect.y + BarVerticalInset, width, height);
            }

            return new Rect(MarkerX(rowRect, size, placement), rowRect.y + (rowRect.height - size) * 0.5f, size, size);
        }

        /// <summary>Draws a marker into a rectangle returned by <see cref="GetRect"/>.</summary>
        /// <param name="rect">Marker rectangle.</param>
        /// <param name="shape">Marker shape.</param>
        /// <param name="color">Marker color. Drawn opaque.</param>
        internal static void Draw(Rect rect, MarkerShape shape, Color color)
        {
            color.a = 1f;
            switch (shape)
            {
                case MarkerShape.Bar:
                    EditorCompat.DrawRoundedRect(rect, color, rect.width * 0.5f);
                    break;
                case MarkerShape.Square:
                    EditorCompat.DrawRoundedRect(rect, color, 0f);
                    break;
                case MarkerShape.Ring:
                    DrawRing(rect, color);
                    break;
                case MarkerShape.Diamond:
                    DrawMask(Enlarge(rect), DiamondMask, color);
                    break;
                case MarkerShape.Triangle:
                    DrawMask(Enlarge(rect), TriangleMask, color);
                    break;
                default:
                    EditorCompat.DrawRoundedRect(rect, color, rect.width * 0.5f);
                    break;
            }
        }

        /// <summary>Destroys the generated masks. Called before an assembly reload so none of them leak.</summary>
        internal static void ReleaseTextures()
        {
            DestroyMask(ref s_DiamondMask);
            DestroyMask(ref s_TriangleMask);
        }

        private static float MarkerX(Rect rowRect, float width, MarkerPlacement placement)
        {
            return placement == MarkerPlacement.BeforeIcon
                ? rowRect.x - width - 1f
                : rowRect.xMax - width - EdgePadding;
        }

        /// <summary>
        /// A diamond or a triangle inscribed in the marker square covers far less area than a dot of the same
        /// size and reads as smaller. Drawing it slightly larger around the same centre gives every shape the
        /// same visual weight at a given marker size.
        /// </summary>
        private static Rect Enlarge(Rect rect)
        {
            float width = rect.width * PointedShapeScale;
            float height = rect.height * PointedShapeScale;
            return new Rect(rect.center.x - width * 0.5f, rect.center.y - height * 0.5f, width, height);
        }

        private static void DrawRing(Rect rect, Color color)
        {
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            float thickness = Mathf.Max(1f, Mathf.Round(rect.width * RingThicknessFactor));
            UnityEngine.GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0f, color,
                new Vector4(thickness, thickness, thickness, thickness), rect.width * 0.5f);
        }

        private static void DrawMask(Rect rect, Texture2D mask, Color color)
        {
            if (mask == null || rect.width <= 0f || rect.height <= 0f)
                return;

            UnityEngine.GUI.DrawTexture(rect, mask, ScaleMode.StretchToFill, true, 0f, color, 0f, 0f);
        }

        private static Texture2D DiamondMask => s_DiamondMask != null ? s_DiamondMask : (s_DiamondMask = CreateMask("Diamond", InsideDiamond));

        private static Texture2D TriangleMask => s_TriangleMask != null ? s_TriangleMask : (s_TriangleMask = CreateMask("Triangle", InsideTriangle));

        // Shapes are defined in a unit square with y pointing down, matching GUI coordinates.
        private static bool InsideDiamond(float x, float y)
        {
            return Mathf.Abs(x - 0.5f) + Mathf.Abs(y - 0.5f) <= 0.5f;
        }

        private static bool InsideTriangle(float x, float y)
        {
            // Apex at the top centre, base along the bottom. The bottom is pulled in slightly so the
            // triangle's visual weight matches a dot of the same size.
            const float top = 0.06f;
            const float bottom = 0.94f;
            if (y < top || y > bottom)
                return false;

            float halfWidth = (y - top) / (bottom - top) * 0.5f;
            return Mathf.Abs(x - 0.5f) <= halfWidth;
        }

        private static Texture2D CreateMask(string shapeName, System.Func<float, float, bool> inside)
        {
            var texture = new Texture2D(MaskSize, MaskSize, TextureFormat.RGBA32, true)
            {
                name = "HierarchyColorStudio" + shapeName + "Marker",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Trilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[MaskSize * MaskSize];
            const float step = 1f / (MaskSize * Supersampling);
            const int samples = Supersampling * Supersampling;

            for (int py = 0; py < MaskSize; py++)
            {
                for (int px = 0; px < MaskSize; px++)
                {
                    int covered = 0;
                    for (int sy = 0; sy < Supersampling; sy++)
                    {
                        for (int sx = 0; sx < Supersampling; sx++)
                        {
                            float x = (px * Supersampling + sx + 0.5f) * step;
                            float y = (py * Supersampling + sy + 0.5f) * step;
                            if (inside(x, y))
                                covered++;
                        }
                    }

                    // Texture rows run bottom-up while the shape is defined top-down.
                    byte alpha = (byte)(covered * 255 / samples);
                    pixels[(MaskSize - 1 - py) * MaskSize + px] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(true, false);
            return texture;
        }

        private static void DestroyMask(ref Texture2D mask)
        {
            if (mask != null)
                Object.DestroyImmediate(mask);
            mask = null;
        }
    }
}
