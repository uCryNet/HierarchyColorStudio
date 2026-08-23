namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Every user-facing string used by the plugin, kept in one place so the product can be
    /// proof-read, re-worded or localized without touching UI code.
    /// </summary>
    internal static class UiStrings
    {
        internal const string ProductName = "Hierarchy Color Studio";
        internal const string Version = "1.0.0";
        internal const string Vendor = "CryNet";
        internal const string AuthorWebsite = "https://crynet.dev/";
        internal const string SupportEmail = "ucrynet@proton.me";

        internal const string MenuRootTools = "Tools/" + ProductName + "/";
        internal const string MenuRootGameObject = "GameObject/Hierarchy Color/";
        internal const string ShortcutCategory = ProductName + "/";
        internal const string ProjectSettingsPath = "Project/" + ProductName;

        internal const string WindowTitle = ProductName;
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
        internal const string LabelHex = "Hex";
        internal const string LabelName = "Name";
        internal const string LabelSearch = "Search";
        internal const string LabelApplyScopePopup = "Apply To";
        internal const string LabelCustomColor = "Custom Color";
        internal const string LabelWebsite = "Website";
        internal const string LabelSupport = "Support";

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

        internal const string TooltipEnabled = "Turns the Hierarchy decoration on or off without deleting any assigned color.";
        internal const string TooltipDecorations = "Combine a marker, a row background wash and a colored name.";
        internal const string TooltipMarkerSize = "Size of the row marker in points.";
        internal const string TooltipTintOpacity = "Opacity of the row background wash. Kept below 1 so row text stays readable.";
        internal const string TooltipLabelBrightness = "Multiplies the assigned color when the GameObject name is re-drawn.";
        internal const string TooltipLabelFill = "Fills the name area with the row color before drawing colored text. Produces crisper text but must match your Editor theme.";
        internal const string TooltipLabelOffset = "Distance between the row rect and the GameObject name. Only change this if colored names look misaligned in your Unity version.";
        internal const string TooltipSelectedBehavior = "How rows that belong to the current selection are decorated.";
        internal const string TooltipHoverBehavior = "How the row under the mouse pointer is decorated.";
        internal const string TooltipDebugLogging = "Writes diagnostic messages to the Console. Disabled by default.";
        internal const string TooltipPruneMissing = "Removes stored colors whose GameObject no longer exists. Only scenes that are currently open are inspected.";
        internal const string TooltipApplyScope = "Whether children of the targeted GameObjects also receive the color.";
        internal const string TooltipSelectColored = "Selects every colored GameObject in the scenes that are currently open.";

        internal const string HintNoSelection = "Select one or more GameObjects in the Hierarchy to assign a color.";
        internal const string HintReorderDisabled = "Clear the search field to reorder presets.";
        internal const string HintNoPresets = "No presets match the current search.";
        internal const string HintDisabled = "Hierarchy colors are currently disabled.";
        internal const string HintMixedColors = "The selection uses more than one color.";
        internal const string HintInvalidHex = "Enter a color such as #3498DB or 3498DBFF.";
        internal const string HintSessionScoped =
            "Some colored objects have not been saved to a scene yet. Their colors are kept for this Editor session and become permanent when you save the scene.";

        internal const string UndoAssignColor = "Assign Hierarchy Color";
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
        internal const string DialogTitleLargeOperation = "Apply To Many Objects";
        internal const string DialogOk = "OK";
        internal const string DialogCancel = "Cancel";
        internal const string DialogReplace = "Replace";
        internal const string DialogMerge = "Merge";
        internal const string DialogContinue = "Continue";

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

        /// <summary>Formats the confirmation body for operations touching many objects.</summary>
        internal static string LargeOperationBody(int count)
        {
            return "This will change " + count + " GameObjects. Continue?";
        }
    }
}
