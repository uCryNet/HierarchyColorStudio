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
