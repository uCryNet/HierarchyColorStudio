using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CryNet.HierarchyColorStudio.Tests
{
    /// <summary>
    /// Row-level drawing rules that do not need a Hierarchy window: marker geometry, zebra parity, Hierarchy
    /// mode settings and the classification behind Minimal mode and folder and prefab icons.
    /// </summary>
    internal sealed class HierarchyRowTests
    {
        private static readonly Rect Row = new Rect(40f, 48f, 260f, 16f);

        private readonly List<Object> m_Created = new List<Object>();
        private string m_PrefabPath;

        [TearDown]
        public void TearDown()
        {
            for (int i = m_Created.Count - 1; i >= 0; i--)
            {
                if (m_Created[i] != null)
                    Object.DestroyImmediate(m_Created[i]);
            }

            m_Created.Clear();
            if (!string.IsNullOrEmpty(m_PrefabPath))
            {
                AssetDatabase.DeleteAsset(m_PrefabPath);
                m_PrefabPath = null;
            }

            HierarchyRowClassifier.Invalidate();
            MarkerShapes.ReleaseTextures();
        }

        private GameObject Create(string name, GameObject parent = null)
        {
            var created = new GameObject(name);
            if (parent != null)
                created.transform.SetParent(parent.transform);
            m_Created.Add(created);
            return created;
        }

        // The formulas version 1.0 used, written out independently of MarkerShapes.
        private static Rect Version1MarkerRect(MarkerShape shape, float size, MarkerPlacement placement)
        {
            float width = shape == MarkerShape.Bar ? Mathf.Max(3f, size * 0.42f) : size;
            float x = placement == MarkerPlacement.BeforeIcon ? Row.x - width - 1f : Row.xMax - width - 4f;
            return shape == MarkerShape.Bar
                ? new Rect(x, Row.y + 2f, width, Mathf.Max(1f, Row.height - 4f))
                : new Rect(x, Row.y + (Row.height - size) * 0.5f, size, size);
        }

        [Test]
        public void ExistingMarkerShapes_KeepTheirVersion1Geometry(
            [Values(MarkerShape.Dot, MarkerShape.Bar, MarkerShape.Square)] MarkerShape shape,
            [Values(MarkerPlacement.RowEnd, MarkerPlacement.BeforeIcon)] MarkerPlacement placement,
            [Values(4f, 8f, 14f)] float size)
        {
            Assert.AreEqual(Version1MarkerRect(shape, size, placement), MarkerShapes.GetRect(Row, shape, size, placement));
        }

        [Test]
        public void NewMarkerShapes_OccupyASquareCentredOnTheRow(
            [Values(MarkerShape.Ring, MarkerShape.Diamond, MarkerShape.Triangle)] MarkerShape shape)
        {
            var rect = MarkerShapes.GetRect(Row, shape, 10f, MarkerPlacement.RowEnd);

            Assert.AreEqual(10f, rect.width);
            Assert.AreEqual(10f, rect.height);
            Assert.AreEqual(Row.center.y, rect.center.y, 0.0001f);
            Assert.AreEqual(Row.xMax - MarkerShapes.EdgePadding, rect.xMax, 0.0001f);
        }

        [Test]
        public void ZebraParity_AlternatesFromTheFirstRow()
        {
            Assert.IsFalse(HierarchyRowRenderer.IsStripedRow(new Rect(0f, 0f, 100f, 16f)));
            Assert.IsTrue(HierarchyRowRenderer.IsStripedRow(new Rect(0f, 16f, 100f, 16f)));
            Assert.IsFalse(HierarchyRowRenderer.IsStripedRow(new Rect(0f, 32f, 100f, 16f)));
            Assert.IsTrue(HierarchyRowRenderer.IsStripedRow(new Rect(0f, 48.4f, 100f, 16f)), "Fractional offsets round to the row.");
            Assert.IsFalse(HierarchyRowRenderer.IsStripedRow(new Rect(0f, 48f, 100f, 0f)));
        }

        [Test]
        public void HierarchyModes_AreIndependentFlags()
        {
            var settings = new AppearanceSettings();
            Assert.IsFalse(settings.AffectsEveryRow, "The default appearance must leave unstyled rows alone.");

            settings.Mode = HierarchyMode.Zebra;
            Assert.IsTrue(settings.ZebraEnabled);
            Assert.IsFalse(settings.MinimalEnabled);
            Assert.IsTrue(settings.AffectsEveryRow);

            settings.Mode = HierarchyMode.Minimal | HierarchyMode.Zebra;
            Assert.IsTrue(settings.ZebraEnabled && settings.MinimalEnabled);

            settings.Mode = (HierarchyMode)0xFF;
            settings.Sanitize();
            Assert.AreEqual(HierarchyMode.Minimal | HierarchyMode.Zebra, settings.Mode, "Unknown mode bits are dropped.");
        }

        [Test]
        public void FolderAndPrefabIcons_AreOptIn()
        {
            var settings = new AppearanceSettings();
            Assert.AreEqual(string.Empty, settings.FolderIcon);
            Assert.AreEqual(string.Empty, settings.PrefabIcon);

            settings.FolderIcon = " Folder Icon@2x ";
            Assert.AreEqual("Folder Icon", settings.FolderIcon);
            Assert.IsTrue(settings.AffectsEveryRow);
        }

        [Test]
        public void ZebraOpacity_IsClamped()
        {
            var settings = new AppearanceSettings { ZebraOpacity = 5f };
            Assert.AreEqual(AppearanceSettings.MaxZebraOpacity, settings.ZebraOpacity, 0.0001f);

            settings.ZebraOpacity = -1f;
            Assert.AreEqual(AppearanceSettings.MinZebraOpacity, settings.ZebraOpacity, 0.0001f);
        }

        [Test]
        public void Classifier_RecognisesPlainObjectsAndFolders()
        {
            var plain = Create("Plain");
            var folder = Create("Environment");
            Create("Child", folder);
            var withComponent = Create("Camera");
            withComponent.AddComponent<Camera>();
            Create("Child", withComponent);

            Assert.AreEqual(RowKind.GameObject, HierarchyRowClassifier.ClassifyUncached(plain).Kind,
                "An empty object without children is not a folder.");
            Assert.AreEqual(RowKind.Folder, HierarchyRowClassifier.ClassifyUncached(folder).Kind);
            Assert.AreEqual(RowKind.GameObject, HierarchyRowClassifier.ClassifyUncached(withComponent).Kind,
                "An object with a component is not a folder, even with children.");
            Assert.AreEqual(RowKind.Keep, HierarchyRowClassifier.ClassifyUncached(null).Kind,
                "Rows that are not GameObjects, such as scene headers, are left alone.");
        }

        [Test]
        public void Classifier_KeepsIconsChosenInTheInspector()
        {
            var target = Create("Custom");
            Create("Child", target);
            EditorGUIUtility.SetIconForObject(target, (Texture2D)BuiltinIcons.GetTexture("Favorite Icon"));

            Assert.AreEqual(RowKind.Keep, HierarchyRowClassifier.ClassifyUncached(target).Kind);
        }

        [Test]
        public void Classifier_ReportsInactiveObjects()
        {
            var target = Create("Inactive");
            target.SetActive(false);

            Assert.IsTrue(HierarchyRowClassifier.ClassifyUncached(target).Inactive);
        }

        [Test]
        public void Classifier_RecognisesPrefabInstanceRoots()
        {
            var source = Create("PrefabSource");
            Create("Child", source);
            m_PrefabPath = AssetDatabase.GenerateUniqueAssetPath("Assets/HierarchyColorStudioClassifierTest.prefab");
            var prefab = PrefabUtility.SaveAsPrefabAsset(source, m_PrefabPath);
            Assert.IsNotNull(prefab);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            m_Created.Add(instance);

            Assert.AreEqual(RowKind.PrefabRoot, HierarchyRowClassifier.ClassifyUncached(instance).Kind,
                "A prefab root is a prefab, even though it would otherwise qualify as a folder.");
            Assert.AreEqual(RowKind.GameObject,
                HierarchyRowClassifier.ClassifyUncached(instance.transform.GetChild(0).gameObject).Kind);
        }

        [Test]
        public void Classifier_CachesUntilInvalidated()
        {
            var target = Create("Cached");
            var rowId = RowId.Of(target);

            Assert.AreEqual(RowKind.GameObject, HierarchyRowClassifier.Classify(rowId).Kind);
            Create("Child", target);
            Assert.AreEqual(RowKind.GameObject, HierarchyRowClassifier.Classify(rowId).Kind, "Cached until invalidated.");

            HierarchyRowClassifier.Invalidate();
            Assert.AreEqual(RowKind.Folder, HierarchyRowClassifier.Classify(rowId).Kind);
        }
    }
}
