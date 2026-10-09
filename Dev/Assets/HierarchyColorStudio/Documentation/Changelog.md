# Changelog

All notable changes to Hierarchy Color Studio are documented here.
This project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## 1.1.0

Styles: a color is now one element of a complete row style. Existing projects upgrade automatically and
look exactly as before.

### Added

- **Styles.** Every colored object, and every preset, now has a complete style made of independent
  elements: main color, background, text color, marker and icon. Each element can follow the project's
  *Display Mode* (the default, and how 1.0 behaved) or be set for the style alone.
- **Separate text color.** A style can draw the object's name in its own color — with a background
  ("dark blue background, white text") or without one ("orange text only"). *Unity Color* keeps Unity's
  own text even when the Display Mode colors names.
- **Project text color.** *Appearance → Text Color* draws the names of all colored objects in one custom
  color — for example white names on every tinted row — instead of each object's own color. It applies to
  colors applied from the palette and the shortcuts too; a style with its own text color keeps it.
- **Unity built-in icons** per style, chosen from a curated picker of Unity's own Editor icons (64 icons,
  verified in Unity 2022.3, 6000.3 and 6000.5) or by typing any other built-in icon name. The plugin
  ships no icon of its own. Icons are drawn in place of the GameObject's icon (default) or at the row end.
- **Icon and marker are independent:** no icon and no marker, marker only, icon only, or both.
- **New marker shapes:** Ring, Diamond and Triangle, alongside Dot, Bar and Square. A style can use its
  own marker shape or turn the marker off.
- **Hierarchy modes:** *Zebra* (subtle alternating row shading, adjustable strength), *Minimal* (hides
  Unity's generic GameObject icon so meaningful icons stand out), and *Minimal + Zebra*. *Default* leaves
  the Hierarchy as it was.
- **Folder and prefab icons** (opt-in): replace Unity's icon on grouping objects (children and no
  component besides Transform) and on prefab instance roots with another built-in icon.
- **Default shortcuts:** *Set Color* (add or change a color) on **Alt + Shift + C** and *Clear Color* on
  **Alt + Shift + Backspace**, through Unity's Shortcut Manager. Existing shortcut identifiers are kept.
- Presets store the complete style. Presets can be expanded in the list to edit every element, and a
  style can be saved from the selection with **Save As Preset**.
- The color palette applies a preset's complete style, and has an **Icon** section that changes only the
  icon of the targets.
- A live **Preview** row in the Selection section and in each expanded preset.
- Public API: `HierarchyStyle`, `StyleFields`, `HierarchyColorService.TryGetStyle`, `ApplyStyle`,
  `ApplyPreset`, `ModifyStyle` and `AnyHasStyle`.

### Changed

- The settings file uses schema 2. Schema 1 files are upgraded on load; see *Compatibility*.
- The export format is version 2. Style elements are written as optional, named fields; version 1 files
  import unchanged.
- *Apply Last Color* re-applies the complete style of the last used preset, or the last custom color.
- *Fill Text Background* is available whenever a name is re-drawn, including for styles with their own text
  color, and no longer depends on *Label Color* being ticked.

### Fixed

- With *Fill Text Background* on, the area behind a re-drawn name lost the row tint, and on selected or
  hovered rows was filled with the plain row color. The fill now uses the row's real background and
  restores the tint and zebra stripe behind the name.
- A color drag that resumed within the collapse timeout could fold a preceding discrete change (applying a
  preset, for example) into its undo step, so undoing the drag also undid that change.

### Compatibility

- Projects saved by 1.0 load with identical meaning: every color keeps its color and preset, its text
  color stays *not configured* (follows the Display Mode, as before), its marker follows the project
  marker shape, names keep the object's own color, and no Hierarchy mode, folder icon or prefab icon is
  enabled. Opening a project does not rewrite the settings file; it is written in the new format with the
  next change.
- Version 1.0 cannot read styles written by 1.1. Upgrade every member of a team together.
- Minimum Unity version unchanged: 2022.3 LTS. Verified on Unity 6000.3 and 6000.5.

---

## 1.0.0

Initial release.

### Added

- Hierarchy color decoration with three independent, combinable modes: row marker (dot, bar or square),
  translucent row background, and colored GameObject name.
- Color assignment for a single GameObject or a whole multi-selection, as one undoable operation.
- Optional apply scope: selection only, direct children, or all descendants.
- Color palette dropdown anchored at the mouse, with preset swatches, RGB channel sliders and a validated
  hexadecimal field.
- Color presets: ten defaults, editable, renameable, reorderable and deletable, with search.
- Color Studio window with Selection, Presets, Appearance, Assignments and About sections.
- Project Settings page under **Hierarchy Color Studio**, sharing the same controls as the window.
- Hierarchy context menu: Set Color, Apply Last Color, Clear Color, Open Color Studio.
- Shortcut Manager integration. **Alt + Shift + H** opens the window; the remaining commands are
  registered unbound so they cannot collide with existing bindings.
- Full Unity Undo/Redo support for every user-visible change, with continuous edits collapsed into one
  step.
- Persistence to `ProjectSettings/HierarchyColorStudio.asset` as diff-friendly text.
- `GlobalObjectId`-based object identity with round-trip verification, session-scoped fallback for objects
  that have no stable file identifier yet, and automatic promotion and re-keying when a scene is saved.
- Support for scene switching, additive scenes, scene unloading, unsaved scenes, and Prefab Mode.
- JSON import and export of colors and presets, with merge or replace.
- Maintenance tools: select colored objects, remove missing entries, clear all colors, restore factory
  defaults, save now.
- About section in the window and the Project Settings page, showing the version and the author's
  website and support address as selectable text.
- Light and dark Editor theme support with no hard-coded text colors.
- Recovery from a corrupt or hand-edited settings file, with the unreadable file preserved as `.corrupt`.
- Optional debug logging, off by default.

### Compatibility

- Minimum Unity version: 2022.3 LTS.
- Verified on Unity 6000.3 (32-bit instance id API) and Unity 6000.5 (`EntityId` API).
- Editor-only: one assembly restricted to the Editor platform, with no runtime code and no render pipeline
  dependency.

---

Hierarchy Color Studio is made by CryNet — <https://crynet.dev/> · <ucrynet@proton.me>
