using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Menu and keyboard entry points.
    /// </summary>
    /// <remarks>
    /// Unity invokes a <c>GameObject/</c> menu item once per selected object. Every command is therefore
    /// routed through <see cref="RunOnce"/>, which collapses one click into a single operation so a
    /// multi-selection produces exactly one undo step.
    /// Only the window shortcut has a default binding. The remaining commands are registered without one
    /// so they cannot collide with a Unity default; users assign them in Edit &gt; Shortcuts.
    /// </remarks>
    internal static class HierarchyColorMenus
    {
        private const int GameObjectMenuPriority = 20;
        private const int ToolsMenuPriority = 100;

        private static bool s_Executing;

        [MenuItem(UiStrings.MenuRootGameObject + UiStrings.MenuSetColor, false, GameObjectMenuPriority)]
        private static void SetColorFromHierarchy()
        {
            RunOnce(OpenPalette);
        }

        [MenuItem(UiStrings.MenuRootGameObject + UiStrings.MenuSetColor, true)]
        private static bool SetColorFromHierarchyValidate()
        {
            return Selection.gameObjects.Length > 0;
        }

        [MenuItem(UiStrings.MenuRootGameObject + UiStrings.MenuApplyLastColor, false, GameObjectMenuPriority + 1)]
        private static void ApplyLastColorFromHierarchy()
        {
            RunOnce(ApplyLastColor);
        }

        [MenuItem(UiStrings.MenuRootGameObject + UiStrings.MenuApplyLastColor, true)]
        private static bool ApplyLastColorFromHierarchyValidate()
        {
            return Selection.gameObjects.Length > 0;
        }

        [MenuItem(UiStrings.MenuRootGameObject + UiStrings.MenuClearColor, false, GameObjectMenuPriority + 2)]
        private static void ClearColorFromHierarchy()
        {
            RunOnce(ClearColor);
        }

        [MenuItem(UiStrings.MenuRootGameObject + UiStrings.MenuClearColor, true)]
        private static bool ClearColorFromHierarchyValidate()
        {
            return HierarchyColorService.AnyHasColor(Selection.gameObjects);
        }

        [MenuItem(UiStrings.MenuRootGameObject + UiStrings.MenuOpenStudio, false, GameObjectMenuPriority + 3)]
        private static void OpenStudioFromHierarchy()
        {
            RunOnce(HierarchyColorStudioWindow.Open);
        }

        [MenuItem(UiStrings.MenuRootTools + UiStrings.MenuOpenWindow, false, ToolsMenuPriority)]
        private static void OpenWindowFromTools()
        {
            HierarchyColorStudioWindow.Open();
        }

        [MenuItem(UiStrings.MenuRootTools + UiStrings.MenuSettings, false, ToolsMenuPriority + 1)]
        private static void OpenSettingsFromTools()
        {
            SettingsService.OpenProjectSettings(UiStrings.ProjectSettingsPath);
        }

        [MenuItem(UiStrings.MenuRootTools + UiStrings.MenuExport, false, ToolsMenuPriority + 20)]
        private static void ExportFromTools()
        {
            ColorTransfer.ExportWithDialog();
        }

        [MenuItem(UiStrings.MenuRootTools + UiStrings.MenuImport, false, ToolsMenuPriority + 21)]
        private static void ImportFromTools()
        {
            ColorTransfer.ImportWithDialog();
        }

        [MenuItem(UiStrings.MenuRootTools + UiStrings.MenuClearAll, false, ToolsMenuPriority + 22)]
        private static void ClearAllFromTools()
        {
            MaintenanceActions.ClearAllWithConfirmation();
        }

        [MenuItem(UiStrings.MenuRootTools + UiStrings.MenuClearAll, true)]
        private static bool ClearAllFromToolsValidate()
        {
            return HierarchyColorService.StoredAssignmentCount > 0;
        }

        [MenuItem(UiStrings.MenuRootTools + UiStrings.MenuDocumentation, false, ToolsMenuPriority + 40)]
        private static void OpenDocumentationFromTools()
        {
            DocumentationLocator.Open();
        }

        [Shortcut(UiStrings.ShortcutCategory + "Open Color Studio", null, KeyCode.H,
            ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        private static void OpenStudioShortcut()
        {
            HierarchyColorStudioWindow.Open();
        }

        [Shortcut(UiStrings.ShortcutCategory + "Set Color")]
        private static void SetColorShortcut()
        {
            OpenPalette();
        }

        [Shortcut(UiStrings.ShortcutCategory + "Apply Last Color")]
        private static void ApplyLastColorShortcut()
        {
            ApplyLastColor();
        }

        [Shortcut(UiStrings.ShortcutCategory + "Clear Color")]
        private static void ClearColorShortcut()
        {
            ClearColor();
        }

        private static void OpenPalette()
        {
            var targets = Selection.gameObjects;
            if (targets.Length == 0)
                return;

            Vector2 anchor = HierarchyRowRenderer.HasContextClickPosition
                ? HierarchyRowRenderer.LastContextClickScreenPosition
                : DefaultAnchor();

            ColorPaletteWindow.ShowAt(anchor, targets);
        }

        private static void ApplyLastColor()
        {
            var targets = Selection.gameObjects;
            if (targets.Length == 0)
                return;

            HierarchyColorService.Assign(targets, LastUsedColor.Color, LastUsedColor.PresetId, LastUsedColor.Scope);
        }

        private static void ClearColor()
        {
            var targets = Selection.gameObjects;
            if (targets.Length == 0)
                return;

            HierarchyColorService.Clear(targets, LastUsedColor.Scope);
        }

        private static Vector2 DefaultAnchor()
        {
            var focused = EditorWindow.focusedWindow;
            if (focused != null)
                return new Vector2(focused.position.x + 40f, focused.position.y + 60f);

            return new Vector2(200f, 200f);
        }

        private static void RunOnce(System.Action action)
        {
            if (s_Executing)
                return;

            s_Executing = true;
            try
            {
                action();
            }
            finally
            {
                EditorApplication.delayCall += ResetExecutionGuard;
            }
        }

        private static void ResetExecutionGuard()
        {
            s_Executing = false;
            EditorApplication.delayCall -= ResetExecutionGuard;
        }
    }
}
