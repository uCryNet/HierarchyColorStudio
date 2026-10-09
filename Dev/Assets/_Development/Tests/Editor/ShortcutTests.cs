using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace CryNet.HierarchyColorStudio.Tests
{
    /// <summary>
    /// The plugin's shortcuts as Unity's Shortcut Manager sees them: identifiers, default bindings, and the
    /// absence of collisions with every other shortcut registered in this project.
    /// </summary>
    internal sealed class ShortcutTests
    {
        private static IShortcutManager Manager => ShortcutManager.instance;

        private static KeyCombination Combination(string id)
        {
            var sequence = Manager.GetShortcutBinding(id).keyCombinationSequence.ToList();
            Assert.AreEqual(1, sequence.Count, "{0} should be bound to a single key combination.", id);
            return sequence[0];
        }

        private static void IgnoreIfCustomised(string id)
        {
            // Tests run against the developer's active shortcut profile; a deliberate rebinding is not a defect.
            if (Manager.IsShortcutOverridden(id))
                Assert.Ignore(id + " is customised in the active shortcut profile.");
        }

        [Test]
        public void EveryCommand_IsRegisteredUnderItsStableIdentifier()
        {
            var ids = new HashSet<string>(Manager.GetAvailableShortcutIds());

            Assert.IsTrue(ids.Contains(UiStrings.ShortcutOpenStudio));
            Assert.IsTrue(ids.Contains(UiStrings.ShortcutSetColor));
            Assert.IsTrue(ids.Contains(UiStrings.ShortcutApplyLastColor));
            Assert.IsTrue(ids.Contains(UiStrings.ShortcutClearColor));
            Assert.AreEqual("Hierarchy Color Studio/Set Color", UiStrings.ShortcutSetColor,
                "Identifiers are stored in users' shortcut profiles and must never change.");
            Assert.AreEqual("Hierarchy Color Studio/Clear Color", UiStrings.ShortcutClearColor);
        }

        [Test]
        public void SetColor_DefaultsToAltShiftC()
        {
            IgnoreIfCustomised(UiStrings.ShortcutSetColor);
            var combination = Combination(UiStrings.ShortcutSetColor);

            Assert.AreEqual(KeyCode.C, combination.keyCode);
            Assert.AreEqual(ShortcutModifiers.Alt | ShortcutModifiers.Shift, combination.modifiers);
        }

        [Test]
        public void ClearColor_DefaultsToAltShiftBackspace()
        {
            IgnoreIfCustomised(UiStrings.ShortcutClearColor);
            var combination = Combination(UiStrings.ShortcutClearColor);

            Assert.AreEqual(KeyCode.Backspace, combination.keyCode);
            Assert.AreEqual(ShortcutModifiers.Alt | ShortcutModifiers.Shift, combination.modifiers);
        }

        [Test]
        public void ApplyLastColor_StaysUnbound()
        {
            IgnoreIfCustomised(UiStrings.ShortcutApplyLastColor);

            Assert.IsFalse(Manager.GetShortcutBinding(UiStrings.ShortcutApplyLastColor).keyCombinationSequence.Any());
        }

        [Test]
        public void DefaultBindings_CollideWithNoOtherShortcut()
        {
            var ours = new[]
            {
                UiStrings.ShortcutOpenStudio, UiStrings.ShortcutSetColor, UiStrings.ShortcutApplyLastColor,
                UiStrings.ShortcutClearColor
            };

            var collisions = new List<string>();
            foreach (string id in ours)
            {
                var binding = Manager.GetShortcutBinding(id);
                if (!binding.keyCombinationSequence.Any())
                    continue;

                foreach (string other in Manager.GetAvailableShortcutIds())
                {
                    if (other == id)
                        continue;
                    if (Manager.GetShortcutBinding(other).Equals(binding))
                        collisions.Add(id + " ↔ " + other + " (" + binding + ")");
                }
            }

            CollectionAssert.IsEmpty(collisions);
        }
    }
}
