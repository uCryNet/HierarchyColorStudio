using System.Text;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Allocation-conscious hexadecimal color conversion helpers.
    /// Accepted input forms are <c>RGB</c>, <c>RGBA</c>, <c>RRGGBB</c> and <c>RRGGBBAA</c>,
    /// with an optional leading <c>#</c> and surrounding whitespace.
    /// </summary>
    public static class ColorHex
    {
        private const int HexCharsPerByte = 2;
        private static readonly StringBuilder s_Builder = new StringBuilder(8);

        /// <summary>Tries to convert a hexadecimal string into a color.</summary>
        /// <param name="text">The hexadecimal text to parse.</param>
        /// <param name="color">The parsed color, or <see cref="Color.magenta"/> when parsing fails.</param>
        /// <returns><c>true</c> when <paramref name="text"/> is a valid hexadecimal color.</returns>
        public static bool TryParse(string text, out Color32 color)
        {
            color = new Color32(255, 0, 255, 255);
            if (string.IsNullOrEmpty(text))
                return false;

            int start = 0;
            int end = text.Length;
            while (start < end && char.IsWhiteSpace(text[start])) start++;
            while (end > start && char.IsWhiteSpace(text[end - 1])) end--;
            if (start < end && text[start] == '#') start++;

            int length = end - start;
            if (length != 3 && length != 4 && length != 6 && length != 8)
                return false;

            byte r = 0, g = 0, b = 0, a = 255;
            bool shortForm = length <= 4;
            int channelCount = shortForm ? length : length / HexCharsPerByte;
            for (int i = 0; i < channelCount; i++)
            {
                byte value;
                if (shortForm)
                {
                    int nibble = HexValue(text[start + i]);
                    if (nibble < 0) return false;
                    value = (byte)(nibble * 17);
                }
                else
                {
                    int high = HexValue(text[start + i * HexCharsPerByte]);
                    int low = HexValue(text[start + i * HexCharsPerByte + 1]);
                    if (high < 0 || low < 0) return false;
                    value = (byte)((high << 4) | low);
                }

                switch (i)
                {
                    case 0: r = value; break;
                    case 1: g = value; break;
                    case 2: b = value; break;
                    default: a = value; break;
                }
            }

            color = new Color32(r, g, b, a);
            return true;
        }

        /// <summary>Formats a color as an uppercase hexadecimal string without a leading <c>#</c>.</summary>
        /// <param name="color">The color to format.</param>
        /// <param name="includeAlpha">When <c>true</c> the alpha channel is appended.</param>
        public static string ToHex(Color32 color, bool includeAlpha = false)
        {
            s_Builder.Length = 0;
            AppendByte(s_Builder, color.r);
            AppendByte(s_Builder, color.g);
            AppendByte(s_Builder, color.b);
            if (includeAlpha)
                AppendByte(s_Builder, color.a);
            return s_Builder.ToString();
        }

        /// <summary>Formats a color as an uppercase <c>#RRGGBB</c> string for display purposes.</summary>
        /// <param name="color">The color to format.</param>
        public static string ToDisplayHex(Color32 color)
        {
            s_Builder.Length = 0;
            s_Builder.Append('#');
            AppendByte(s_Builder, color.r);
            AppendByte(s_Builder, color.g);
            AppendByte(s_Builder, color.b);
            return s_Builder.ToString();
        }

        private static void AppendByte(StringBuilder builder, byte value)
        {
            builder.Append(HexDigit(value >> 4));
            builder.Append(HexDigit(value & 0xF));
        }

        private static char HexDigit(int nibble)
        {
            return (char)(nibble < 10 ? '0' + nibble : 'A' + (nibble - 10));
        }

        private static int HexValue(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            return -1;
        }
    }
}
