using System.IO;
using NUnit.Framework;
using UnityEditorInternal;
using UnityEngine;

namespace CryNet.HierarchyColorStudio.Tests
{
    /// <summary>
    /// Upgrading settings files written by version 1.0 (schema 1). The fixtures are real schema 1 files,
    /// loaded through Unity's own deserializer, so they also cover how Unity fills the fields schema 1 lacks.
    /// </summary>
    internal sealed class StyleMigrationTests
    {
        private const string PresetRedId = "d28f1bd2a26843508f85b124241bb901";
        private const string PresetGreenId = "4370946002a9415cbaaccbe03502d618";
        private const string KeyA = "GlobalObjectId_V1-2-1418f92b8d7271aacba8778b8453d3f1-1790670796-0";
        private const string KeyB = "GlobalObjectId_V1-2-1418f92b8d7271aacba8778b8453d3f1-453595394-0";

        // A schema 1 file as version 1.0 writes it, modelled on a real project: customised preset colors,
        // Bar markers before the icon, every decoration on, and non-default behaviours.
        private static readonly string[] s_Version1Lines =
        {
            "%YAML 1.1",
            "%TAG !u! tag:unity3d.com,2011:",
            "--- !u!114 &1",
            "MonoBehaviour:",
            "  m_ObjectHideFlags: 0",
            "  m_CorrespondingSourceObject: {fileID: 0}",
            "  m_PrefabInstance: {fileID: 0}",
            "  m_PrefabAsset: {fileID: 0}",
            "  m_GameObject: {fileID: 0}",
            "  m_Enabled: 1",
            "  m_EditorHideFlags: 0",
            "  m_Script: {fileID: 11500000, guid: c3e1aab73da22cc4890818555df7d414, type: 3}",
            "  m_Name: Hierarchy Color Studio",
            "  m_EditorClassIdentifier: CryNet.HierarchyColorStudio.Editor::CryNet.HierarchyColorStudio.HierarchyColorStore",
            "  m_Version: 1",
            "  m_Appearance:",
            "    m_Enabled: 1",
            "    m_Decorations: 7",
            "    m_MarkerShape: 1",
            "    m_MarkerPlacement: 1",
            "    m_MarkerSize: 9",
            "    m_TintOpacity: 0.3",
            "    m_TintScope: 1",
            "    m_LabelBrightness: 1.2",
            "    m_LabelFillsBackground: 1",
            "    m_RowBackgroundDark: {r: 0.2196, g: 0.2196, b: 0.2196, a: 1}",
            "    m_RowBackgroundLight: {r: 0.7843, g: 0.7843, b: 0.7843, a: 1}",
            "    m_SelectedRowBehavior: 0",
            "    m_HoverBehavior: 1",
            "    m_LabelOffset: 18",
            "    m_DefaultApplyScope: 2",
            "    m_DebugLogging: 0",
            "  m_Presets:",
            "  - m_Id: " + PresetRedId,
            "    m_Name: Red",
            "    m_Color: FF1800FF",
            "  - m_Id: " + PresetGreenId,
            "    m_Name: Green",
            "    m_Color: 00FE6CFF",
            "  m_Assignments:",
            "  - m_Key: " + KeyA,
            "    m_Color: 00FE6CFF",
            "    m_PresetId: " + PresetGreenId,
            "  - m_Key: " + KeyB,
            "    m_Color: 3498DBFF",
            "    m_PresetId: ",
            ""
        };

        private string m_TempDirectory;

        [SetUp]
        public void SetUp()
        {
            m_TempDirectory = Path.Combine(Path.GetTempPath(), "HierarchyColorStudioMigration-" + Path.GetRandomFileName());
            Directory.CreateDirectory(m_TempDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(m_TempDirectory))
                Directory.Delete(m_TempDirectory, true);
        }

        private string TempFile => Path.Combine(m_TempDirectory, HierarchyColorStoreFile.FileName);

        private string WriteFixture(params string[] lines)
        {
            File.WriteAllText(TempFile, string.Join("\n", lines));
            return TempFile;
        }

        /// <summary>Deserializes a file without sanitizing it, exactly as Unity hands it to the plugin.</summary>
        private static HierarchyColorStore LoadRaw(string path)
        {
            var loaded = InternalEditorUtility.LoadSerializedFileAndForget(path);
            Assert.IsNotNull(loaded);
            for (int i = 0; i < loaded.Length; i++)
            {
                if (loaded[i] is HierarchyColorStore store)
                    return store;
            }

            Assert.Fail("The fixture did not deserialize into a store.");
            return null;
        }

        [Test]
        public void Version1File_UpgradesWithoutReportingRepairs()
        {
            var store = LoadRaw(WriteFixture(s_Version1Lines));
            try
            {
                Assert.AreEqual(1, store.Version, "The fixture must be read as schema 1.");

                int repairs = store.Sanitize();

                Assert.AreEqual(0, repairs, "A clean schema 1 file must upgrade without any repair.");
                Assert.AreEqual(1, store.UpgradedFromVersion);
                Assert.AreEqual(HierarchyColorStore.CurrentVersion, store.Version);
                Assert.AreEqual(0, store.Sanitize(), "Sanitizing an upgraded store again must change nothing.");
                Assert.AreEqual(0, store.UpgradedFromVersion);
            }
            finally
            {
                Object.DestroyImmediate(store);
            }
        }

        [Test]
        public void Version1File_KeepsEveryAppearanceSettingAndAddsOnlyNeutralDefaults()
        {
            var store = HierarchyColorStoreFile.LoadOrCreate(WriteFixture(s_Version1Lines));
            try
            {
                var appearance = store.Appearance;
                Assert.AreEqual(HierarchyDecorations.Marker | HierarchyDecorations.RowTint | HierarchyDecorations.LabelColor,
                    appearance.Decorations);
                Assert.AreEqual(MarkerShape.Bar, appearance.MarkerShape);
                Assert.AreEqual(MarkerPlacement.BeforeIcon, appearance.MarkerPlacement);
                Assert.AreEqual(9f, appearance.MarkerSize, 0.0001f);
                Assert.AreEqual(0.3f, appearance.TintOpacity, 0.0001f);
                Assert.AreEqual(TintScope.FromIndent, appearance.TintScope);
                Assert.AreEqual(1.2f, appearance.LabelBrightness, 0.0001f);
                Assert.IsTrue(appearance.LabelFillsBackground);
                Assert.AreEqual(SelectedRowBehavior.DrawAll, appearance.SelectedRowBehavior);
                Assert.AreEqual(HoverBehavior.Emphasize, appearance.HoverBehavior);
                Assert.AreEqual(ApplyScope.AllDescendants, appearance.DefaultApplyScope);

                Assert.AreEqual(HierarchyMode.Default, appearance.Mode);
                Assert.AreEqual(AppearanceSettings.DefaultZebraOpacity, appearance.ZebraOpacity, 0.0001f);
                Assert.AreEqual(IconPlacement.ObjectIcon, appearance.IconPlacement);
                Assert.AreEqual(string.Empty, appearance.FolderIcon);
                Assert.AreEqual(string.Empty, appearance.PrefabIcon);
                Assert.AreEqual(TextColorSource.ObjectColor, appearance.TextColorSource,
                    "Names keep being drawn in each object's own color.");
                Assert.AreEqual(Color.white, appearance.CustomTextColor);
                Assert.IsFalse(appearance.AffectsEveryRow, "An upgraded project must not decorate rows it did not decorate before.");
            }
            finally
            {
                Object.DestroyImmediate(store);
            }
        }

        [Test]
        public void Version1File_KeepsPresetsAsColorOnlyStyles()
        {
            var store = HierarchyColorStoreFile.LoadOrCreate(WriteFixture(s_Version1Lines));
            try
            {
                Assert.AreEqual(2, store.Presets.Count);

                var red = store.FindPreset(PresetRedId);
                Assert.IsNotNull(red);
                Assert.AreEqual("Red", red.Name);
                Assert.AreEqual(new Color32(0xFF, 0x18, 0x00, 0xFF), red.Color);
                Assert.AreEqual(HierarchyStyle.FromColor(new Color32(0xFF, 0x18, 0x00, 0xFF)), red.Style);

                var green = store.FindPreset(PresetGreenId);
                Assert.IsNotNull(green);
                Assert.AreEqual(StyleOverride.Default, green.Style.Text);
                Assert.AreEqual(StyleOverride.Default, green.Style.Marker);
                Assert.IsFalse(green.Style.HasIcon);
            }
            finally
            {
                Object.DestroyImmediate(store);
            }
        }

        [Test]
        public void Version1File_KeepsAssignmentsWithTextColorNotConfigured()
        {
            var store = HierarchyColorStoreFile.LoadOrCreate(WriteFixture(s_Version1Lines));
            try
            {
                Assert.AreEqual(2, store.Assignments.Count);

                var a = store.Assignments[store.IndexOfKey(KeyA)];
                Assert.AreEqual(new Color32(0x00, 0xFE, 0x6C, 0xFF), a.Color);
                Assert.AreEqual(PresetGreenId, a.PresetId);

                var b = store.Assignments[store.IndexOfKey(KeyB)];
                Assert.AreEqual(new Color32(0x34, 0x98, 0xDB, 0xFF), b.Color);
                Assert.AreEqual(string.Empty, b.PresetId);

                // "Not configured" must stay distinguishable from any explicit text color, including the
                // one the display mode happens to produce.
                Assert.AreEqual(StyleOverride.Default, a.Style.Text);
                Assert.IsFalse(a.Style.HasTextColor);
                Assert.AreEqual(StyleOverride.Default, a.Style.Background);
                Assert.AreEqual(StyleOverride.Default, a.Style.Marker);
                Assert.IsFalse(a.Style.HasIcon);
                Assert.AreEqual(HierarchyStyle.FromColor(a.Color), a.Style);
            }
            finally
            {
                Object.DestroyImmediate(store);
            }
        }

        [Test]
        public void Version1File_DrawsExactlyWhatVersion1Drew()
        {
            var store = HierarchyColorStoreFile.LoadOrCreate(WriteFixture(s_Version1Lines));
            try
            {
                var settings = store.Appearance;
                var style = store.Assignments[store.IndexOfKey(KeyB)].Style;

                foreach (bool selected in new[] { false, true })
                {
                    // Selected Rows is Draw All in the fixture, so selection changes nothing.
                    var plan = DecorationPlan.Resolve(style, settings, selected);
                    Assert.IsTrue(plan.Background);
                    Assert.IsTrue(plan.Marker);
                    Assert.AreEqual(MarkerShape.Bar, plan.MarkerShape, "The project marker shape must still apply.");
                    Assert.IsTrue(plan.Text);
                    Assert.IsFalse(plan.Icon);

                    // Version 1.0 drew the name in the assigned color multiplied by the brightness.
                    Color expected = (Color)new Color32(0x34, 0x98, 0xDB, 0xFF);
                    Assert.AreEqual(Mathf.Clamp01(expected.r * 1.2f), plan.TextColor.r, 0.0001f);
                    Assert.AreEqual(Mathf.Clamp01(expected.g * 1.2f), plan.TextColor.g, 0.0001f);
                    Assert.AreEqual(Mathf.Clamp01(expected.b * 1.2f), plan.TextColor.b, 0.0001f);
                    Assert.AreEqual(1f, plan.TextColor.a, 0.0001f);
                }
            }
            finally
            {
                Object.DestroyImmediate(store);
            }
        }

        [Test]
        public void Version1File_WithUnusableRecords_AppliesVersion1Rules()
        {
            var lines = (string[])s_Version1Lines.Clone();
            // Break the second preset's color and the second assignment's color.
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == "    m_Color: 00FE6CFF" && lines[i - 1] == "    m_Name: Green")
                    lines[i] = "    m_Color: not-a-color";
                if (lines[i] == "    m_Color: 3498DBFF")
                    lines[i] = "    m_Color: ";
            }

            var store = LoadRaw(WriteFixture(lines));
            try
            {
                int repairs = store.Sanitize();

                Assert.AreEqual(1, repairs, "Only the assignment without a color is dropped.");
                Assert.AreEqual(1, store.Assignments.Count);
                Assert.AreEqual(KeyA, store.Assignments[0].Key);
                Assert.AreEqual((Color32)Color.white, store.FindPreset(PresetGreenId).Color,
                    "Version 1.0 repaired an unreadable preset color to white.");
                Assert.IsTrue(store.FindPreset(PresetGreenId).Style.HasColor);
            }
            finally
            {
                Object.DestroyImmediate(store);
            }
        }

        [Test]
        public void Version2FileWithoutTextColorSettings_KeepsObjectColorNames()
        {
            // Written by an earlier 1.1 build, before the project text color existed.
            var lines = (string[])s_Version1Lines.Clone();
            lines[System.Array.IndexOf(lines, "  m_Version: 1")] = "  m_Version: 2";

            var store = HierarchyColorStoreFile.LoadOrCreate(WriteFixture(lines));
            try
            {
                Assert.AreEqual(0, store.UpgradedFromVersion, "A schema 2 file is not upgraded.");
                Assert.AreEqual(TextColorSource.ObjectColor, store.Appearance.TextColorSource);
                Assert.AreEqual(Color.white, store.Appearance.CustomTextColor,
                    "The missing color gets the default of a new project, whatever Unity initialized it to.");
            }
            finally
            {
                Object.DestroyImmediate(store);
            }
        }

        [Test]
        public void UpgradedFile_IsWrittenAsVersion2AndRoundTrips()
        {
            var store = HierarchyColorStoreFile.LoadOrCreate(WriteFixture(s_Version1Lines));
            string resaved = Path.Combine(m_TempDirectory, "resaved.asset");
            try
            {
                var style = store.Assignments[store.IndexOfKey(KeyB)].Style;
                style.Text = StyleOverride.On;
                style.TextColor = new Color32(255, 255, 255, 255);
                style.Icon = "Camera Icon";
                store.SetAssignmentStyle(KeyB, style, string.Empty);
                store.Appearance.TextColorSource = TextColorSource.Custom;
                store.Appearance.CustomTextColor = new Color32(0xFF, 0xD7, 0x00, 0xFF);

                Assert.IsTrue(HierarchyColorStoreFile.Save(store, resaved));
            }
            finally
            {
                Object.DestroyImmediate(store);
            }

            string text = File.ReadAllText(resaved);
            StringAssert.Contains("m_Version: 2", text);
            StringAssert.Contains("m_Elements:", text);
            StringAssert.Contains("m_Icon: Camera Icon", text);

            var reloaded = HierarchyColorStoreFile.LoadOrCreate(resaved);
            try
            {
                Assert.AreEqual(0, reloaded.UpgradedFromVersion);
                var b = reloaded.Assignments[reloaded.IndexOfKey(KeyB)].Style;
                Assert.AreEqual(StyleOverride.On, b.Text);
                Assert.AreEqual(new Color32(255, 255, 255, 255), b.TextColor);
                Assert.AreEqual("Camera Icon", b.Icon);
                Assert.AreEqual(new Color32(0x34, 0x98, 0xDB, 0xFF), b.Color, "The main color must survive untouched.");

                var a = reloaded.Assignments[reloaded.IndexOfKey(KeyA)].Style;
                Assert.AreEqual(StyleOverride.Default, a.Text, "Untouched records keep 'not configured'.");

                Assert.AreEqual(TextColorSource.Custom, reloaded.Appearance.TextColorSource);
                Assert.AreEqual(new Color32(0xFF, 0xD7, 0x00, 0xFF), (Color32)reloaded.Appearance.CustomTextColor);
            }
            finally
            {
                Object.DestroyImmediate(reloaded);
            }
        }
    }
}
