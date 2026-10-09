using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CryNet.HierarchyColorStudio.Tests
{
    /// <summary>Export and import of color sets, including rejection of foreign files.</summary>
    internal sealed class ColorTransferTests
    {
        private readonly StoreIsolation m_Isolation = new StoreIsolation();

        private string m_TempDirectory;

        [SetUp]
        public void SetUp()
        {
            m_Isolation.Begin();
            m_TempDirectory = Path.Combine(Path.GetTempPath(), "HierarchyColorStudioTransfer-" + Path.GetRandomFileName());
            Directory.CreateDirectory(m_TempDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(m_TempDirectory))
                Directory.Delete(m_TempDirectory, true);

            Undo.ClearAll();
            m_Isolation.End();
        }

        private string TempFile => Path.Combine(m_TempDirectory, "colors.json");

        [Test]
        public void ExportThenImport_RestoresTheAssignments()
        {
            var store = HierarchyColorService.Store;
            store.SetAssignment("GlobalObjectId_V1-2-abc-111-0", new Color32(1, 2, 3, 255), string.Empty);
            store.SetAssignment("GlobalObjectId_V1-2-abc-222-0", new Color32(4, 5, 6, 255), string.Empty);
            HierarchyColorService.NotifyDataChanged();

            Assert.IsTrue(ColorTransfer.Export(TempFile));

            HierarchyColorService.ClearAll();
            Assert.AreEqual(0, HierarchyColorService.StoredAssignmentCount);

            Assert.AreEqual(2, ColorTransfer.Import(TempFile, replace: false));
            Assert.AreEqual(2, HierarchyColorService.StoredAssignmentCount);
            Assert.AreEqual(new Color32(1, 2, 3, 255),
                store.Assignments[store.IndexOfKey("GlobalObjectId_V1-2-abc-111-0")].Color);
        }

        [Test]
        public void Export_SkipsSessionScopedAssignments()
        {
            var store = HierarchyColorService.Store;
            store.SetAssignment(ColorAssignment.SessionKeyPrefix + "7", Color.red, string.Empty);
            store.SetAssignment("GlobalObjectId_V1-2-abc-111-0", Color.blue, string.Empty);
            HierarchyColorService.NotifyDataChanged();

            ColorTransfer.Export(TempFile);
            string json = File.ReadAllText(TempFile);

            StringAssert.DoesNotContain(ColorAssignment.SessionKeyPrefix, json);
            StringAssert.Contains("GlobalObjectId_V1-2-abc-111-0", json);
        }

        [Test]
        public void Import_InReplaceMode_DiscardsExistingAssignments()
        {
            var store = HierarchyColorService.Store;
            store.SetAssignment("GlobalObjectId_V1-2-abc-111-0", Color.red, string.Empty);
            HierarchyColorService.NotifyDataChanged();
            ColorTransfer.Export(TempFile);

            store.SetAssignment("GlobalObjectId_V1-2-abc-999-0", Color.green, string.Empty);
            HierarchyColorService.NotifyDataChanged();

            ColorTransfer.Import(TempFile, replace: true);

            Assert.AreEqual(1, HierarchyColorService.StoredAssignmentCount);
            Assert.AreEqual("GlobalObjectId_V1-2-abc-111-0", store.Assignments[0].Key);
        }

        [Test]
        public void Import_RejectsAFileThatIsNotAColorSet()
        {
            File.WriteAllText(TempFile, "{\"kind\":\"something.else\",\"version\":1}");

            Assert.AreEqual(-1, ColorTransfer.Import(TempFile, replace: false));
        }

        [Test]
        public void Import_RejectsMalformedJsonWithoutThrowing()
        {
            File.WriteAllText(TempFile, "{ this is not json");

            Assert.AreEqual(-1, ColorTransfer.Import(TempFile, replace: false));
        }

        [Test]
        public void Import_SkipsEntriesWithAnInvalidColor()
        {
            File.WriteAllText(TempFile,
                "{\"kind\":\"CryNet.HierarchyColorStudio.ColorSet\",\"version\":1,\"presets\":[]," +
                "\"assignments\":[{\"key\":\"GlobalObjectId_V1-2-abc-111-0\",\"color\":\"nope\",\"preset\":\"\"}," +
                "{\"key\":\"GlobalObjectId_V1-2-abc-222-0\",\"color\":\"FF0000FF\",\"preset\":\"\"}]}");

            Assert.AreEqual(1, ColorTransfer.Import(TempFile, replace: false));
            Assert.AreEqual(1, HierarchyColorService.StoredAssignmentCount);
        }

        [Test]
        public void ExportThenImport_RestoresCompleteStylesAndPresets()
        {
            var store = HierarchyColorService.Store;
            var full = new HierarchyStyle
            {
                HasColor = true,
                Color = new Color32(0x1A, 0x2B, 0x6C, 0xFF),
                Background = StyleOverride.On,
                Text = StyleOverride.On,
                TextColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF),
                Marker = StyleOverride.On,
                MarkerShape = MarkerShape.Triangle,
                Icon = "Camera Icon"
            };
            var iconOnly = new HierarchyStyle { Icon = "Light Icon" };
            var textOnly = new HierarchyStyle { Text = StyleOverride.On, TextColor = new Color32(0xFF, 0x8C, 0x00, 0xFF) };

            store.SetAssignmentStyle("GlobalObjectId_V1-2-abc-111-0", full, string.Empty);
            store.SetAssignmentStyle("GlobalObjectId_V1-2-abc-222-0", iconOnly, string.Empty);
            store.SetAssignmentStyle("GlobalObjectId_V1-2-abc-333-0", textOnly, string.Empty);
            var preset = new ColorPreset("Cameras", full);
            store.Presets.Add(preset);
            HierarchyColorService.NotifyDataChanged();

            Assert.IsTrue(ColorTransfer.Export(TempFile));
            string json = File.ReadAllText(TempFile);
            StringAssert.Contains("\"version\": 2", json);
            StringAssert.Contains("\"markerShape\": \"Triangle\"", json);

            ColorTransfer.Import(TempFile, replace: true);

            Assert.AreEqual(3, HierarchyColorService.StoredAssignmentCount);
            Assert.AreEqual(full, store.Assignments[store.IndexOfKey("GlobalObjectId_V1-2-abc-111-0")].Style);
            Assert.AreEqual(iconOnly, store.Assignments[store.IndexOfKey("GlobalObjectId_V1-2-abc-222-0")].Style);
            Assert.AreEqual(textOnly, store.Assignments[store.IndexOfKey("GlobalObjectId_V1-2-abc-333-0")].Style);
            Assert.AreEqual(full, store.FindPreset(preset.Id).Style);
        }

        [Test]
        public void Import_OfAVersion1Document_GivesColorOnlyStyles()
        {
            File.WriteAllText(TempFile,
                "{\"kind\":\"CryNet.HierarchyColorStudio.ColorSet\",\"version\":1," +
                "\"presets\":[{\"id\":\"p1\",\"name\":\"Blue\",\"color\":\"3498DBFF\"}]," +
                "\"assignments\":[{\"key\":\"GlobalObjectId_V1-2-abc-111-0\",\"color\":\"FF0000FF\",\"preset\":\"p1\"}]}");

            Assert.AreEqual(1, ColorTransfer.Import(TempFile, replace: true));

            var store = HierarchyColorService.Store;
            var style = store.Assignments[0].Style;
            Assert.AreEqual(HierarchyStyle.FromColor(new Color32(0xFF, 0, 0, 0xFF)), style);
            Assert.AreEqual(StyleOverride.Default, style.Text, "A version 1 entry has no text color configured.");
            Assert.AreEqual(HierarchyStyle.FromColor(new Color32(0x34, 0x98, 0xDB, 0xFF)), store.FindPreset("p1").Style);
        }

        [Test]
        public void Import_ToleratesUnknownStyleValues()
        {
            File.WriteAllText(TempFile,
                "{\"kind\":\"CryNet.HierarchyColorStudio.ColorSet\",\"version\":2,\"presets\":[]," +
                "\"assignments\":[{\"key\":\"GlobalObjectId_V1-2-abc-111-0\",\"color\":\"FF0000FF\"," +
                "\"background\":\"Sideways\",\"text\":\"On\",\"textColor\":\"nope\"," +
                "\"marker\":\"On\",\"markerShape\":\"Hexagon\",\"icon\":\"Camera Icon\"}]}");

            Assert.AreEqual(1, ColorTransfer.Import(TempFile, replace: true));

            var style = HierarchyColorService.Store.Assignments[0].Style;
            Assert.AreEqual(StyleOverride.Default, style.Background);
            Assert.AreEqual(StyleOverride.Default, style.Text, "An unreadable text color falls back to the default.");
            Assert.AreEqual(StyleOverride.Default, style.Marker, "An unknown shape falls back to the default marker.");
            Assert.AreEqual("Camera Icon", style.Icon);
        }

        [Test]
        public void Import_IsUndoable()
        {
            var store = HierarchyColorService.Store;
            store.SetAssignment("GlobalObjectId_V1-2-abc-111-0", Color.red, string.Empty);
            HierarchyColorService.NotifyDataChanged();
            ColorTransfer.Export(TempFile);
            HierarchyColorService.ClearAll();

            ColorTransfer.Import(TempFile, replace: false);
            Assert.AreEqual(1, HierarchyColorService.StoredAssignmentCount);

            Undo.PerformUndo();

            Assert.AreEqual(0, HierarchyColorService.StoredAssignmentCount);
        }
    }
}
