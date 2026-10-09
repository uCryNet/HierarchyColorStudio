using System;
using UnityEditor;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Thin wrapper around Unity's native Undo system.
    /// Every mutation of the store is recorded as a complete-object snapshot, which keeps a bulk edit
    /// (for example applying one color to a whole multi-selection) inside a single undo step.
    /// </summary>
    internal static class UndoScope
    {
        private const double ContinuousEditTimeout = 0.75;

        private static int s_ContinuousGroup = -1;
        private static double s_LastContinuousEdit;

        /// <summary>Records the current state of the store as one undo step.</summary>
        /// <param name="store">Store to snapshot.</param>
        /// <param name="label">Undo step name shown in the Edit menu.</param>
        /// <remarks>
        /// A discrete step ends any continuous interaction in progress. Otherwise a drag that resumed within
        /// the collapse timeout would fold this step into the drag's group, and undoing the drag would undo
        /// this step with it.
        /// </remarks>
        internal static void Record(HierarchyColorStore store, string label)
        {
            if (store == null)
                return;

            EndContinuous();
            try
            {
                Undo.IncrementCurrentGroup();
                Undo.RegisterCompleteObjectUndo(store, label);
                Undo.SetCurrentGroupName(label);
            }
            catch (Exception exception)
            {
                StudioLog.ExceptionOnce("undo-record", exception, "Could not register an undo step.");
            }
        }

        /// <summary>
        /// Records a state snapshot that belongs to a continuous interaction such as dragging a color
        /// picker. Consecutive calls collapse into a single undo step until the interaction pauses.
        /// </summary>
        /// <param name="store">Store to snapshot.</param>
        /// <param name="label">Undo step name shown in the Edit menu.</param>
        internal static void RecordContinuous(HierarchyColorStore store, string label)
        {
            if (store == null)
                return;

            double now = EditorApplication.timeSinceStartup;
            bool startNewGroup = s_ContinuousGroup < 0 || now - s_LastContinuousEdit > ContinuousEditTimeout;
            s_LastContinuousEdit = now;

            try
            {
                if (startNewGroup)
                {
                    Undo.IncrementCurrentGroup();
                    s_ContinuousGroup = Undo.GetCurrentGroup();
                }

                Undo.RegisterCompleteObjectUndo(store, label);
                Undo.SetCurrentGroupName(label);
                Undo.CollapseUndoOperations(s_ContinuousGroup);
            }
            catch (Exception exception)
            {
                s_ContinuousGroup = -1;
                StudioLog.ExceptionOnce("undo-record-continuous", exception, "Could not register an undo step.");
            }
        }

        /// <summary>Ends the current continuous interaction so the next edit starts a new undo step.</summary>
        internal static void EndContinuous()
        {
            s_ContinuousGroup = -1;
        }
    }
}
