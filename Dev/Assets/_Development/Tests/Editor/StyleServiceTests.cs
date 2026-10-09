using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CryNet.HierarchyColorStudio.Tests
{
    /// <summary>Applying, editing and clearing styles through the service: multi-selection, presets and undo/redo.</summary>
    internal sealed class StyleServiceTests
    {
        private static readonly Color32 Blue = new Color32(0x34, 0x98, 0xDB, 0xFF);
        private static readonly Color32 Red = new Color32(0xE7, 0x4C, 0x3C, 0xFF);
        private static readonly Color32 White = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

        private readonly StoreIsolation m_Isolation = new StoreIsolation();
        private readonly List<GameObject> m_Created = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            m_Isolation.Begin();
            Undo.ClearAll();
            UndoScope.EndContinuous();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = m_Created.Count - 1; i >= 0; i--)
            {
                if (m_Created[i] != null)
                    Object.DestroyImmediate(m_Created[i]);
            }

            m_Created.Clear();
            Undo.ClearAll();
            m_Isolation.End();
        }

        private GameObject Create(string name)
        {
            var created = new GameObject(name);
            m_Created.Add(created);
            return created;
        }

        private static HierarchyStyle StyleOf(GameObject target)
        {
            Assert.IsTrue(HierarchyColorService.TryGetStyle(target, out HierarchyStyle style), "{0} has no style.", target.name);
            return style;
        }

        private static HierarchyStyle FullStyle()
        {
            return new HierarchyStyle
            {
                HasColor = true,
                Color = Blue,
                Background = StyleOverride.On,
                Text = StyleOverride.On,
                TextColor = White,
                Marker = StyleOverride.On,
                MarkerShape = MarkerShape.Diamond,
                Icon = "Camera Icon"
            };
        }

        [Test]
        public void ApplyStyle_ToAMultiSelection_StylesEveryObjectInOneUndoStep()
        {
            var targets = new[] { Create("A"), Create("B"), Create("C") };

            Assert.AreEqual(3, HierarchyColorService.ApplyStyle(targets, FullStyle()));
            foreach (var target in targets)
                Assert.AreEqual(FullStyle(), StyleOf(target));

            Undo.PerformUndo();

            Assert.AreEqual(0, HierarchyColorService.StoredAssignmentCount);
        }

        [Test]
        public void ApplyStyle_ReplacesTheWholeStyle()
        {
            var target = Create("Target");
            HierarchyColorService.ApplyStyle(new[] { target }, FullStyle());

            HierarchyColorService.ApplyStyle(new[] { target }, HierarchyStyle.FromColor(Red));

            Assert.AreEqual(HierarchyStyle.FromColor(Red), StyleOf(target));
        }

        [Test]
        public void ApplyStyle_WithAnEmptyStyle_ClearsTheTargets()
        {
            var target = Create("Target");
            HierarchyColorService.ApplyStyle(new[] { target }, FullStyle());

            HierarchyColorService.ApplyStyle(new[] { target }, default(HierarchyStyle));

            Assert.IsFalse(HierarchyColorService.TryGetStyle(target, out _));
            Assert.AreEqual(0, HierarchyColorService.StoredAssignmentCount);
        }

        [Test]
        public void ApplyPreset_AppliesTheCompleteStyleAndRecordsThePreset()
        {
            var target = Create("Target");
            var preset = new ColorPreset("Camera", FullStyle());
            HierarchyColorService.Store.Presets.Add(preset);

            HierarchyColorService.ApplyPreset(new[] { target }, preset);

            Assert.AreEqual(FullStyle(), StyleOf(target));
            var store = HierarchyColorService.Store;
            Assert.AreEqual(preset.Id, store.Assignments[0].PresetId);
            Assert.AreEqual(preset.Id, LastUsedColor.PresetId);
        }

        [Test]
        public void ApplyLastUsed_ReappliesTheLastPresetStyle()
        {
            var first = Create("First");
            var second = Create("Second");
            var preset = new ColorPreset("Camera", FullStyle());
            HierarchyColorService.Store.Presets.Add(preset);
            HierarchyColorService.ApplyPreset(new[] { first }, preset);

            HierarchyColorService.ApplyLastUsed(new[] { second }, ApplyScope.SelectionOnly);

            Assert.AreEqual(FullStyle(), StyleOf(second));
        }

        [Test]
        public void Assign_ChangesTheColorAndKeepsTheOtherElements()
        {
            var target = Create("Target");
            HierarchyColorService.ApplyStyle(new[] { target }, FullStyle());

            HierarchyColorService.Assign(target, (Color)Red);

            var expected = FullStyle();
            expected.Color = Red;
            Assert.AreEqual(expected, StyleOf(target));
        }

        [Test]
        public void ModifyStyle_ChangesOnlyTheSelectedElement_ForEveryObject()
        {
            var a = Create("A");
            var b = Create("B");
            HierarchyColorService.Assign(new[] { a }, (Color)Red);
            HierarchyColorService.Assign(new[] { b }, (Color)Blue);

            HierarchyColorService.ModifyStyle(new[] { a, b }, StyleFields.Text,
                new HierarchyStyle { Text = StyleOverride.On, TextColor = White });

            Assert.AreEqual(Red, StyleOf(a).Color, "Each object keeps its own color.");
            Assert.AreEqual(Blue, StyleOf(b).Color);
            Assert.AreEqual(White, StyleOf(a).TextColor);
            Assert.AreEqual(White, StyleOf(b).TextColor);
        }

        [Test]
        public void ModifyStyle_IconOnAnUnstyledObject_CreatesAnIconOnlyStyle()
        {
            var target = Create("Target");

            HierarchyColorService.ModifyStyle(new[] { target }, StyleFields.Icon, new HierarchyStyle { Icon = "Camera Icon" });

            var style = StyleOf(target);
            Assert.AreEqual("Camera Icon", style.Icon);
            Assert.IsFalse(style.HasColor);
            Assert.IsFalse(HierarchyColorService.TryGetColor(target, out _), "An icon-only style has no color.");
            Assert.IsFalse(HierarchyColorService.AnyHasColor(new[] { target }));
            Assert.IsTrue(HierarchyColorService.AnyHasStyle(new[] { target }));
        }

        [Test]
        public void ModifyStyle_MarkerOnAnUnstyledObject_UsesTheLastColor()
        {
            var colored = Create("Colored");
            var target = Create("Target");
            HierarchyColorService.Assign(colored, (Color)Red);

            HierarchyColorService.ModifyStyle(new[] { target }, StyleFields.Marker,
                new HierarchyStyle { Marker = StyleOverride.On, MarkerShape = MarkerShape.Triangle });

            var style = StyleOf(target);
            Assert.IsTrue(style.HasColor, "A marker needs a color to be visible.");
            Assert.AreEqual(Red, style.Color);
            Assert.AreEqual(MarkerShape.Triangle, style.MarkerShape);
        }

        [Test]
        public void ModifyStyle_RemovingTheColor_KeepsTheIcon()
        {
            var target = Create("Target");
            HierarchyColorService.ApplyStyle(new[] { target }, FullStyle());

            HierarchyColorService.ModifyStyle(new[] { target }, StyleFields.Color, new HierarchyStyle { HasColor = false });

            var style = StyleOf(target);
            Assert.IsFalse(style.HasColor);
            Assert.AreEqual("Camera Icon", style.Icon);
        }

        [Test]
        public void ModifyStyle_RemovingTheLastElement_RemovesTheAssignment()
        {
            var target = Create("Target");
            HierarchyColorService.ModifyStyle(new[] { target }, StyleFields.Icon, new HierarchyStyle { Icon = "Camera Icon" });

            HierarchyColorService.ModifyStyle(new[] { target }, StyleFields.Icon, new HierarchyStyle { Icon = string.Empty });

            Assert.AreEqual(0, HierarchyColorService.StoredAssignmentCount);
        }

        [Test]
        public void ModifyStyle_ThatChangesNothing_RegistersNoUndoStep()
        {
            var target = Create("Target");
            HierarchyColorService.ApplyStyle(new[] { target }, FullStyle());
            int group = Undo.GetCurrentGroup();

            int changed = HierarchyColorService.ModifyStyle(new[] { target }, StyleFields.Icon,
                new HierarchyStyle { Icon = "Camera Icon" });

            Assert.AreEqual(0, changed);
            Assert.AreEqual(group, Undo.GetCurrentGroup());
        }

        [Test]
        public void ModifyStyle_DetachesTheStyleFromItsPreset()
        {
            var target = Create("Target");
            var preset = new ColorPreset("Camera", FullStyle());
            HierarchyColorService.Store.Presets.Add(preset);
            HierarchyColorService.ApplyPreset(new[] { target }, preset);

            HierarchyColorService.ModifyStyle(new[] { target }, StyleFields.Marker, new HierarchyStyle { Marker = StyleOverride.Off });

            Assert.AreEqual(string.Empty, HierarchyColorService.Store.Assignments[0].PresetId);
        }

        [Test]
        public void UndoAndRedo_RestoreTextColorMarkerAndIcon()
        {
            var target = Create("Target");
            HierarchyColorService.Assign(target, (Color)Blue);
            var before = StyleOf(target);

            HierarchyColorService.ModifyStyle(new[] { target }, StyleFields.Text | StyleFields.Marker | StyleFields.Icon, FullStyle());
            var after = StyleOf(target);
            Assert.AreNotEqual(before, after);

            Undo.PerformUndo();
            Assert.AreEqual(before, StyleOf(target));

            Undo.PerformRedo();
            Assert.AreEqual(after, StyleOf(target));
        }

        [Test]
        public void Clear_RemovesTheWholeStyleButNeverTheGameObject()
        {
            var a = Create("A");
            var b = Create("B");
            HierarchyColorService.ApplyStyle(new[] { a }, FullStyle());
            HierarchyColorService.ModifyStyle(new[] { b }, StyleFields.Icon, new HierarchyStyle { Icon = "Camera Icon" });

            Assert.AreEqual(2, HierarchyColorService.Clear(new[] { a, b }));

            Assert.IsFalse(HierarchyColorService.AnyHasStyle(new[] { a, b }));
            Assert.IsTrue(a != null && b != null, "Clearing a style must never delete the GameObject.");

            Undo.PerformUndo();
            Assert.AreEqual(FullStyle(), StyleOf(a));
            Assert.AreEqual("Camera Icon", StyleOf(b).Icon);
        }

        [Test]
        public void ContinuousColorEdits_CollapseIntoOneUndoStep()
        {
            var target = Create("Target");
            HierarchyColorService.ApplyStyle(new[] { target }, FullStyle());

            for (int i = 0; i < 5; i++)
            {
                HierarchyColorService.ModifyStyle(new[] { target }, StyleFields.Color,
                    HierarchyStyle.FromColor(new Color32((byte)(i * 40), 0, 0, 255)), ApplyScope.SelectionOnly, true);
            }

            Undo.PerformUndo();

            Assert.AreEqual(FullStyle(), StyleOf(target));
        }

        [Test]
        public void DiscreteStep_AfterAContinuousEdit_IsNotFoldedIntoIt()
        {
            var target = Create("Target");
            HierarchyColorService.Assign(target, (Color)Blue);
            HierarchyColorService.ModifyStyle(new[] { target }, StyleFields.Color, HierarchyStyle.FromColor(Red),
                ApplyScope.SelectionOnly, true);

            HierarchyColorService.ApplyStyle(new[] { target }, FullStyle());
            HierarchyColorService.ModifyStyle(new[] { target }, StyleFields.Color, HierarchyStyle.FromColor(White),
                ApplyScope.SelectionOnly, true);

            Undo.PerformUndo();

            Assert.AreEqual(FullStyle(), StyleOf(target), "Undoing the second drag must stop at the applied style.");
        }

        [Test]
        public void PresetEdits_AreUndoable()
        {
            var store = HierarchyColorService.Store;
            var preset = store.Presets[0];
            var original = preset.Style;

            UndoScope.Record(store, UiStrings.UndoEditPresets);
            preset.Style = FullStyle();
            HierarchyColorService.NotifySettingsChanged();

            Undo.PerformUndo();

            Assert.AreEqual(original, store.Presets[0].Style);
        }

        [Test]
        public void FolderAndPrefabIconSettings_AreUndoable()
        {
            var store = HierarchyColorService.Store;

            UndoScope.Record(store, UiStrings.UndoEditAppearance);
            store.Appearance.FolderIcon = "Folder Icon";
            store.Appearance.PrefabIcon = "Favorite Icon";
            store.Appearance.Mode = HierarchyMode.Minimal | HierarchyMode.Zebra;
            HierarchyColorService.NotifySettingsChanged();

            Undo.PerformUndo();

            Assert.AreEqual(string.Empty, store.Appearance.FolderIcon);
            Assert.AreEqual(string.Empty, store.Appearance.PrefabIcon);
            Assert.AreEqual(HierarchyMode.Default, store.Appearance.Mode);
        }
    }
}
