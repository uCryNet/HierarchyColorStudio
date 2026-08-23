using UnityEditor;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Destructive operations, each guarded by a confirmation dialog.
    /// </summary>
    internal static class MaintenanceActions
    {
        /// <summary>Removes every stored color after asking for confirmation.</summary>
        internal static void ClearAllWithConfirmation()
        {
            if (HierarchyColorService.StoredAssignmentCount == 0)
                return;

            if (!EditorUtility.DisplayDialog(UiStrings.DialogTitleClearAll, UiStrings.DialogBodyClearAll,
                    UiStrings.DialogOk, UiStrings.DialogCancel))
                return;

            HierarchyColorService.ClearAll();
        }

        /// <summary>Restores settings, presets and assignments to the factory defaults after confirmation.</summary>
        internal static void FactoryResetWithConfirmation()
        {
            if (!EditorUtility.DisplayDialog(UiStrings.DialogTitleFactoryReset, UiStrings.DialogBodyFactoryReset,
                    UiStrings.DialogOk, UiStrings.DialogCancel))
                return;

            HierarchyColorStoreProvider.ResetEverything();
            StudioLog.ResetDeduplication();
            HierarchyRowRenderer.ResetFailureState();
            HierarchyColorService.InvalidateAll();
        }
    }
}
