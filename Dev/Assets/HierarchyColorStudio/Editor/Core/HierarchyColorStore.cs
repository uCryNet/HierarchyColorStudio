using System.Collections.Generic;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Serialized container for every piece of Hierarchy Color Studio data: appearance settings,
    /// presets and per-GameObject style assignments.
    /// The instance is never part of the AssetDatabase; it is written to the project's
    /// <c>ProjectSettings</c> folder by <see cref="HierarchyColorStoreFile"/>.
    /// </summary>
    /// <remarks>
    /// Schema history:
    /// <list type="bullet">
    /// <item><description>1 — version 1.0: a color per object, color-only presets.</description></item>
    /// <item><description>2 — version 1.1: complete styles (background, text color, marker, icon) for
    /// assignments and presets; Hierarchy modes; folder and prefab icons. Every added field defaults to
    /// schema 1 behaviour, and <see cref="Sanitize"/> upgrades schema 1 data explicitly.</description></item>
    /// </list>
    /// </remarks>
    public sealed class HierarchyColorStore : ScriptableObject
    {
        /// <summary>Schema version of the serialized file.</summary>
        public const int CurrentVersion = 2;

        [SerializeField] private int m_Version = CurrentVersion;
        [SerializeField] private AppearanceSettings m_Appearance = new AppearanceSettings();
        [SerializeField] private List<ColorPreset> m_Presets = new List<ColorPreset>();
        [SerializeField] private List<ColorAssignment> m_Assignments = new List<ColorAssignment>();

        private readonly Dictionary<string, int> m_KeyLookup = new Dictionary<string, int>(64);
        private bool m_LookupValid;

        /// <summary>Schema version of the loaded data.</summary>
        public int Version => m_Version;

        /// <summary>
        /// Schema version the data was upgraded from by the last <see cref="Sanitize"/>, or <c>0</c> when no
        /// upgrade happened. Not serialized.
        /// </summary>
        internal int UpgradedFromVersion { get; private set; }

        /// <summary>Appearance and behaviour options.</summary>
        public AppearanceSettings Appearance => m_Appearance ?? (m_Appearance = new AppearanceSettings());

        /// <summary>Editable list of color presets.</summary>
        public List<ColorPreset> Presets => m_Presets ?? (m_Presets = new List<ColorPreset>());

        /// <summary>Editable list of color assignments.</summary>
        public List<ColorAssignment> Assignments => m_Assignments ?? (m_Assignments = new List<ColorAssignment>());

        /// <summary>Invalidates the key lookup after external list edits, undo or a reload.</summary>
        public void InvalidateLookup()
        {
            m_LookupValid = false;
        }

        /// <summary>Finds the index of the assignment for an identity key, or <c>-1</c>.</summary>
        /// <param name="key">Identity key to search for.</param>
        public int IndexOfKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return -1;

            EnsureLookup();
            return m_KeyLookup.TryGetValue(key, out int index) && index < Assignments.Count ? index : -1;
        }

        /// <summary>
        /// Adds or updates the assignment for an identity key. An existing assignment keeps its other style
        /// elements; only its main color and preset change.
        /// </summary>
        /// <param name="key">Identity key of the target GameObject.</param>
        /// <param name="color">Color to store.</param>
        /// <param name="presetId">Optional source preset identifier.</param>
        public void SetAssignment(string key, Color32 color, string presetId)
        {
            if (string.IsNullOrEmpty(key))
                return;

            int index = IndexOfKey(key);
            if (index >= 0)
            {
                Assignments[index].Color = color;
                Assignments[index].PresetId = presetId;
                return;
            }

            AddAssignment(new ColorAssignment(key, color, presetId));
        }

        /// <summary>
        /// Adds or replaces the complete style of an identity key. A style that draws nothing removes the
        /// assignment, because such a record would only be dropped on the next load.
        /// </summary>
        /// <param name="key">Identity key of the target GameObject.</param>
        /// <param name="style">Style to store.</param>
        /// <param name="presetId">Optional source preset identifier.</param>
        public void SetAssignmentStyle(string key, HierarchyStyle style, string presetId)
        {
            if (string.IsNullOrEmpty(key))
                return;

            if (style.IsEmpty)
            {
                RemoveAssignment(key);
                return;
            }

            int index = IndexOfKey(key);
            if (index >= 0)
            {
                Assignments[index].Style = style;
                Assignments[index].PresetId = presetId;
                return;
            }

            AddAssignment(new ColorAssignment(key, style, presetId));
        }

        private void AddAssignment(ColorAssignment assignment)
        {
            Assignments.Add(assignment);
            if (m_LookupValid)
                m_KeyLookup[assignment.Key] = Assignments.Count - 1;
        }

        /// <summary>Removes the assignment for an identity key.</summary>
        /// <param name="key">Identity key of the target GameObject.</param>
        /// <returns><c>true</c> when an assignment was removed.</returns>
        public bool RemoveAssignment(string key)
        {
            int index = IndexOfKey(key);
            if (index < 0)
                return false;

            Assignments.RemoveAt(index);
            InvalidateLookup();
            return true;
        }

        /// <summary>Finds a preset by identifier, or returns <c>null</c>.</summary>
        /// <param name="presetId">Preset identifier.</param>
        public ColorPreset FindPreset(string presetId)
        {
            if (string.IsNullOrEmpty(presetId))
                return null;

            var presets = Presets;
            for (int i = 0; i < presets.Count; i++)
            {
                if (presets[i] != null && presets[i].Id == presetId)
                    return presets[i];
            }

            return null;
        }

        /// <summary>Replaces the current data with the factory defaults.</summary>
        public void ResetToDefaults()
        {
            m_Version = CurrentVersion;
            m_Appearance = new AppearanceSettings();
            m_Presets = CreateDefaultPresets();
            m_Assignments = new List<ColorAssignment>();
            InvalidateLookup();
        }

        /// <summary>Restores appearance settings to the factory defaults, keeping presets and assignments.</summary>
        public void ResetAppearance()
        {
            Appearance.CopyFrom(new AppearanceSettings());
        }

        /// <summary>Replaces the preset list with the factory defaults.</summary>
        public void ResetPresets()
        {
            m_Presets = CreateDefaultPresets();
        }

        /// <summary>
        /// Repairs the loaded data: drops unusable records, removes duplicate keys and clamps settings.
        /// Called after every load so a hand-edited or partially merged file can never break the Editor.
        /// </summary>
        /// <returns>The number of records that were dropped or repaired.</returns>
        public int Sanitize()
        {
            int repairs = 0;
            UpgradedFromVersion = 0;

            if (m_Version > CurrentVersion)
            {
                // Written by a newer version. Everything this version understands is kept; fields it does
                // not know are dropped by Unity when the file is next saved.
                m_Version = CurrentVersion;
                repairs++;
            }
            else if (m_Version < CurrentVersion)
            {
                if (m_Version <= 0)
                    repairs++;

                UpgradedFromVersion = Mathf.Max(1, m_Version);
                UpgradeFromVersion1();
            }

            if (m_Appearance == null)
            {
                m_Appearance = new AppearanceSettings();
                repairs++;
            }
            m_Appearance.Sanitize();

            if (m_Presets == null)
            {
                m_Presets = CreateDefaultPresets();
                repairs++;
            }
            else
            {
                for (int i = m_Presets.Count - 1; i >= 0; i--)
                {
                    if (m_Presets[i] == null)
                    {
                        m_Presets.RemoveAt(i);
                        repairs++;
                        continue;
                    }

                    m_Presets[i].Sanitize();
                }
            }

            if (m_Assignments == null)
            {
                m_Assignments = new List<ColorAssignment>();
                repairs++;
            }
            else
            {
                var seen = new HashSet<string>();
                for (int i = m_Assignments.Count - 1; i >= 0; i--)
                {
                    var assignment = m_Assignments[i];
                    if (assignment == null)
                    {
                        m_Assignments.RemoveAt(i);
                        repairs++;
                        continue;
                    }

                    bool repaired = assignment.RepairStyle();
                    if (!assignment.IsValid() || !seen.Add(assignment.Key))
                    {
                        m_Assignments.RemoveAt(i);
                        repairs++;
                    }
                    else if (repaired)
                    {
                        repairs++;
                    }
                }
            }

            InvalidateLookup();
            return repairs;
        }

        /// <summary>
        /// Upgrades schema 1 data. Schema 1 had no style elements, no Hierarchy modes and no folder or prefab
        /// icons; each of them is set explicitly to the value that reproduces schema 1, so an upgraded project
        /// looks exactly as it did and does not depend on how Unity initialized the absent fields.
        /// The rules schema 1 applied on load — a preset color is mandatory, an assignment without a usable
        /// color is dropped — are applied here too, before the schema 2 rules take over.
        /// </summary>
        private void UpgradeFromVersion1()
        {
            if (m_Appearance != null)
                m_Appearance.ResetSettingsAddedInVersion2();

            if (m_Presets != null)
            {
                for (int i = 0; i < m_Presets.Count; i++)
                    m_Presets[i]?.ResetElementsAddedInVersion2();
            }

            if (m_Assignments != null)
            {
                for (int i = 0; i < m_Assignments.Count; i++)
                    m_Assignments[i]?.ResetElementsAddedInVersion2();
            }

            m_Version = CurrentVersion;
        }

        /// <summary>Removes every assignment whose key is only valid inside a single Editor session.</summary>
        /// <returns>The number of removed assignments.</returns>
        public int RemoveSessionScopedAssignments()
        {
            int removed = 0;
            var assignments = Assignments;
            for (int i = assignments.Count - 1; i >= 0; i--)
            {
                if (assignments[i] != null && assignments[i].IsSessionScoped)
                {
                    assignments.RemoveAt(i);
                    removed++;
                }
            }

            if (removed > 0)
                InvalidateLookup();
            return removed;
        }

        private void EnsureLookup()
        {
            if (m_LookupValid)
                return;

            m_KeyLookup.Clear();
            var assignments = Assignments;
            for (int i = 0; i < assignments.Count; i++)
            {
                var assignment = assignments[i];
                if (assignment == null || string.IsNullOrEmpty(assignment.Key))
                    continue;
                m_KeyLookup[assignment.Key] = i;
            }

            m_LookupValid = true;
        }

        private static List<ColorPreset> CreateDefaultPresets()
        {
            return new List<ColorPreset>
            {
                new ColorPreset("Red", new Color32(0xE7, 0x4C, 0x3C, 0xFF)),
                new ColorPreset("Orange", new Color32(0xE6, 0x7E, 0x22, 0xFF)),
                new ColorPreset("Amber", new Color32(0xF1, 0xC4, 0x0F, 0xFF)),
                new ColorPreset("Green", new Color32(0x2E, 0xCC, 0x71, 0xFF)),
                new ColorPreset("Teal", new Color32(0x1A, 0xBC, 0x9C, 0xFF)),
                new ColorPreset("Blue", new Color32(0x34, 0x98, 0xDB, 0xFF)),
                new ColorPreset("Indigo", new Color32(0x5B, 0x6C, 0xE8, 0xFF)),
                new ColorPreset("Violet", new Color32(0x9B, 0x59, 0xB6, 0xFF)),
                new ColorPreset("Pink", new Color32(0xE8, 0x5A, 0x9B, 0xFF)),
                new ColorPreset("Slate", new Color32(0x7F, 0x8C, 0x8D, 0xFF))
            };
        }
    }
}
