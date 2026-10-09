using System;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Serialized appearance and behaviour options for the Hierarchy decoration.
    /// </summary>
    [Serializable]
    public sealed class AppearanceSettings
    {
        /// <summary>Smallest supported marker size in points.</summary>
        public const float MinMarkerSize = 4f;

        /// <summary>Largest supported marker size in points.</summary>
        public const float MaxMarkerSize = 14f;

        /// <summary>Smallest supported row tint opacity.</summary>
        public const float MinTintOpacity = 0.04f;

        /// <summary>Largest supported row tint opacity. Capped so row text stays readable.</summary>
        public const float MaxTintOpacity = 0.65f;

        /// <summary>Smallest supported label brightness multiplier.</summary>
        public const float MinLabelBrightness = 0.5f;

        /// <summary>Largest supported label brightness multiplier.</summary>
        public const float MaxLabelBrightness = 1.6f;

        /// <summary>Horizontal distance from the row rect to the GameObject name, in points.</summary>
        public const float DefaultLabelOffset = 18f;

        /// <summary>Smallest supported zebra stripe opacity.</summary>
        public const float MinZebraOpacity = 0.02f;

        /// <summary>Largest supported zebra stripe opacity. Capped so stripes never compete with colors.</summary>
        public const float MaxZebraOpacity = 0.15f;

        /// <summary>Default zebra stripe opacity.</summary>
        public const float DefaultZebraOpacity = 0.05f;

        [SerializeField] private bool m_Enabled = true;
        [SerializeField] private HierarchyDecorations m_Decorations = HierarchyDecorations.Marker | HierarchyDecorations.RowTint;
        [SerializeField] private MarkerShape m_MarkerShape = MarkerShape.Dot;
        [SerializeField] private MarkerPlacement m_MarkerPlacement = MarkerPlacement.RowEnd;
        [SerializeField] private float m_MarkerSize = 8f;
        [SerializeField] private float m_TintOpacity = 0.22f;
        [SerializeField] private TintScope m_TintScope = TintScope.FullRow;
        [SerializeField] private float m_LabelBrightness = 1f;
        [SerializeField] private bool m_LabelFillsBackground;
        [SerializeField] private Color m_RowBackgroundDark = new Color(0.2196f, 0.2196f, 0.2196f, 1f);
        [SerializeField] private Color m_RowBackgroundLight = new Color(0.7843f, 0.7843f, 0.7843f, 1f);
        [SerializeField] private SelectedRowBehavior m_SelectedRowBehavior = SelectedRowBehavior.MarkerOnly;
        [SerializeField] private HoverBehavior m_HoverBehavior = HoverBehavior.Ignore;
        [SerializeField] private float m_LabelOffset = DefaultLabelOffset;
        [SerializeField] private ApplyScope m_DefaultApplyScope = ApplyScope.SelectionOnly;
        [SerializeField] private bool m_DebugLogging;

        // Added in schema 2. Their defaults leave the Hierarchy exactly as schema 1 drew it.
        [SerializeField] private HierarchyMode m_Mode = HierarchyMode.Default;
        [SerializeField] private float m_ZebraOpacity = DefaultZebraOpacity;
        [SerializeField] private IconPlacement m_IconPlacement = IconPlacement.ObjectIcon;
        [SerializeField] private string m_FolderIcon = string.Empty;
        [SerializeField] private string m_PrefabIcon = string.Empty;
        [SerializeField] private TextColorSource m_TextColorSource = TextColorSource.ObjectColor;
        [SerializeField] private Color m_CustomTextColor = Color.white;

        /// <summary>Master switch for the Hierarchy decoration.</summary>
        public bool Enabled
        {
            get => m_Enabled;
            set => m_Enabled = value;
        }

        /// <summary>Which decorations are drawn.</summary>
        public HierarchyDecorations Decorations
        {
            get => m_Decorations;
            set => m_Decorations = value;
        }

        /// <summary>Shape of the row marker.</summary>
        public MarkerShape MarkerShape
        {
            get => m_MarkerShape;
            set => m_MarkerShape = value;
        }

        /// <summary>Placement of the row marker.</summary>
        public MarkerPlacement MarkerPlacement
        {
            get => m_MarkerPlacement;
            set => m_MarkerPlacement = value;
        }

        /// <summary>Marker size in points.</summary>
        public float MarkerSize
        {
            get => m_MarkerSize;
            set => m_MarkerSize = Mathf.Clamp(value, MinMarkerSize, MaxMarkerSize);
        }

        /// <summary>Opacity of the row tint.</summary>
        public float TintOpacity
        {
            get => m_TintOpacity;
            set => m_TintOpacity = Mathf.Clamp(value, MinTintOpacity, MaxTintOpacity);
        }

        /// <summary>Horizontal extent of the row tint.</summary>
        public TintScope TintScope
        {
            get => m_TintScope;
            set => m_TintScope = value;
        }

        /// <summary>Multiplier applied to the assigned color when re-drawing the row label.</summary>
        public float LabelBrightness
        {
            get => m_LabelBrightness;
            set => m_LabelBrightness = Mathf.Clamp(value, MinLabelBrightness, MaxLabelBrightness);
        }

        /// <summary>
        /// Color in which <see cref="HierarchyDecorations.LabelColor"/> re-draws the names of colored objects:
        /// each object's own color, or <see cref="CustomTextColor"/> for all of them.
        /// </summary>
        public TextColorSource TextColorSource
        {
            get => m_TextColorSource;
            set => m_TextColorSource = value;
        }

        /// <summary>Name color used when <see cref="TextColorSource"/> is <see cref="TextColorSource.Custom"/>.</summary>
        public Color CustomTextColor
        {
            get => m_CustomTextColor;
            set
            {
                value.a = 1f;
                m_CustomTextColor = value;
            }
        }

        /// <summary>
        /// When <c>true</c> the label area is filled with the configured row background color before the
        /// colored name is drawn. Produces crisper text at the cost of matching the Editor theme exactly.
        /// </summary>
        public bool LabelFillsBackground
        {
            get => m_LabelFillsBackground;
            set => m_LabelFillsBackground = value;
        }

        /// <summary>Row background color used by <see cref="LabelFillsBackground"/> in the dark Editor theme.</summary>
        public Color RowBackgroundDark
        {
            get => m_RowBackgroundDark;
            set => m_RowBackgroundDark = value;
        }

        /// <summary>Row background color used by <see cref="LabelFillsBackground"/> in the light Editor theme.</summary>
        public Color RowBackgroundLight
        {
            get => m_RowBackgroundLight;
            set => m_RowBackgroundLight = value;
        }

        /// <summary>Behaviour of decorations on selected rows.</summary>
        public SelectedRowBehavior SelectedRowBehavior
        {
            get => m_SelectedRowBehavior;
            set => m_SelectedRowBehavior = value;
        }

        /// <summary>Behaviour of decorations on the hovered row.</summary>
        public HoverBehavior HoverBehavior
        {
            get => m_HoverBehavior;
            set => m_HoverBehavior = value;
        }

        /// <summary>Distance between the row rect and the GameObject name, in points.</summary>
        public float LabelOffset
        {
            get => m_LabelOffset;
            set => m_LabelOffset = Mathf.Clamp(value, 0f, 64f);
        }

        /// <summary>Scope pre-selected when the color palette is opened.</summary>
        public ApplyScope DefaultApplyScope
        {
            get => m_DefaultApplyScope;
            set => m_DefaultApplyScope = value;
        }

        /// <summary>Enables verbose diagnostics in the Console.</summary>
        public bool DebugLogging
        {
            get => m_DebugLogging;
            set => m_DebugLogging = value;
        }

        /// <summary>Overall look of the Hierarchy window: default, zebra striped, minimal, or minimal and striped.</summary>
        public HierarchyMode Mode
        {
            get => m_Mode;
            set => m_Mode = value;
        }

        /// <summary>Opacity of the alternating row shading drawn in <see cref="HierarchyMode.Zebra"/> mode.</summary>
        public float ZebraOpacity
        {
            get => m_ZebraOpacity;
            set => m_ZebraOpacity = Mathf.Clamp(value, MinZebraOpacity, MaxZebraOpacity);
        }

        /// <summary>Where the icon of a style is drawn.</summary>
        public IconPlacement IconPlacement
        {
            get => m_IconPlacement;
            set => m_IconPlacement = value;
        }

        /// <summary>
        /// Built-in icon drawn in place of Unity's icon on grouping objects — GameObjects that have children
        /// and no component besides their Transform. Empty keeps Unity's icon.
        /// </summary>
        public string FolderIcon
        {
            get => m_FolderIcon ?? string.Empty;
            set => m_FolderIcon = BuiltinIcons.NormalizeName(value);
        }

        /// <summary>Built-in icon drawn in place of Unity's icon on prefab instance roots. Empty keeps Unity's icon.</summary>
        public string PrefabIcon
        {
            get => m_PrefabIcon ?? string.Empty;
            set => m_PrefabIcon = BuiltinIcons.NormalizeName(value);
        }

        /// <summary>
        /// <c>true</c> when a setting affects rows that have no style of their own, so the Hierarchy callback
        /// has work to do on every row rather than on styled rows only.
        /// </summary>
        public bool AffectsEveryRow =>
            m_Mode != HierarchyMode.Default || !string.IsNullOrEmpty(m_FolderIcon) || !string.IsNullOrEmpty(m_PrefabIcon);

        /// <summary><c>true</c> when alternating row shading is drawn.</summary>
        public bool ZebraEnabled => (m_Mode & HierarchyMode.Zebra) != 0;

        /// <summary><c>true</c> when Unity's generic GameObject icon is hidden.</summary>
        public bool MinimalEnabled => (m_Mode & HierarchyMode.Minimal) != 0;

        /// <summary>Row background color matching the active Editor theme.</summary>
        public Color CurrentRowBackground(bool proSkin)
        {
            return proSkin ? m_RowBackgroundDark : m_RowBackgroundLight;
        }

        /// <summary>Clamps every numeric field into its supported range.</summary>
        internal void Sanitize()
        {
            m_MarkerSize = Mathf.Clamp(m_MarkerSize, MinMarkerSize, MaxMarkerSize);
            m_TintOpacity = Mathf.Clamp(m_TintOpacity, MinTintOpacity, MaxTintOpacity);
            m_LabelBrightness = Mathf.Clamp(m_LabelBrightness, MinLabelBrightness, MaxLabelBrightness);
            m_LabelOffset = Mathf.Clamp(m_LabelOffset, 0f, 64f);
            m_RowBackgroundDark.a = 1f;
            m_RowBackgroundLight.a = 1f;

            m_Mode &= HierarchyMode.Zebra | HierarchyMode.Minimal;
            if (m_TextColorSource < TextColorSource.ObjectColor || m_TextColorSource > TextColorSource.Custom)
                m_TextColorSource = TextColorSource.ObjectColor;

            // A stored text color is always opaque, so a transparent one is a field missing from a file
            // written before the setting existed. It gets the same default as a new project.
            if (m_CustomTextColor.a <= 0f)
                m_CustomTextColor = Color.white;
            m_CustomTextColor.a = 1f;
            if (m_IconPlacement < IconPlacement.ObjectIcon || m_IconPlacement > IconPlacement.RowEnd)
                m_IconPlacement = IconPlacement.ObjectIcon;
            if (!HierarchyStyle.IsKnownShape(m_MarkerShape))
                m_MarkerShape = MarkerShape.Dot;

            m_ZebraOpacity = Mathf.Clamp(m_ZebraOpacity, MinZebraOpacity, MaxZebraOpacity);
            m_FolderIcon = BuiltinIcons.NormalizeName(m_FolderIcon);
            m_PrefabIcon = BuiltinIcons.NormalizeName(m_PrefabIcon);
        }

        /// <summary>
        /// Puts every setting added in schema 2 at the value that reproduces schema 1. Called when a schema 1
        /// file is upgraded, so the result never depends on how Unity initializes fields absent from the file.
        /// </summary>
        internal void ResetSettingsAddedInVersion2()
        {
            m_Mode = HierarchyMode.Default;
            m_ZebraOpacity = DefaultZebraOpacity;
            m_IconPlacement = IconPlacement.ObjectIcon;
            m_FolderIcon = string.Empty;
            m_PrefabIcon = string.Empty;
            m_TextColorSource = TextColorSource.ObjectColor;
            m_CustomTextColor = Color.white;
        }

        /// <summary>Copies every field from another instance.</summary>
        internal void CopyFrom(AppearanceSettings other)
        {
            if (other == null)
                return;

            m_Enabled = other.m_Enabled;
            m_Decorations = other.m_Decorations;
            m_MarkerShape = other.m_MarkerShape;
            m_MarkerPlacement = other.m_MarkerPlacement;
            m_MarkerSize = other.m_MarkerSize;
            m_TintOpacity = other.m_TintOpacity;
            m_TintScope = other.m_TintScope;
            m_LabelBrightness = other.m_LabelBrightness;
            m_LabelFillsBackground = other.m_LabelFillsBackground;
            m_RowBackgroundDark = other.m_RowBackgroundDark;
            m_RowBackgroundLight = other.m_RowBackgroundLight;
            m_SelectedRowBehavior = other.m_SelectedRowBehavior;
            m_HoverBehavior = other.m_HoverBehavior;
            m_LabelOffset = other.m_LabelOffset;
            m_DefaultApplyScope = other.m_DefaultApplyScope;
            m_DebugLogging = other.m_DebugLogging;
            m_Mode = other.m_Mode;
            m_ZebraOpacity = other.m_ZebraOpacity;
            m_IconPlacement = other.m_IconPlacement;
            m_FolderIcon = other.m_FolderIcon;
            m_PrefabIcon = other.m_PrefabIcon;
            m_TextColorSource = other.m_TextColorSource;
            m_CustomTextColor = other.m_CustomTextColor;
        }
    }
}
