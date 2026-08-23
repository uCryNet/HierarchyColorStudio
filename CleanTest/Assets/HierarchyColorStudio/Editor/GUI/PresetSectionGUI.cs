using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Draws the preset editor: search, per-row editing, ordering, creation and deletion.
    /// </summary>
    /// <remarks>
    /// Ordering uses explicit move buttons rather than drag reordering. A drag reorder mutates the list
    /// before any callback fires, which would make the undo snapshot describe the already-changed state;
    /// move buttons let every edit be snapshotted before it happens.
    /// </remarks>
    internal sealed class PresetSectionGUI
    {
        private const float SwatchWidth = 34f;
        private const float HexWidth = 78f;
        private const float ApplyWidth = 52f;
        private const float IconButtonWidth = 22f;
        private const float RowSpacing = 2f;

        private static readonly Color InvalidInputColor = new Color(0.85f, 0.35f, 0.3f, 0.9f);

        private enum PendingOperation
        {
            None,
            Add,
            Remove,
            Move,
            RestoreDefaults
        }

        private readonly List<int> m_VisibleIndices = new List<int>(32);

        private string m_Search = string.Empty;
        private string m_HexEditKey;
        private string m_HexEditValue;
        private PendingOperation m_Pending;
        private int m_PendingFrom;
        private int m_PendingTo;

        /// <summary>Draws the preset section.</summary>
        /// <param name="store">Store whose presets are edited.</param>
        /// <param name="selection">Current GameObject selection, used by the per-row Apply button.</param>
        internal void Draw(HierarchyColorStore store, IReadOnlyList<GameObject> selection)
        {
            if (store == null)
                return;

            DrawSearchField();

            var presets = store.Presets;
            BuildVisibleIndices(presets);

            if (m_VisibleIndices.Count == 0)
            {
                EditorGUILayout.LabelField(UiStrings.HintNoPresets, StudioStyles.Hint);
            }
            else
            {
                for (int i = 0; i < m_VisibleIndices.Count; i++)
                    DrawPresetRow(store, presets, m_VisibleIndices[i], selection);
            }

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(UiStrings.ButtonAddPreset, GUILayout.Width(110f)))
                    m_Pending = PendingOperation.Add;

                if (GUILayout.Button(UiStrings.ButtonResetPresets, EditorStyles.miniButton, GUILayout.Width(170f)))
                    m_Pending = PendingOperation.RestoreDefaults;

                GUILayout.FlexibleSpace();
            }

            if (!string.IsNullOrEmpty(m_Search))
                EditorGUILayout.LabelField(UiStrings.HintReorderDisabled, StudioStyles.Hint);

            ApplyPendingOperation(store);
        }

        private void ApplyPendingOperation(HierarchyColorStore store)
        {
            var pending = m_Pending;
            m_Pending = PendingOperation.None;

            switch (pending)
            {
                case PendingOperation.Add:
                    UndoScope.Record(store, UiStrings.UndoEditPresets);
                    store.Presets.Add(new ColorPreset("New Preset", LastUsedColor.Color));
                    break;
                case PendingOperation.Remove:
                    if (m_PendingFrom < 0 || m_PendingFrom >= store.Presets.Count)
                        return;
                    UndoScope.Record(store, UiStrings.UndoEditPresets);
                    store.Presets.RemoveAt(m_PendingFrom);
                    break;
                case PendingOperation.Move:
                {
                    var presets = store.Presets;
                    if (m_PendingFrom < 0 || m_PendingFrom >= presets.Count ||
                        m_PendingTo < 0 || m_PendingTo >= presets.Count)
                        return;
                    UndoScope.Record(store, UiStrings.UndoEditPresets);
                    var moved = presets[m_PendingFrom];
                    presets.RemoveAt(m_PendingFrom);
                    presets.Insert(m_PendingTo, moved);
                    break;
                }
                case PendingOperation.RestoreDefaults:
                    UndoScope.Record(store, UiStrings.UndoEditPresets);
                    store.ResetPresets();
                    break;
                default:
                    return;
            }

            HierarchyColorService.NotifySettingsChanged();
        }

        private void DrawSearchField()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(UiStrings.LabelSearch, GUILayout.Width(48f));
                m_Search = EditorGUILayout.TextField(m_Search);
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(m_Search)))
                {
                    if (GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(IconButtonWidth)))
                    {
                        m_Search = string.Empty;
                        GUIUtility.keyboardControl = 0;
                    }
                }
            }
        }

        private void BuildVisibleIndices(List<ColorPreset> presets)
        {
            m_VisibleIndices.Clear();
            for (int i = 0; i < presets.Count; i++)
            {
                var preset = presets[i];
                if (preset == null)
                    continue;

                if (string.IsNullOrEmpty(m_Search) ||
                    preset.Name.IndexOf(m_Search, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    ColorHex.ToHex(preset.Color).IndexOf(m_Search, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    m_VisibleIndices.Add(i);
                }
            }
        }

        private void DrawPresetRow(HierarchyColorStore store, List<ColorPreset> presets, int index,
            IReadOnlyList<GameObject> selection)
        {
            var preset = presets[index];
            bool reorderable = string.IsNullOrEmpty(m_Search);

            using (new EditorGUILayout.HorizontalScope())
            {
                var swatchRect = GUILayoutUtility.GetRect(SwatchWidth, EditorGUIUtility.singleLineHeight,
                    GUILayout.Width(SwatchWidth));
                swatchRect.y += 1f;
                swatchRect.height -= 2f;

                EditorGUI.BeginChangeCheck();
                Color newColor = EditorGUI.ColorField(swatchRect, GUIContent.none, preset.Color, true, false, false);
                if (EditorGUI.EndChangeCheck())
                {
                    UndoScope.RecordContinuous(store, UiStrings.UndoEditPresets);
                    preset.Color = newColor;
                    HierarchyColorService.NotifySettingsChanged();
                }

                EditorGUI.BeginChangeCheck();
                string newName = EditorGUILayout.TextField(preset.Name);
                if (EditorGUI.EndChangeCheck())
                {
                    UndoScope.RecordContinuous(store, UiStrings.UndoEditPresets);
                    preset.Name = newName;
                    HierarchyColorService.NotifySettingsChanged();
                }

                DrawHexField(store, preset);

                using (new EditorGUI.DisabledScope(selection == null || selection.Count == 0))
                {
                    if (GUILayout.Button(UiStrings.ButtonApply, EditorStyles.miniButton, GUILayout.Width(ApplyWidth)))
                        ApplyPreset(preset, selection);
                }

                using (new EditorGUI.DisabledScope(!reorderable || index == 0))
                {
                    if (GUILayout.Button("▲", EditorStyles.miniButtonLeft, GUILayout.Width(IconButtonWidth)))
                        QueueMove(index, index - 1);
                }

                using (new EditorGUI.DisabledScope(!reorderable || index >= presets.Count - 1))
                {
                    if (GUILayout.Button("▼", EditorStyles.miniButtonMid, GUILayout.Width(IconButtonWidth)))
                        QueueMove(index, index + 1);
                }

                if (GUILayout.Button("×", EditorStyles.miniButtonRight, GUILayout.Width(IconButtonWidth)))
                {
                    m_Pending = PendingOperation.Remove;
                    m_PendingFrom = index;
                }
            }

            GUILayout.Space(RowSpacing);
        }

        private void DrawHexField(HierarchyColorStore store, ColorPreset preset)
        {
            string editKey = preset.Id;
            bool editing = m_HexEditKey == editKey;
            string displayed = editing ? m_HexEditValue : ColorHex.ToDisplayHex(preset.Color);

            EditorGUI.BeginChangeCheck();
            string typed = EditorGUILayout.TextField(displayed, GUILayout.Width(HexWidth));
            if (EditorGUI.EndChangeCheck())
            {
                m_HexEditKey = editKey;
                m_HexEditValue = typed;

                if (ColorHex.TryParse(typed, out Color32 parsed))
                {
                    UndoScope.RecordContinuous(store, UiStrings.UndoEditPresets);
                    preset.Color = parsed;
                    HierarchyColorService.NotifySettingsChanged();
                }
            }

            if (editing && Event.current.type == EventType.Repaint && !ColorHex.TryParse(m_HexEditValue, out _))
                StudioStyles.DrawOutline(GUILayoutUtility.GetLastRect(), InvalidInputColor, 1f);
        }

        private static void ApplyPreset(ColorPreset preset, IReadOnlyList<GameObject> selection)
        {
            if (preset == null || selection == null || selection.Count == 0)
                return;

            HierarchyColorService.Assign(selection, preset.Color, preset.Id, LastUsedColor.Scope);
        }

        private void QueueMove(int from, int to)
        {
            m_Pending = PendingOperation.Move;
            m_PendingFrom = from;
            m_PendingTo = to;
        }
    }
}
