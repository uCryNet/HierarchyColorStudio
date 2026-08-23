using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace CryNet.HierarchyColorStudio.Tests
{
    /// <summary>Serialization round trip and recovery from an unreadable file.</summary>
    internal sealed class StorePersistenceTests
    {
        private string m_TempDirectory;

        [SetUp]
        public void SetUp()
        {
            m_TempDirectory = Path.Combine(Path.GetTempPath(), "HierarchyColorStudioTests-" + Path.GetRandomFileName());
            Directory.CreateDirectory(m_TempDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(m_TempDirectory))
                Directory.Delete(m_TempDirectory, true);
        }

        private string TempFile => Path.Combine(m_TempDirectory, HierarchyColorStoreFile.FileName);

        [Test]
        public void SaveThenLoad_RestoresAssignmentsPresetsAndAppearance()
        {
            var original = ScriptableObject.CreateInstance<HierarchyColorStore>();
            try
            {
                original.ResetToDefaults();
                original.SetAssignment("GlobalObjectId_V1-2-abc-123-0", new Color32(0x11, 0x22, 0x33, 0xFF), "p1");
                original.Presets.Add(new ColorPreset("Custom", new Color32(9, 8, 7, 255)));
                original.Appearance.Decorations = HierarchyDecorations.LabelColor;
                original.Appearance.TintOpacity = 0.4f;

                Assert.IsTrue(HierarchyColorStoreFile.Save(original, TempFile));
            }
            finally
            {
                Object.DestroyImmediate(original);
            }

            var loaded = HierarchyColorStoreFile.LoadOrCreate(TempFile);
            try
            {
                Assert.AreEqual(1, loaded.Assignments.Count);
                Assert.AreEqual("GlobalObjectId_V1-2-abc-123-0", loaded.Assignments[0].Key);
                Assert.AreEqual(new Color32(0x11, 0x22, 0x33, 0xFF), loaded.Assignments[0].Color);
                Assert.AreEqual("p1", loaded.Assignments[0].PresetId);
                Assert.AreEqual(HierarchyDecorations.LabelColor, loaded.Appearance.Decorations);
                Assert.AreEqual(0.4f, loaded.Appearance.TintOpacity, 0.0001f);
                Assert.IsNotNull(loaded.Presets.Find(preset => preset.Name == "Custom"));
            }
            finally
            {
                Object.DestroyImmediate(loaded);
            }
        }

        [Test]
        public void SavedFileIsTextAndContainsTheStoredColor()
        {
            var store = ScriptableObject.CreateInstance<HierarchyColorStore>();
            try
            {
                store.ResetToDefaults();
                store.SetAssignment("key", new Color32(0xAB, 0xCD, 0xEF, 0xFF), string.Empty);
                HierarchyColorStoreFile.Save(store, TempFile);
            }
            finally
            {
                Object.DestroyImmediate(store);
            }

            string text = File.ReadAllText(TempFile);
            StringAssert.Contains("ABCDEF", text);
        }

        [Test]
        public void LoadOrCreate_FallsBackToDefaultsWhenFileIsMissing()
        {
            var store = HierarchyColorStoreFile.LoadOrCreate(TempFile);
            try
            {
                Assert.Greater(store.Presets.Count, 0);
                Assert.AreEqual(0, store.Assignments.Count);
            }
            finally
            {
                Object.DestroyImmediate(store);
            }
        }

        [Test]
        public void LoadOrCreate_RecoversFromAnUnreadableFile()
        {
            File.WriteAllText(TempFile, "this is not a serialized Unity object");

            // Unity's native loader may report the malformed file to the Console; that is the behaviour
            // being verified here, so its messages must not fail the test.
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            var store = HierarchyColorStoreFile.LoadOrCreate(TempFile);
            try
            {
                Assert.IsNotNull(store);
                Assert.Greater(store.Presets.Count, 0);
                Assert.IsFalse(File.Exists(TempFile), "The unreadable file should have been moved aside.");
                Assert.IsTrue(File.Exists(TempFile + ".corrupt"));
            }
            finally
            {
                Object.DestroyImmediate(store);
            }
        }
    }
}
