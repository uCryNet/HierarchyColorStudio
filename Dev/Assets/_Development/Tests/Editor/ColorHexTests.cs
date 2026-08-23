using NUnit.Framework;
using UnityEngine;

namespace CryNet.HierarchyColorStudio.Tests
{
    /// <summary>Hexadecimal parsing and formatting.</summary>
    internal sealed class ColorHexTests
    {
        [TestCase("#3498DB", 0x34, 0x98, 0xDB, 0xFF)]
        [TestCase("3498DB", 0x34, 0x98, 0xDB, 0xFF)]
        [TestCase("  #3498db  ", 0x34, 0x98, 0xDB, 0xFF)]
        [TestCase("#3498DB80", 0x34, 0x98, 0xDB, 0x80)]
        [TestCase("#F00", 0xFF, 0x00, 0x00, 0xFF)]
        [TestCase("#F008", 0xFF, 0x00, 0x00, 0x88)]
        public void TryParse_AcceptsSupportedForms(string text, int r, int g, int b, int a)
        {
            Assert.IsTrue(ColorHex.TryParse(text, out Color32 color), "Expected '{0}' to parse.", text);
            Assert.AreEqual(new Color32((byte)r, (byte)g, (byte)b, (byte)a), color);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("#")]
        [TestCase("#12")]
        [TestCase("#12345")]
        [TestCase("#1234567")]
        [TestCase("#123456789")]
        [TestCase("#GGGGGG")]
        [TestCase("not a color")]
        public void TryParse_RejectsInvalidInput(string text)
        {
            Assert.IsFalse(ColorHex.TryParse(text, out _), "Expected '{0}' to be rejected.", text);
        }

        [Test]
        public void ToHex_RoundTripsThroughTryParse()
        {
            var original = new Color32(0x12, 0xAB, 0x7F, 0x5A);

            Assert.IsTrue(ColorHex.TryParse(ColorHex.ToHex(original, true), out Color32 parsed));
            Assert.AreEqual(original, parsed);
        }

        [Test]
        public void ToHex_OmitsAlphaWhenNotRequested()
        {
            Assert.AreEqual("12AB7F", ColorHex.ToHex(new Color32(0x12, 0xAB, 0x7F, 0x5A)));
        }

        [Test]
        public void ToDisplayHex_UsesUppercaseWithHash()
        {
            Assert.AreEqual("#12AB7F", ColorHex.ToDisplayHex(new Color32(0x12, 0xAB, 0x7F, 0xFF)));
        }
    }
}
