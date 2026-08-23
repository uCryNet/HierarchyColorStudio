using NUnit.Framework;
using UnityEngine;

namespace CryNet.HierarchyColorStudio.Tests
{
    /// <summary>Assignment table, preset list and data repair behaviour of the store.</summary>
    internal sealed class HierarchyColorStoreTests
    {
        private HierarchyColorStore m_Store;

        [SetUp]
        public void SetUp()
        {
            m_Store = ScriptableObject.CreateInstance<HierarchyColorStore>();
            m_Store.ResetToDefaults();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Store);
        }

        [Test]
        public void ResetToDefaults_ProvidesPresetsAndNoAssignments()
        {
            Assert.Greater(m_Store.Presets.Count, 0, "Expected factory presets.");
            Assert.AreEqual(0, m_Store.Assignments.Count);
            Assert.AreEqual(HierarchyColorStore.CurrentVersion, m_Store.Version);
        }

        [Test]
        public void SetAssignment_AddsThenUpdatesInPlace()
        {
            m_Store.SetAssignment("key-a", Color.red, string.Empty);
            m_Store.SetAssignment("key-a", Color.green, "preset-1");

            Assert.AreEqual(1, m_Store.Assignments.Count);
            Assert.AreEqual((Color32)Color.green, m_Store.Assignments[0].Color);
            Assert.AreEqual("preset-1", m_Store.Assignments[0].PresetId);
        }

        [Test]
        public void RemoveAssignment_RemovesOnlyTheRequestedKey()
        {
            m_Store.SetAssignment("key-a", Color.red, string.Empty);
            m_Store.SetAssignment("key-b", Color.blue, string.Empty);

            Assert.IsTrue(m_Store.RemoveAssignment("key-a"));
            Assert.IsFalse(m_Store.RemoveAssignment("key-a"));
            Assert.AreEqual(1, m_Store.Assignments.Count);
            Assert.AreEqual("key-b", m_Store.Assignments[0].Key);
        }

        [Test]
        public void IndexOfKey_StaysCorrectAfterRemoval()
        {
            m_Store.SetAssignment("key-a", Color.red, string.Empty);
            m_Store.SetAssignment("key-b", Color.blue, string.Empty);
            m_Store.SetAssignment("key-c", Color.green, string.Empty);

            m_Store.RemoveAssignment("key-a");

            Assert.AreEqual(-1, m_Store.IndexOfKey("key-a"));
            Assert.AreEqual(0, m_Store.IndexOfKey("key-b"));
            Assert.AreEqual(1, m_Store.IndexOfKey("key-c"));
        }

        [Test]
        public void Sanitize_DropsUnusableAndDuplicateRecords()
        {
            m_Store.Assignments.Add(new ColorAssignment("valid", Color.red, string.Empty));
            m_Store.Assignments.Add(new ColorAssignment("valid", Color.blue, string.Empty));
            m_Store.Assignments.Add(new ColorAssignment(string.Empty, Color.blue, string.Empty));
            m_Store.Assignments.Add(null);
            m_Store.InvalidateLookup();

            int repairs = m_Store.Sanitize();

            Assert.AreEqual(3, repairs);
            Assert.AreEqual(1, m_Store.Assignments.Count);
            Assert.AreEqual("valid", m_Store.Assignments[0].Key);
        }

        [Test]
        public void Sanitize_RepairsInvalidPresetColorInsteadOfThrowing()
        {
            var preset = new ColorPreset("Broken", Color.red);
            m_Store.Presets.Clear();
            m_Store.Presets.Add(preset);

            typeof(ColorPreset)
                .GetField("m_Color", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(preset, "not-a-color");

            m_Store.Sanitize();

            Assert.AreEqual((Color32)Color.white, m_Store.Presets[0].Color);
        }

        [Test]
        public void Sanitize_RepairsOutOfRangeVersion()
        {
            typeof(HierarchyColorStore)
                .GetField("m_Version", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(m_Store, 999);

            m_Store.Sanitize();

            Assert.AreEqual(HierarchyColorStore.CurrentVersion, m_Store.Version);
        }

        [Test]
        public void Sanitize_ClampsAppearanceValues()
        {
            m_Store.Appearance.TintOpacity = 5f;
            m_Store.Appearance.MarkerSize = -3f;

            m_Store.Sanitize();

            Assert.AreEqual(AppearanceSettings.MaxTintOpacity, m_Store.Appearance.TintOpacity, 0.0001f);
            Assert.AreEqual(AppearanceSettings.MinMarkerSize, m_Store.Appearance.MarkerSize, 0.0001f);
        }

        [Test]
        public void RemoveSessionScopedAssignments_KeepsPersistentKeys()
        {
            m_Store.SetAssignment("GlobalObjectId_V1-2-abc-123-0", Color.red, string.Empty);
            m_Store.SetAssignment(ColorAssignment.SessionKeyPrefix + "42", Color.blue, string.Empty);

            int removed = m_Store.RemoveSessionScopedAssignments();

            Assert.AreEqual(1, removed);
            Assert.AreEqual(1, m_Store.Assignments.Count);
            Assert.IsFalse(m_Store.Assignments[0].IsSessionScoped);
        }

        [Test]
        public void FindPreset_ReturnsNullForDeletedPreset()
        {
            var preset = m_Store.Presets[0];
            string id = preset.Id;

            m_Store.Presets.RemoveAt(0);

            Assert.IsNull(m_Store.FindPreset(id));
            Assert.IsNull(m_Store.FindPreset(null));
        }
    }
}
