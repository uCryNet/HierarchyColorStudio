using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CryNet.HierarchyColorStudio.Tests
{
    /// <summary>Resolution of Unity built-in icons: the catalog, silent misses, name handling and handles.</summary>
    internal sealed class BuiltinIconsTests
    {
        private readonly List<string> m_Logs = new List<string>();

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            m_Logs.Add(type + ": " + condition);
        }

        [Test]
        public void EveryCatalogIcon_ExistsInThisUnityVersion()
        {
            BuiltinIcons.ClearCache();
            var missing = new List<string>();
            foreach (var entry in BuiltinIcons.Catalog)
            {
                if (BuiltinIcons.GetTexture(entry.Name) == null)
                    missing.Add(entry.Name);
            }

            CollectionAssert.IsEmpty(missing, "Catalog icons missing in this Unity version.");
        }

        [Test]
        public void Catalog_HasUniqueNamesAndLabels()
        {
            var names = new HashSet<string>();
            foreach (var entry in BuiltinIcons.Catalog)
            {
                Assert.IsTrue(names.Add(entry.Name.ToLowerInvariant()), "Duplicate icon {0}.", entry.Name);
                Assert.IsFalse(string.IsNullOrEmpty(entry.Label));
                Assert.AreEqual(entry.Name, BuiltinIcons.NormalizeName(entry.Name), "Catalog names must be canonical.");
            }
        }

        [Test]
        public void UnknownIcon_ResolvesToNullWithoutLogging()
        {
            m_Logs.Clear();
            Application.logMessageReceived += OnLog;
            try
            {
                Assert.IsNull(BuiltinIcons.GetTexture("Hierarchy Color Studio Missing Icon " + System.Guid.NewGuid()));
                Assert.IsFalse(BuiltinIcons.Exists("Another Missing Icon"));
            }
            finally
            {
                Application.logMessageReceived -= OnLog;
            }

            CollectionAssert.IsEmpty(m_Logs, "Resolving an unknown icon must not write to the Console.");
        }

        [Test]
        public void ThemeVariant_IsChosenAutomatically()
        {
            BuiltinIcons.ClearCache();
            var texture = BuiltinIcons.GetTexture("Camera Icon");

            Assert.IsNotNull(texture);
            // The dark theme prefers the d_ variant, exactly as Unity does for its own icons.
            Assert.AreEqual(UnityEditor.EditorGUIUtility.isProSkin, texture.name.StartsWith("d_"));
        }

        [TestCase(null, "")]
        [TestCase("", "")]
        [TestCase("   ", "")]
        [TestCase("Camera Icon", "Camera Icon")]
        [TestCase("  Camera Icon  ", "Camera Icon")]
        [TestCase("Camera Icon@2x", "Camera Icon")]
        [TestCase("d_Camera Icon", "d_Camera Icon")]
        public void NormalizeName_TrimsAndDropsTheDensitySuffix(string input, string expected)
        {
            Assert.AreEqual(expected, BuiltinIcons.NormalizeName(input));
        }

        [Test]
        public void Handles_AreStableAndCaseInsensitive()
        {
            int handle = BuiltinIcons.GetHandle("Camera Icon");

            Assert.GreaterOrEqual(handle, 0);
            Assert.AreEqual(handle, BuiltinIcons.GetHandle("camera icon"));
            Assert.AreEqual(handle, BuiltinIcons.GetHandle(" Camera Icon@2x "));
            Assert.AreEqual(-1, BuiltinIcons.GetHandle(string.Empty));
            Assert.IsNull(BuiltinIcons.GetTexture(-1));
            Assert.AreSame(BuiltinIcons.GetTexture("Camera Icon"), BuiltinIcons.GetTexture(handle));
        }

        [Test]
        public void GetLabel_UsesTheCatalogLabel()
        {
            Assert.AreEqual("Camera", BuiltinIcons.GetLabel("Camera Icon"));
            Assert.AreEqual("Some Other Icon", BuiltinIcons.GetLabel("Some Other Icon"));
        }
    }
}
