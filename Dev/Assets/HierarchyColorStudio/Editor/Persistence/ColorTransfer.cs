using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Human-readable JSON export and import for style assignments and presets.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Exported keys are <see cref="GlobalObjectId"/> strings, which stay valid as long as the scene or
    /// prefab asset they refer to is unchanged. That makes an export usable for sharing a color scheme
    /// with teammates working on the same scenes.
    /// Session-scoped keys are never exported because they are meaningless outside the session.
    /// </para>
    /// <para>
    /// Document version 2 adds the style elements to every entry and preset as named, optional fields.
    /// A missing field means the element's default, so a version 1 document imports with its exact
    /// meaning, and version 1.0 of the plugin can still read the colors of a version 2 document, ignoring
    /// the fields it does not know.
    /// </para>
    /// </remarks>
    internal static class ColorTransfer
    {
        private const int DocumentVersion = 2;
        private const string DocumentKind = "CryNet.HierarchyColorStudio.ColorSet";

        /// <summary>Style elements shared by entries and presets. Enums are written by name for readability.</summary>
        [Serializable]
        private class TransferStyle
        {
            public string color;
            public string background;
            public string text;
            public string textColor;
            public string marker;
            public string markerShape;
            public string icon;
        }

        [Serializable]
        private sealed class TransferEntry : TransferStyle
        {
            public string key;
            public string preset;
        }

        [Serializable]
        private sealed class TransferPreset : TransferStyle
        {
            public string id;
            public string name;
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

                var transferPreset = new TransferPreset { id = preset.Id, name = preset.Name };
                WriteStyle(preset.Style, transferPreset);
                document.presets.Add(transferPreset);
            }

            var assignments = store.Assignments;
            for (int i = 0; i < assignments.Count; i++)
            {
                var assignment = assignments[i];
                if (assignment == null || !assignment.IsValid() || assignment.IsSessionScoped)
                    continue;

                var entry = new TransferEntry { key = assignment.Key, preset = assignment.PresetId };
                WriteStyle(assignment.Style, entry);
                document.assignments.Add(entry);
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
                    if (imported == null || !TryReadStyle(imported, out HierarchyStyle presetStyle))
                        continue;
                    if (store.FindPreset(imported.id) != null)
                        continue;

                    store.Presets.Add(new ColorPreset(imported.id, imported.name, presetStyle));
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
                    if (!TryReadStyle(imported, out HierarchyStyle style))
                        continue;
                    if (imported.key.StartsWith(ColorAssignment.SessionKeyPrefix, StringComparison.Ordinal))
                        continue;

                    store.SetAssignmentStyle(imported.key, style, imported.preset);
                    applied++;
                }
            }

            store.Sanitize();
            HierarchyColorService.NotifyDataChanged();
            StudioLog.Info("Imported " + applied + " color(s) from " + path);
            return applied;
        }

        private static void WriteStyle(HierarchyStyle style, TransferStyle target)
        {
            var normalized = style.Normalized();
            target.color = normalized.HasColor ? ColorHex.ToHex(normalized.Color, true) : string.Empty;
            target.background = normalized.Background.ToString();
            target.text = normalized.Text.ToString();
            target.textColor = normalized.Text == StyleOverride.On ? ColorHex.ToHex(normalized.TextColor, true) : string.Empty;
            target.marker = normalized.Marker.ToString();
            target.markerShape = normalized.Marker == StyleOverride.On ? normalized.MarkerShape.ToString() : string.Empty;
            target.icon = normalized.Icon ?? string.Empty;
        }

        /// <summary>
        /// Reads a style. A color that is present but unparsable rejects the record, as version 1 did; an
        /// absent color is valid when another element draws something. Unknown element values fall back to
        /// their defaults rather than rejecting the record.
        /// </summary>
        private static bool TryReadStyle(TransferStyle source, out HierarchyStyle style)
        {
            style = default;
            if (!string.IsNullOrEmpty(source.color))
            {
                if (!ColorHex.TryParse(source.color, out style.Color))
                    return false;
                style.HasColor = true;
            }

            style.Background = ParseOverride(source.background);
            style.Text = ParseOverride(source.text);
            if (style.Text == StyleOverride.On && !ColorHex.TryParse(source.textColor, out style.TextColor))
                style.Text = StyleOverride.Default;

            style.Marker = ParseOverride(source.marker);
            if (style.Marker == StyleOverride.On)
            {
                if (!Enum.TryParse(source.markerShape, true, out style.MarkerShape) ||
                    !HierarchyStyle.IsKnownShape(style.MarkerShape))
                    style.Marker = StyleOverride.Default;
            }

            style.Icon = BuiltinIcons.NormalizeName(source.icon);
            style = style.Normalized();
            return !style.IsEmpty;
        }

        private static StyleOverride ParseOverride(string value)
        {
            return !string.IsNullOrEmpty(value) && Enum.TryParse(value, true, out StyleOverride parsed) &&
                   parsed >= StyleOverride.Default && parsed <= StyleOverride.On
                ? parsed
                : StyleOverride.Default;
        }
    }
}
