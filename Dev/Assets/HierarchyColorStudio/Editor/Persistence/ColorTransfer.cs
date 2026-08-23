using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Human-readable JSON export and import for color assignments and presets.
    /// </summary>
    /// <remarks>
    /// Exported keys are <see cref="GlobalObjectId"/> strings, which stay valid as long as the scene or
    /// prefab asset they refer to is unchanged. That makes an export usable for sharing a color scheme
    /// with teammates working on the same scenes.
    /// Session-scoped keys are never exported because they are meaningless outside the session.
    /// </remarks>
    internal static class ColorTransfer
    {
        private const int DocumentVersion = 1;
        private const string DocumentKind = "CryNet.HierarchyColorStudio.ColorSet";

        [Serializable]
        private sealed class TransferEntry
        {
            public string key;
            public string color;
            public string preset;
        }

        [Serializable]
        private sealed class TransferPreset
        {
            public string id;
            public string name;
            public string color;
        }

        [Serializable]
        private sealed class TransferDocument
        {
            public string kind = DocumentKind;
            public int version = DocumentVersion;
            public List<TransferPreset> presets = new List<TransferPreset>();
            public List<TransferEntry> assignments = new List<TransferEntry>();
        }

        /// <summary>Asks for a destination file and writes the current colors and presets to it.</summary>
        internal static void ExportWithDialog()
        {
            string path = EditorUtility.SaveFilePanel(UiStrings.FilePanelExportTitle, string.Empty,
                UiStrings.FileDefaultName, UiStrings.FileExtension);
            if (string.IsNullOrEmpty(path))
                return;

            Export(path);
        }

        /// <summary>Writes the current colors and presets to a file.</summary>
        /// <param name="path">Absolute destination path.</param>
        /// <returns><c>true</c> when the file was written.</returns>
        internal static bool Export(string path)
        {
            var store = HierarchyColorService.Store;
            var document = new TransferDocument();

            var presets = store.Presets;
            for (int i = 0; i < presets.Count; i++)
            {
                var preset = presets[i];
                if (preset == null)
                    continue;
                document.presets.Add(new TransferPreset
                {
                    id = preset.Id,
                    name = preset.Name,
                    color = ColorHex.ToHex(preset.Color, true)
                });
            }

            var assignments = store.Assignments;
            for (int i = 0; i < assignments.Count; i++)
            {
                var assignment = assignments[i];
                if (assignment == null || !assignment.IsValid() || assignment.IsSessionScoped)
                    continue;
                document.assignments.Add(new TransferEntry
                {
                    key = assignment.Key,
                    color = ColorHex.ToHex(assignment.Color, true),
                    preset = assignment.PresetId
                });
            }

            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(document, true));
                StudioLog.Info("Exported " + document.assignments.Count + " color(s) to " + path);
                return true;
            }
            catch (Exception exception)
            {
                StudioLog.ExceptionOnce("transfer-export", exception, "Could not write the export file.");
                return false;
            }
        }

        /// <summary>Asks for a source file and an import mode, then imports it.</summary>
        internal static void ImportWithDialog()
        {
            string path = EditorUtility.OpenFilePanel(UiStrings.FilePanelImportTitle, string.Empty,
                UiStrings.FileExtension);
            if (string.IsNullOrEmpty(path))
                return;

            int choice = EditorUtility.DisplayDialogComplex(UiStrings.DialogTitleImport, UiStrings.DialogBodyImport,
                UiStrings.DialogMerge, UiStrings.DialogCancel, UiStrings.DialogReplace);
            if (choice == 1)
                return;

            Import(path, replace: choice == 2);
        }

        /// <summary>Imports colors and presets from a JSON file.</summary>
        /// <param name="path">Absolute source path.</param>
        /// <param name="replace">When <c>true</c> existing assignments and presets are discarded first.</param>
        /// <returns>The number of imported assignments, or <c>-1</c> when the file could not be read.</returns>
        internal static int Import(string path, bool replace)
        {
            TransferDocument document;
            try
            {
                document = JsonUtility.FromJson<TransferDocument>(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                StudioLog.ExceptionOnce("transfer-import", exception, "Could not read the import file.");
                return -1;
            }

            if (document == null || document.kind != DocumentKind)
            {
                StudioLog.WarnOnce("transfer-import-kind",
                    "The selected file is not a " + UiStrings.ProductName + " color set.");
                return -1;
            }

            var store = HierarchyColorService.Store;
            UndoScope.Record(store, UiStrings.UndoImport);

            if (replace)
            {
                store.Assignments.Clear();
                store.Presets.Clear();
                store.InvalidateLookup();
            }

            if (document.presets != null)
            {
                for (int i = 0; i < document.presets.Count; i++)
                {
                    var imported = document.presets[i];
                    if (imported == null || !ColorHex.TryParse(imported.color, out Color32 presetColor))
                        continue;
                    if (store.FindPreset(imported.id) != null)
                        continue;

                    store.Presets.Add(new ColorPreset(imported.id, imported.name, presetColor));
                }
            }

            int applied = 0;
            if (document.assignments != null)
            {
                for (int i = 0; i < document.assignments.Count; i++)
                {
                    var imported = document.assignments[i];
                    if (imported == null || string.IsNullOrEmpty(imported.key))
                        continue;
                    if (!ColorHex.TryParse(imported.color, out Color32 color))
                        continue;
                    if (imported.key.StartsWith(ColorAssignment.SessionKeyPrefix, StringComparison.Ordinal))
                        continue;

                    store.SetAssignment(imported.key, color, imported.preset);
                    applied++;
                }
            }

            store.Sanitize();
            HierarchyColorService.NotifyDataChanged();
            StudioLog.Info("Imported " + applied + " color(s) from " + path);
            return applied;
        }
    }
}
