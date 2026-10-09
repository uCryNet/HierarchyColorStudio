using NUnit.Framework;
using UnityEngine;

namespace CryNet.HierarchyColorStudio.Tests
{
    /// <summary>
    /// What a styled row draws: the rules that combine a style with the display mode, including every
    /// combination the feature set promises — text color with and without a background, marker only,
    /// icon only, marker and icon — and the version 1.0 behaviour of colors without style elements.
    /// </summary>
    internal sealed class DecorationPlanTests
    {
        private static readonly Color32 DarkBlue = new Color32(0x1A, 0x2B, 0x6C, 0xFF);
        private static readonly Color32 Orange = new Color32(0xFF, 0x8C, 0x00, 0xFF);
        private static readonly Color32 White = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

        private static AppearanceSettings Settings(HierarchyDecorations decorations,
            SelectedRowBehavior selected = SelectedRowBehavior.DrawAll)
        {
            return new AppearanceSettings
            {
                Decorations = decorations,
                SelectedRowBehavior = selected,
                MarkerShape = MarkerShape.Square,
                LabelBrightness = 1f
            };
        }

        [TestCase(HierarchyDecorations.None)]
        [TestCase(HierarchyDecorations.Marker)]
        [TestCase(HierarchyDecorations.RowTint)]
        [TestCase(HierarchyDecorations.LabelColor)]
        [TestCase(HierarchyDecorations.Marker | HierarchyDecorations.RowTint)]
        [TestCase(HierarchyDecorations.Marker | HierarchyDecorations.RowTint | HierarchyDecorations.LabelColor)]
        public void ColorOnlyStyle_DrawsWhatTheDisplayModeSays(HierarchyDecorations decorations)
        {
            var plan = DecorationPlan.Resolve(HierarchyStyle.FromColor(DarkBlue), Settings(decorations), false);

            Assert.AreEqual((decorations & HierarchyDecorations.Marker) != 0, plan.Marker);
            Assert.AreEqual((decorations & HierarchyDecorations.RowTint) != 0, plan.Background);
            Assert.AreEqual((decorations & HierarchyDecorations.LabelColor) != 0, plan.Text);
            Assert.AreEqual(MarkerShape.Square, plan.MarkerShape, "The project marker shape applies by default.");
            Assert.IsFalse(plan.Icon);
        }

        [Test]
        public void ColorOnlyStyle_OnSelectedRows_FollowsSelectedRowBehavior()
        {
            var style = HierarchyStyle.FromColor(DarkBlue);
            var all = HierarchyDecorations.Marker | HierarchyDecorations.RowTint | HierarchyDecorations.LabelColor;

            var markerOnly = DecorationPlan.Resolve(style, Settings(all, SelectedRowBehavior.MarkerOnly), true);
            Assert.IsTrue(markerOnly.Marker);
            Assert.IsFalse(markerOnly.Background);
            Assert.IsFalse(markerOnly.Text);

            var hide = DecorationPlan.Resolve(style, Settings(all, SelectedRowBehavior.Hide), true);
            Assert.IsTrue(hide.IsEmpty);

            var drawAll = DecorationPlan.Resolve(style, Settings(all, SelectedRowBehavior.DrawAll), true);
            Assert.IsTrue(drawAll.Marker && drawAll.Background && drawAll.Text);
        }

        [Test]
        public void TextColor_WithBackground_DrawsBoth()
        {
            // "Background: dark blue, Text: white".
            var style = HierarchyStyle.FromColor(DarkBlue);
            style.Background = StyleOverride.On;
            style.Text = StyleOverride.On;
            style.TextColor = White;

            var plan = DecorationPlan.Resolve(style, Settings(HierarchyDecorations.Marker), false);

            Assert.IsTrue(plan.Background, "Show overrides a display mode without Row Tint.");
            Assert.IsTrue(plan.Text);
            Assert.AreEqual((Color)White, plan.TextColor);
        }

        [Test]
        public void TextColor_WithoutBackground_DrawsTheTextAlone()
        {
            // "Background: none, Text: orange", in a project whose display mode tints every row.
            var style = new HierarchyStyle { Text = StyleOverride.On, TextColor = Orange };

            var plan = DecorationPlan.Resolve(style,
                Settings(HierarchyDecorations.Marker | HierarchyDecorations.RowTint), false);

            Assert.IsFalse(plan.Background, "A style without a main color has no background.");
            Assert.IsFalse(plan.Marker);
            Assert.IsTrue(plan.Text);
            Assert.AreEqual((Color)Orange, plan.TextColor);
        }

        [Test]
        public void TextColor_WithColorAndBackgroundHidden_DrawsTextAndMarkerOnly()
        {
            var style = HierarchyStyle.FromColor(DarkBlue);
            style.Background = StyleOverride.Off;
            style.Text = StyleOverride.On;
            style.TextColor = Orange;

            var plan = DecorationPlan.Resolve(style,
                Settings(HierarchyDecorations.Marker | HierarchyDecorations.RowTint), false);

            Assert.IsFalse(plan.Background);
            Assert.IsTrue(plan.Marker);
            Assert.AreEqual((Color)Orange, plan.TextColor);
        }

        [Test]
        public void ExplicitTextColor_IgnoresBrightness_DefaultTextUsesIt()
        {
            var settings = Settings(HierarchyDecorations.LabelColor);
            settings.LabelBrightness = 0.5f;

            var explicitStyle = new HierarchyStyle { Text = StyleOverride.On, TextColor = White };
            Assert.AreEqual((Color)White, DecorationPlan.Resolve(explicitStyle, settings, false).TextColor);

            var derived = DecorationPlan.Resolve(HierarchyStyle.FromColor(White), settings, false);
            Assert.AreEqual(0.5f, derived.TextColor.r, 0.0001f);
        }

        [Test]
        public void CustomTextColorSource_DrawsEveryDefaultNameInOneColor_IgnoringBrightness()
        {
            // A color applied from the palette has no text color of its own and follows the project.
            var settings = Settings(HierarchyDecorations.RowTint | HierarchyDecorations.LabelColor);
            settings.TextColorSource = TextColorSource.Custom;
            settings.CustomTextColor = new Color(1f, 1f, 1f, 0.3f);
            settings.LabelBrightness = 0.5f;

            foreach (var color in new[] { DarkBlue, Orange })
            {
                var plan = DecorationPlan.Resolve(HierarchyStyle.FromColor(color), settings, false);
                Assert.IsTrue(plan.Background);
                Assert.IsTrue(plan.Text);
                Assert.AreEqual(Color.white, plan.TextColor, "The custom color is drawn opaque and as is.");
            }
        }

        [Test]
        public void CustomTextColorSource_KeepsStylesWithTheirOwnTextSetting()
        {
            var settings = Settings(HierarchyDecorations.LabelColor);
            settings.TextColorSource = TextColorSource.Custom;
            settings.CustomTextColor = Color.white;

            var own = HierarchyStyle.FromColor(DarkBlue);
            own.Text = StyleOverride.On;
            own.TextColor = Orange;
            Assert.AreEqual((Color)Orange, DecorationPlan.Resolve(own, settings, false).TextColor);

            var unity = HierarchyStyle.FromColor(DarkBlue);
            unity.Text = StyleOverride.Off;
            Assert.IsFalse(DecorationPlan.Resolve(unity, settings, false).Text);

            Assert.IsFalse(DecorationPlan.Resolve(new HierarchyStyle { Icon = "Camera Icon" }, settings, false).Text,
                "A style without a main color is not a colored object.");
        }

        [Test]
        public void CustomTextColorSource_OnlyAppliesWhenTheDisplayModeColorsNames()
        {
            var settings = Settings(HierarchyDecorations.Marker | HierarchyDecorations.RowTint);
            settings.TextColorSource = TextColorSource.Custom;

            Assert.IsFalse(DecorationPlan.Resolve(HierarchyStyle.FromColor(DarkBlue), settings, false).Text);
        }

        [Test]
        public void TextColorSource_IsSanitized()
        {
            var settings = new AppearanceSettings();
            Assert.AreEqual(TextColorSource.ObjectColor, settings.TextColorSource, "Object color is the default.");

            settings.TextColorSource = (TextColorSource)42;
            settings.Sanitize();
            Assert.AreEqual(TextColorSource.ObjectColor, settings.TextColorSource);
        }

        [Test]
        public void TextOff_KeepsUnitysTextEvenWhenTheDisplayModeColorsNames()
        {
            var style = HierarchyStyle.FromColor(DarkBlue);
            style.Text = StyleOverride.Off;

            Assert.IsFalse(DecorationPlan.Resolve(style, Settings(HierarchyDecorations.LabelColor), false).Text);
        }

        [Test]
        public void MarkerOn_UsesTheStyleShape_EvenWhenTheDisplayModeHasNoMarker()
        {
            var style = HierarchyStyle.FromColor(DarkBlue);
            style.Marker = StyleOverride.On;
            style.MarkerShape = MarkerShape.Triangle;

            var plan = DecorationPlan.Resolve(style, Settings(HierarchyDecorations.RowTint), false);

            Assert.IsTrue(plan.Marker);
            Assert.AreEqual(MarkerShape.Triangle, plan.MarkerShape);
        }

        [Test]
        public void MarkerOff_HidesTheMarker()
        {
            var style = HierarchyStyle.FromColor(DarkBlue);
            style.Marker = StyleOverride.Off;

            Assert.IsFalse(DecorationPlan.Resolve(style, Settings(HierarchyDecorations.Marker), false).Marker);
        }

        [Test]
        public void IconOnly_DrawsTheIconAndNothingElse()
        {
            var plan = DecorationPlan.Resolve(new HierarchyStyle { Icon = "Camera Icon" },
                Settings(HierarchyDecorations.Marker | HierarchyDecorations.RowTint | HierarchyDecorations.LabelColor), false);

            Assert.IsTrue(plan.Icon);
            Assert.IsFalse(plan.Marker);
            Assert.IsFalse(plan.Background);
            Assert.IsFalse(plan.Text);
        }

        [Test]
        public void MarkerAndIcon_AreDrawnTogether()
        {
            // "Camera icon + red marker".
            var style = HierarchyStyle.FromColor(new Color32(0xE7, 0x4C, 0x3C, 0xFF));
            style.Icon = "Camera Icon";
            style.Marker = StyleOverride.On;
            style.MarkerShape = MarkerShape.Dot;
            style.Background = StyleOverride.Off;

            var plan = DecorationPlan.Resolve(style, Settings(HierarchyDecorations.RowTint), false);

            Assert.IsTrue(plan.Icon);
            Assert.IsTrue(plan.Marker);
            Assert.IsFalse(plan.Background);
        }

        [Test]
        public void MarkerOnly_IsAValidStyle()
        {
            var style = HierarchyStyle.FromColor(DarkBlue);
            style.Background = StyleOverride.Off;
            style.Text = StyleOverride.Off;
            style.Marker = StyleOverride.On;
            style.MarkerShape = MarkerShape.Ring;

            var plan = DecorationPlan.Resolve(style,
                Settings(HierarchyDecorations.RowTint | HierarchyDecorations.LabelColor), false);

            Assert.IsTrue(plan.Marker);
            Assert.IsFalse(plan.Background);
            Assert.IsFalse(plan.Text);
            Assert.IsFalse(plan.Icon);
        }

        [Test]
        public void Icon_StaysOnSelectedRows_InEverySelectedRowBehavior()
        {
            var style = HierarchyStyle.FromColor(DarkBlue);
            style.Icon = "Camera Icon";
            var all = HierarchyDecorations.Marker | HierarchyDecorations.RowTint | HierarchyDecorations.LabelColor;

            Assert.IsTrue(DecorationPlan.Resolve(style, Settings(all, SelectedRowBehavior.Hide), true).Icon);
            Assert.IsTrue(DecorationPlan.Resolve(style, Settings(all, SelectedRowBehavior.MarkerOnly), true).Icon);
        }
    }
}
