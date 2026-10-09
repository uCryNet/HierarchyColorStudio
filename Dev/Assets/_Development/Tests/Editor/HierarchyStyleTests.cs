using NUnit.Framework;
using UnityEngine;

namespace CryNet.HierarchyColorStudio.Tests
{
    /// <summary>The style model: defaults, normalization, partial edits, comparison and serialization through records.</summary>
    internal sealed class HierarchyStyleTests
    {
        private static readonly Color32 Blue = new Color32(0x34, 0x98, 0xDB, 0xFF);
        private static readonly Color32 White = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

        [Test]
        public void Default_IsEmpty()
        {
            var style = default(HierarchyStyle);

            Assert.IsTrue(style.IsEmpty);
            Assert.IsFalse(style.HasColor);
            Assert.IsFalse(style.HasIcon);
            Assert.IsFalse(style.HasTextColor);
        }

        [Test]
        public void FromColor_FollowsTheDisplayModeForEveryElement()
        {
            var style = HierarchyStyle.FromColor(Blue);

            Assert.IsTrue(style.HasColor);
            Assert.AreEqual(Blue, style.Color);
            Assert.AreEqual(StyleOverride.Default, style.Background);
            Assert.AreEqual(StyleOverride.Default, style.Text);
            Assert.AreEqual(StyleOverride.Default, style.Marker);
            Assert.IsFalse(style.HasIcon);
        }

        [Test]
        public void IconOrTextColorAlone_IsNotEmpty()
        {
            Assert.IsFalse(new HierarchyStyle { Icon = "Camera Icon" }.IsEmpty);
            Assert.IsFalse(new HierarchyStyle { Text = StyleOverride.On, TextColor = White }.IsEmpty);
        }

        [Test]
        public void Normalized_ClearsValuesThatAreNotInUse()
        {
            var style = new HierarchyStyle
            {
                Color = Blue,
                TextColor = White,
                MarkerShape = MarkerShape.Diamond,
                Icon = "  Camera Icon@2x  "
            }.Normalized();

            Assert.AreEqual(default(Color32), style.Color, "Without HasColor the color is unused.");
            Assert.AreEqual(default(Color32), style.TextColor, "Without Text On the text color is unused.");
            Assert.AreEqual(MarkerShape.Dot, style.MarkerShape, "Without Marker On the shape is unused.");
            Assert.AreEqual("Camera Icon", style.Icon);
        }

        [Test]
        public void Normalized_RejectsUnknownEnumValues()
        {
            var style = new HierarchyStyle
            {
                Background = (StyleOverride)42,
                Text = (StyleOverride)(-1),
                Marker = StyleOverride.On,
                MarkerShape = (MarkerShape)99
            }.Normalized();

            Assert.AreEqual(StyleOverride.Default, style.Background);
            Assert.AreEqual(StyleOverride.Default, style.Text);
            Assert.AreEqual(StyleOverride.Default, style.Marker);
            Assert.AreEqual(MarkerShape.Dot, style.MarkerShape);
        }

        [Test]
        public void With_CopiesOnlyTheSelectedElements()
        {
            var original = new HierarchyStyle { HasColor = true, Color = Blue, Icon = "Camera Icon" };
            var source = new HierarchyStyle
            {
                HasColor = true,
                Color = White,
                Text = StyleOverride.On,
                TextColor = White,
                Marker = StyleOverride.On,
                MarkerShape = MarkerShape.Triangle,
                Icon = "Light Icon"
            };

            var result = original.With(source, StyleFields.Text | StyleFields.Marker);

            Assert.AreEqual(Blue, result.Color, "The color was not selected.");
            Assert.AreEqual("Camera Icon", result.Icon, "The icon was not selected.");
            Assert.AreEqual(StyleOverride.On, result.Text);
            Assert.AreEqual(White, result.TextColor);
            Assert.AreEqual(StyleOverride.On, result.Marker);
            Assert.AreEqual(MarkerShape.Triangle, result.MarkerShape);
        }

        [Test]
        public void Differences_ReportsEachElementIndependently()
        {
            var a = HierarchyStyle.FromColor(Blue);
            var b = a;
            b.Icon = "Camera Icon";
            b.Marker = StyleOverride.Off;

            Assert.AreEqual(StyleFields.Icon | StyleFields.Marker, a.Differences(b));
            Assert.AreEqual(StyleFields.None, a.Differences(a));
        }

        [Test]
        public void Equality_IgnoresAlphaAndUnusedValues()
        {
            var a = HierarchyStyle.FromColor(new Color32(10, 20, 30, 255));
            var b = HierarchyStyle.FromColor(new Color32(10, 20, 30, 128));
            b.MarkerShape = MarkerShape.Ring;

            Assert.AreEqual(a, b);
            Assert.IsTrue(a == b);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void MarkerShapeValues_AreStable()
        {
            // Persisted as integers: existing values may never change meaning.
            Assert.AreEqual(0, (int)MarkerShape.Dot);
            Assert.AreEqual(1, (int)MarkerShape.Bar);
            Assert.AreEqual(2, (int)MarkerShape.Square);
            Assert.AreEqual(3, (int)MarkerShape.Ring);
            Assert.AreEqual(4, (int)MarkerShape.Diamond);
            Assert.AreEqual(5, (int)MarkerShape.Triangle);
            Assert.AreEqual(0, (int)StyleOverride.Default, "Default must be the zero value absent fields deserialize to.");
            Assert.AreEqual(0, (int)HierarchyMode.Default);
            Assert.AreEqual(0, (int)IconPlacement.ObjectIcon);
        }

        [Test]
        public void Assignment_StoresAndReturnsTheCompleteStyle()
        {
            var style = new HierarchyStyle
            {
                HasColor = true,
                Color = Blue,
                Background = StyleOverride.Off,
                Text = StyleOverride.On,
                TextColor = White,
                Marker = StyleOverride.On,
                MarkerShape = MarkerShape.Diamond,
                Icon = "Camera Icon"
            };

            var assignment = new ColorAssignment("key", style, "preset");

            Assert.AreEqual(style, assignment.Style);
            Assert.IsTrue(assignment.HasColor);
            Assert.AreEqual(Blue, assignment.Color);
            Assert.AreEqual("preset", assignment.PresetId);
            Assert.IsTrue(assignment.IsValid());
        }

        [Test]
        public void Assignment_WithoutColor_IsValidOnlyWhenSomethingElseIsDrawn()
        {
            var iconOnly = new ColorAssignment("key", new HierarchyStyle { Icon = "Camera Icon" }, string.Empty);
            var nothing = new ColorAssignment("key", default(HierarchyStyle), string.Empty);

            Assert.IsTrue(iconOnly.IsValid());
            Assert.IsFalse(iconOnly.HasColor);
            Assert.IsFalse(nothing.IsValid());
        }

        [Test]
        public void Assignment_RepairStyle_DropsAnUnreadableColorButKeepsTheIcon()
        {
            var assignment = new ColorAssignment("key", new HierarchyStyle { HasColor = true, Color = Blue, Icon = "Camera Icon" }, string.Empty);
            typeof(ColorAssignment)
                .GetField("m_Color", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(assignment, "not-a-color");

            Assert.IsTrue(assignment.RepairStyle());
            Assert.IsFalse(assignment.HasColor);
            Assert.AreEqual("Camera Icon", assignment.Style.Icon);
            Assert.IsTrue(assignment.IsValid());
        }

        [Test]
        public void Preset_StoresTheCompleteStyle_AndCloneKeepsIt()
        {
            var style = new HierarchyStyle { Text = StyleOverride.On, TextColor = White, Icon = "Favorite Icon" };
            var preset = new ColorPreset("Text only", style);

            Assert.AreEqual(style, preset.Style);
            Assert.IsFalse(preset.Style.HasColor);
            Assert.AreEqual(string.Empty, preset.Hex);

            var clone = preset.Clone();
            Assert.AreEqual(preset.Id, clone.Id);
            Assert.AreEqual(style, clone.Style);
        }

        [Test]
        public void Preset_SettingTheColor_KeepsTheOtherElements()
        {
            var preset = new ColorPreset("Camera", new HierarchyStyle { Icon = "Camera Icon" });

            preset.Color = Blue;

            Assert.IsTrue(preset.Style.HasColor);
            Assert.AreEqual(Blue, preset.Style.Color);
            Assert.AreEqual("Camera Icon", preset.Style.Icon);
        }

        [Test]
        public void Preset_Sanitize_KeepsAColorlessPresetColorless()
        {
            var preset = new ColorPreset("Icon only", new HierarchyStyle { Icon = "Camera Icon" });

            preset.Sanitize();

            Assert.IsFalse(preset.Style.HasColor, "An empty color means no color, not white.");
            Assert.AreEqual("Camera Icon", preset.Style.Icon);
        }
    }
}
