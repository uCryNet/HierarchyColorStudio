using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CryNet.HierarchyColorStudio.Tests
{
    /// <summary>Assigning, changing, clearing, multi-selection, scopes and undo/redo.</summary>
    internal sealed class HierarchyColorServiceTests
    {
        private readonly StoreIsolation m_Isolation = new StoreIsolation();
        private readonly List<GameObject> m_Created = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            m_Isolation.Begin();
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

        private GameObject CreateObject(string name, GameObject parent = null)
        {
            var created = new GameObject(name);
            if (parent != null)
                created.transform.SetParent(parent.transform);
            m_Created.Add(created);
            return created;
        }

        [Test]
        public void Assign_MakesTheColorReadable()
        {
            var target = CreateObject("Target");

            HierarchyColorService.Assign(target, Color.red);

            Assert.IsTrue(HierarchyColorService.TryGetColor(target, out Color stored));
            Assert.AreEqual((Color32)Color.red, (Color32)stored);
            Assert.AreEqual(1, HierarchyColorService.StoredAssignmentCount);
        }

        [Test]
        public void Assign_Twice_UpdatesInsteadOfDuplicating()
        {
            var target = CreateObject("Target");

            HierarchyColorService.Assign(target, Color.red);
            HierarchyColorService.Assign(target, Color.blue);

            Assert.AreEqual(1, HierarchyColorService.StoredAssignmentCount);
            Assert.IsTrue(HierarchyColorService.TryGetColor(target, out Color stored));
            Assert.AreEqual((Color32)Color.blue, (Color32)stored);
        }

        [Test]
        public void Clear_RemovesTheColor()
        {
            var target = CreateObject("Target");
            HierarchyColorService.Assign(target, Color.red);

            int removed = HierarchyColorService.Clear(new[] { target });

            Assert.AreEqual(1, removed);
            Assert.IsFalse(HierarchyColorService.TryGetColor(target, out _));
            Assert.AreEqual(0, HierarchyColorService.StoredAssignmentCount);
        }

        [Test]
        public void Clear_OnUncoloredObject_ChangesNothing()
        {
            var target = CreateObject("Target");

            Assert.AreEqual(0, HierarchyColorService.Clear(new[] { target }));
            Assert.AreEqual(0, HierarchyColorService.StoredAssignmentCount);
        }

        [Test]
        public void Assign_ToMultipleObjects_ColorsAllOfThem()
        {
            var targets = new[] { CreateObject("A"), CreateObject("B"), CreateObject("C") };

            int changed = HierarchyColorService.Assign(targets, Color.green);

            Assert.AreEqual(3, changed);
            for (int i = 0; i < targets.Length; i++)
            {
                Assert.IsTrue(HierarchyColorService.TryGetColor(targets[i], out Color stored),
                    "Expected {0} to be colored.", targets[i].name);
                Assert.AreEqual((Color32)Color.green, (Color32)stored);
            }
        }

        [Test]
        public void Assign_ToMultipleObjects_IsASingleUndoStep()
        {
            var targets = new[] { CreateObject("A"), CreateObject("B"), CreateObject("C") };
            HierarchyColorService.Assign(targets, Color.green);

            Undo.PerformUndo();

            Assert.AreEqual(0, HierarchyColorService.StoredAssignmentCount,
                "One multi-selection assignment must undo in one step.");
        }

        [Test]
        public void Undo_RestoresThePreviousColor()
        {
            var target = CreateObject("Target");
            HierarchyColorService.Assign(target, Color.red);
            HierarchyColorService.Assign(target, Color.blue);

            Undo.PerformUndo();

            Assert.IsTrue(HierarchyColorService.TryGetColor(target, out Color stored));
            Assert.AreEqual((Color32)Color.red, (Color32)stored);
        }

        [Test]
        public void Redo_ReappliesTheUndoneColor()
        {
            var target = CreateObject("Target");
            HierarchyColorService.Assign(target, Color.red);

            Undo.PerformUndo();
            Assert.IsFalse(HierarchyColorService.TryGetColor(target, out _));

            Undo.PerformRedo();

            Assert.IsTrue(HierarchyColorService.TryGetColor(target, out Color stored));
            Assert.AreEqual((Color32)Color.red, (Color32)stored);
        }

        [Test]
        public void Undo_RestoresAClearedColor()
        {
            var target = CreateObject("Target");
            HierarchyColorService.Assign(target, Color.red);
            HierarchyColorService.Clear(new[] { target });

            Undo.PerformUndo();

            Assert.IsTrue(HierarchyColorService.TryGetColor(target, out Color stored));
            Assert.AreEqual((Color32)Color.red, (Color32)stored);
        }

        [Test]
        public void ApplyScope_DirectChildren_SkipsGrandchildren()
        {
            var root = CreateObject("Root");
            var child = CreateObject("Child", root);
            var grandChild = CreateObject("GrandChild", child);

            HierarchyColorService.Assign(new[] { root }, Color.yellow, null, ApplyScope.DirectChildren);

            Assert.IsTrue(HierarchyColorService.TryGetColor(root, out _));
            Assert.IsTrue(HierarchyColorService.TryGetColor(child, out _));
            Assert.IsFalse(HierarchyColorService.TryGetColor(grandChild, out _));
        }

        [Test]
        public void ApplyScope_AllDescendants_ColorsTheWholeBranch()
        {
            var root = CreateObject("Root");
            var child = CreateObject("Child", root);
            var grandChild = CreateObject("GrandChild", child);

            HierarchyColorService.Assign(new[] { root }, Color.yellow, null, ApplyScope.AllDescendants);

            Assert.IsTrue(HierarchyColorService.TryGetColor(root, out _));
            Assert.IsTrue(HierarchyColorService.TryGetColor(child, out _));
            Assert.IsTrue(HierarchyColorService.TryGetColor(grandChild, out _));
        }

        [Test]
        public void Assign_OverlappingSelectionAndChildren_DoesNotDuplicateEntries()
        {
            var root = CreateObject("Root");
            var child = CreateObject("Child", root);

            HierarchyColorService.Assign(new[] { root, child }, Color.cyan, null, ApplyScope.AllDescendants);

            Assert.AreEqual(2, HierarchyColorService.StoredAssignmentCount);
        }

        [Test]
        public void ClearAll_RemovesEveryAssignment()
        {
            HierarchyColorService.Assign(new[] { CreateObject("A"), CreateObject("B") }, Color.red);

            int removed = HierarchyColorService.ClearAll();

            Assert.AreEqual(2, removed);
            Assert.AreEqual(0, HierarchyColorService.StoredAssignmentCount);
        }

        [Test]
        public void ClearAll_IsUndoable()
        {
            HierarchyColorService.Assign(new[] { CreateObject("A"), CreateObject("B") }, Color.red);
            HierarchyColorService.ClearAll();

            Undo.PerformUndo();

            Assert.AreEqual(2, HierarchyColorService.StoredAssignmentCount);
        }

        [Test]
        public void AnyHasColor_ReflectsTheSelection()
        {
            var colored = CreateObject("Colored");
            var plain = CreateObject("Plain");
            HierarchyColorService.Assign(colored, Color.red);

            Assert.IsTrue(HierarchyColorService.AnyHasColor(new[] { plain, colored }));
            Assert.IsFalse(HierarchyColorService.AnyHasColor(new[] { plain }));
            Assert.IsFalse(HierarchyColorService.AnyHasColor(null));
        }

        [Test]
        public void Assign_IgnoresNullTargetsWithoutThrowing()
        {
            var target = CreateObject("Target");

            Assert.AreEqual(1, HierarchyColorService.Assign(new[] { null, target, null }, Color.red));
            Assert.AreEqual(0, HierarchyColorService.Assign(new GameObject[] { null }, Color.red));
            Assert.AreEqual(0, HierarchyColorService.Assign((IReadOnlyList<GameObject>)null, Color.red));
            Assert.DoesNotThrow(() => HierarchyColorService.Assign((GameObject)null, Color.red));
        }

        [Test]
        public void RemoveMissingAssignments_DropsEntriesWhoseObjectIsGone()
        {
            var target = CreateObject("Doomed");
            HierarchyColorService.Assign(target, Color.red);
            Assert.AreEqual(1, HierarchyColorService.StoredAssignmentCount);

            m_Created.Remove(target);
            Object.DestroyImmediate(target);
            HierarchyColorService.InvalidateAll();

            Assert.AreEqual(1, HierarchyColorService.RemoveMissingAssignments());
            Assert.AreEqual(0, HierarchyColorService.StoredAssignmentCount);
        }

        [Test]
        public void SessionScopedAssignment_IsReportedForUnsavedObjects()
        {
            var target = CreateObject("Unsaved");
            HierarchyColorService.Assign(target, Color.red);

            Assert.IsTrue(HierarchyColorService.HasSessionScopedAssignments,
                "An object that has never been saved must use a session-scoped key.");
            Assert.IsTrue(HierarchyColorService.Store.Assignments[0].IsSessionScoped);
        }
    }
}
