namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Every user-facing string used by the plugin, kept in one place so the product can be
    /// proof-read, re-worded or localized without touching UI code.
    /// </summary>
    internal static class UiStrings
    {
        internal const string ProductName = "Hierarchy Color Studio";
        internal const string Version = "1.1.0";
        internal const string Vendor = "CryNet";
        internal const string AuthorWebsite = "https://crynet.dev/";
        internal const string SupportEmail = "ucrynet@proton.me";

        internal const string MenuRootTools = "Tools/" + ProductName + "/";
        internal const string MenuRootGameObject = "GameObject/Hierarchy Color/";
        internal const string ShortcutCategory = ProductName + "/";
        internal const string ProjectSettingsPath = "Project/" + ProductName;

        // Shortcut identifiers are persisted in users' shortcut profiles. Never rename them.
        internal const string ShortcutOpenStudio = ShortcutCategory + "Open Color Studio";
        internal const string ShortcutSetColor = ShortcutCategory + "Set Color";
        internal const string ShortcutApplyLastColor = ShortcutCategory + "Apply Last Color";
        internal const string ShortcutClearColor = ShortcutCategory + "Clear Color";

        internal const string WindowTitleShort = "Hierarchy Colors";

        internal const string MenuSetColor = "Set Color…";
        internal const string MenuClearColor = "Clear Color";
        internal const string MenuApplyLastColor = "Apply Last Color";
        internal const string MenuOpenStudio = "Open Color Studio";
        internal const string MenuOpenWindow = "Color Studio Window";
        internal const string MenuSettings = "Settings";
        internal const string MenuImport = "Import Colors…";
        internal const string MenuExport = "Export Colors…";
        internal const string MenuClearAll = "Clear All Colors";
        internal const string MenuDocumentation = "Documentation";

        internal const string SectionSelection = "Selection";
        internal const string SectionPresets = "Presets";
        internal const string SectionAppearance = "Appearance";
        internal const string SectionMaintenance = "Assignments";
        internal const string SectionAdvanced = "Advanced";
        internal const string SectionAbout = "About";

        internal const string LabelEnabled = "Enable Hierarchy Colors";
        internal const string LabelDecorations = "Display Mode";
        internal const string LabelMarkerShape = "Marker Shape";
        internal const string LabelMarkerPlacement = "Marker Placement";
        internal const string LabelMarkerSize = "Marker Size";
        internal const string LabelTintOpacity = "Background Opacity";
        internal const string LabelTintScope = "Background Extent";
        internal const string LabelTextColor = "Text Color";
        internal const string LabelLabelBrightness = "Text Brightness";
        internal const string LabelLabelFill = "Fill Text Background";
        internal const string LabelRowBackgroundDark = "Row Color (Dark Theme)";
        internal const string LabelRowBackgroundLight = "Row Color (Light Theme)";
        internal const string LabelSelectedBehavior = "Selected Rows";
        internal const string LabelHoverBehavior = "Hovered Rows";
        internal const string LabelLabelOffset = "Text Offset";
        internal const string LabelApplyScope = "Default Apply Scope";
        internal const string LabelDebugLogging = "Debug Logging";
        internal const string LabelColor = "Color";
        internal const string LabelSearch = "Search";
        internal const string LabelApplyScopePopup = "Apply To";
        internal const string LabelCustomColor = "Custom Color";
        internal const string LabelWebsite = "Website";
        internal const string LabelSupport = "Support";
        internal const string LabelHierarchyMode = "Hierarchy Mode";
        internal const string LabelZebraOpacity = "Stripe Strength";
        internal const string LabelIconPlacement = "Icon Placement";
        internal const string LabelFolderIcon = "Folder Icon";
        internal const string LabelPrefabIcon = "Prefab Icon";
        internal const string LabelStyleColor = "Color";
        internal const string LabelStyleBackground = "Background";
        internal const string LabelStyleText = "Text";
        internal const string LabelStyleMarker = "Marker";
        internal const string LabelStyleIcon = "Icon";
        internal const string LabelPreview = "Preview";
        internal const string LabelIconName = "Icon Name";

        internal const string ModeDefault = "Default";
        internal const string ModeZebra = "Zebra";
        internal const string ModeMinimal = "Minimal";
        internal const string ModeMinimalZebra = "Minimal + Zebra";

        internal const string OptionDefaultShown = "Default (Shown)";
        internal const string OptionDefaultHidden = "Default (None)";
        internal const string OptionDefaultColored = "Default (Colored)";
        internal const string OptionDefaultUnity = "Default (Unity Color)";
        internal const string OptionDefaultPrefix = "Default (";
        internal const string OptionHide = "None";
        internal const string OptionShow = "Show";
        internal const string OptionUnityText = "Unity Color";
        internal const string OptionCustom = "Custom";
        internal const string OptionNone = "None";
        internal const string OptionUnityIcon = "Unity's icon";
        internal const string SuffixIconMissing = " (missing)";
        internal const string NewPresetName = "New Preset";

        /// <summary>Formats the product line shown in the About section.</summary>
        internal const string AboutProduct = ProductName + " " + Version + "  ·  " + Vendor;

        internal const string ButtonAddPreset = "Add Preset";
        internal const string ButtonApply = "Apply";
        internal const string ButtonClear = "Clear";
        internal const string ButtonClearAll = "Clear All Colors";
        internal const string ButtonResetAppearance = "Reset Appearance";
        internal const string ButtonResetPresets = "Restore Default Presets";
        internal const string ButtonResetAll = "Restore Factory Defaults";
        internal const string ButtonSelectColored = "Select Colored Objects";
        internal const string ButtonPruneMissing = "Remove Missing Entries";
        internal const string ButtonSaveAsPreset = "Save As Preset";
        internal const string ButtonOpenStudio = "Open Color Studio…";
        internal const string ButtonDocumentation = "Documentation";
        internal const string ButtonSaveNow = "Save Now";
        internal const string ButtonChooseIcon = "Choose…";
        internal const string ButtonUse = "Use";
        internal const string ButtonSaveStyleAsPreset = "Save As Preset";

        internal const string TooltipEnabled = "Turns the Hierarchy decoration on or off without deleting any assigned color.";
        internal const string TooltipDecorations = "Combine a marker, a row background wash and a colored name.";
        internal const string TooltipMarkerSize = "Size of the row marker in points.";
        internal const string TooltipTintOpacity = "Opacity of the row background wash. Kept below 1 so row text stays readable.";
        internal const string TooltipTextColor =
            "Color of the names of colored objects: each object's own color, or one custom color for all of them. A style with its own text color keeps it.";
        internal const string TooltipLabelBrightness = "Multiplies the assigned color when the GameObject name is re-drawn.";
        internal const string TooltipLabelFill = "Fills the name area with the row color before drawing colored text. Produces crisper text but must match your Editor theme.";
        internal const string TooltipLabelOffset = "Distance between the row rect and the GameObject name. Only change this if colored names look misaligned in your Unity version.";
        internal const string TooltipSelectedBehavior = "How rows that belong to the current selection are decorated.";
        internal const string TooltipHoverBehavior = "How the row under the mouse pointer is decorated.";
        internal const string TooltipDebugLogging = "Writes diagnostic messages to the Console. Disabled by default.";
        internal const string TooltipPruneMissing = "Removes stored colors whose GameObject no longer exists. Only scenes that are currently open are inspected.";
        internal const string TooltipApplyScope = "Whether children of the targeted GameObjects also receive the color.";
        internal const string TooltipSelectColored = "Selects every colored GameObject in the scenes that are currently open.";
        internal const string TooltipHierarchyMode =
            "The look of the whole Hierarchy. Zebra shades every second row; Minimal hides Unity's generic GameObject icon so meaningful icons stand out. The two can be combined.";
        internal const string TooltipZebraOpacity = "Strength of the alternating row shading.";
        internal const string TooltipIconPlacement =
            "Where a style's icon is drawn: in place of the GameObject's own icon, or at the end of the row next to a row-end marker.";
        internal const string TooltipFolderIcon =
            "Replaces Unity's icon on grouping objects — GameObjects with children and no component besides their Transform. None keeps Unity's icon.";
        internal const string TooltipPrefabIcon =
            "Replaces Unity's icon on prefab instance roots. None keeps Unity's icon. Missing prefabs always keep theirs.";
        internal const string TooltipStyleColor =
            "Main color of the style, used by the background and the marker. Untick for a style made of an icon or a text color alone.";
        internal const string TooltipStyleBackground =
            "Whether the row gets a background wash in the main color. Default follows the Display Mode.";
        internal const string TooltipStyleText =
            "Color of the object's name. Default follows the Display Mode, Unity Color never recolors it, Custom uses its own color — with or without a background.";
        internal const string TooltipStyleMarker =
            "How the object is classified: a marker shape in the main color. Default uses the Display Mode and the project marker shape.";
        internal const string TooltipStyleIcon = "What the object is: a Unity built-in icon. Independent of the marker.";
        internal const string TooltipChooseIcon = "Choose a Unity built-in icon.";
        internal const string TooltipRemoveIcon = "Remove the icon.";
        internal const string TooltipNoIcon = "No icon";
        internal const string TooltipIconName =
            "Any Unity built-in icon name, for icons not listed above. Names that do not exist in this Unity version are rejected.";
        internal const string TooltipSaveStyleAsPreset = "Creates a preset with the complete style of the selection.";
        internal const string TooltipClearStyle = "Removes the color and every other style element from the selected objects. The objects themselves are not changed.";

        internal const string HintNoSelection = "Select one or more GameObjects in the Hierarchy to assign a color.";
        internal const string HintReorderDisabled = "Clear the search field to reorder presets.";
        internal const string HintNoPresets = "No presets match the current search.";
        internal const string HintDisabled = "Hierarchy colors are currently disabled.";
        internal const string HintMixedColors = "The selection uses more than one style.";
        internal const string HintIconMissing = "Unity has no icon with this name in this version.";
        internal const string HintModeDefault = "Unity's Hierarchy, plus the decorations of styled rows.";
        internal const string HintModeZebra = "Every second row is gently shaded, so long lists are easier to follow.";
        internal const string HintModeMinimal = "Unity's generic GameObject icon is hidden; prefab, folder and style icons remain.";
        internal const string HintModeMinimalZebra = "Generic GameObject icons are hidden and every second row is shaded.";
        internal const string HintInvalidHex = "Enter a color such as #3498DB or 3498DBFF.";
        internal const string HintSessionScoped =
            "Some colored objects have not been saved to a scene yet. Their colors are kept for this Editor session and become permanent when you save the scene.";

        internal const string UndoAssignColor = "Assign Hierarchy Color";
        internal const string UndoApplyStyle = "Apply Hierarchy Style";
        internal const string UndoEditStyle = "Edit Hierarchy Style";
        internal const string UndoClearColor = "Clear Hierarchy Color";
        internal const string UndoClearAllColors = "Clear All Hierarchy Colors";
        internal const string UndoEditPresets = "Edit Hierarchy Color Presets";
        internal const string UndoEditAppearance = "Edit Hierarchy Color Appearance";
        internal const string UndoImport = "Import Hierarchy Colors";
        internal const string UndoResetEverything = "Reset Hierarchy Color Studio";
        internal const string UndoPrune = "Remove Missing Hierarchy Colors";

        internal const string DialogTitleClearAll = "Clear All Colors";
        internal const string DialogTitleFactoryReset = "Restore Factory Defaults";
        internal const string DialogTitleImport = "Import Colors";
        internal const string DialogOk = "OK";
        internal const string DialogCancel = "Cancel";
        internal const string DialogReplace = "Replace";
        internal const string DialogMerge = "Merge";

        internal const string DialogBodyClearAll = "Remove every stored Hierarchy color from this project? This can be undone.";
        internal const string DialogBodyFactoryReset =
            "Restore appearance settings and presets to their defaults and remove every stored color? This can be undone.";
        internal const string DialogBodyImport = "Merge the imported colors into the current project, or replace them?";

        internal const string FilePanelExportTitle = "Export Hierarchy Colors";
        internal const string FilePanelImportTitle = "Import Hierarchy Colors";
        internal const string FileDefaultName = "HierarchyColors";
        internal const string FileExtension = "json";

        /// <summary>Formats the "n objects selected" header.</summary>
        internal static string SelectionHeader(int count)
        {
            return count == 1 ? "1 GameObject selected" : count + " GameObjects selected";
        }

        /// <summary>Formats the assignment counter shown in the maintenance section.</summary>
        internal static string AssignmentSummary(int stored, int resolved)
        {
            return stored + " stored color(s), " + resolved + " visible in the open scenes.";
        }
    }
}
