# Hierarchy Color Studio

**Version 1.0.0** · Editor-only Unity extension by CryNet
· [crynet.dev](https://crynet.dev/) · [ucrynet@proton.me](mailto:ucrynet@proton.me)

Hierarchy Color Studio colors GameObjects in Unity's Hierarchy window so large scenes stay readable.
Pick one or more objects, assign a color, and the Hierarchy shows it as a marker, a row background, a
colored name, or any combination of the three.

No components are added to your GameObjects. No runtime code ships with the plugin. Nothing is written
into your scenes.

---

## Contents

- [Supported Unity versions](#supported-unity-versions)
- [Installation](#installation)
- [Quick start](#quick-start)
- [Features](#features)
- [Settings](#settings)
- [Where your data is stored](#where-your-data-is-stored)
- [Source control and teams](#source-control-and-teams)
- [Performance](#performance)
- [Privacy and offline use](#privacy-and-offline-use)
- [Known limitations](#known-limitations)
- [Troubleshooting](#troubleshooting)
- [Support](#support)
- [Documentation index](#documentation-index)

---

## Supported Unity versions

| Unity release | Status |
| --- | --- |
| 2022.3 LTS | Minimum supported version |
| Unity 6 LTS (6000.0.x) | Supported |
| Unity 6 tech releases up to 6000.5 | Supported and verified |

The plugin adapts to two Unity API changes automatically:

- Unity replaced 32-bit instance ids with `UnityEngine.EntityId` and deprecated the previous API,
  including the Hierarchy GUI callback.
- Unity 6 reinstated `PrefabStage.assetPath` and deprecated `PrefabStage.prefabAssetPath`, reversing an
  earlier change.

Both boundaries are declared in one place, as `versionDefines` in
`Editor/CryNet.HierarchyColorStudio.Editor.asmdef`. Nothing else in the plugin depends on the Unity
version. See [UserGuide.md](UserGuide.md#unity-version-compatibility) for details.

No render pipeline is required. The plugin works identically in Built-in, URP and HDRP projects. It ships
no scenes, materials or shaders, so it cannot pull in a pipeline dependency.

---

## Installation

1. Import `HierarchyColorStudio.unitypackage` (**Assets → Import Package → Custom Package…**).
2. Wait for the Editor to finish compiling.

That is the whole installation. The plugin activates itself, and colored rows appear in the Hierarchy as
soon as you assign a color. There is no setup step, no prefab to place, and no scene to modify.

The package installs into a single top-level folder, `Assets/HierarchyColorStudio`, which you can move or
rename freely.

---

## Quick start

1. Select one or more GameObjects in the Hierarchy.
2. Right-click → **Hierarchy Color → Set Color…**
3. Click a preset swatch.

To remove a color, right-click → **Hierarchy Color → Clear Color**.

Everything is undoable with **Ctrl/Cmd + Z**, including an assignment made to a whole multi-selection.

A four-minute walkthrough is in [GettingStarted.md](GettingStarted.md).

---

## Features

- **Four ways to show a color** — row marker (dot, bar or square), translucent row background, colored
  GameObject name, or any combination.
- **Multi-selection** — one color applied to any number of GameObjects, as a single undo step.
- **Color presets** — ten sensible defaults, fully editable: rename, recolor, reorder, add, delete.
- **Custom colors** — RGB channel sliders and a validated hexadecimal field (`#FF6B35`, `FF6B35FF`,
  `#F63`, all accepted).
- **Apply to children** — optional, off by default, available as *direct children* or *all descendants*.
- **Native undo/redo** — every change goes through Unity's own Undo system.
- **Prefab Mode support** — colors assigned inside a prefab appear whenever that prefab is opened.
- **Multi-scene support** — additive scenes, scene loading and unloading, and unsaved scenes.
- **Import / export** — share a color scheme with your team as a readable JSON file.
- **Maintenance tools** — select every colored object, remove entries whose object no longer exists,
  clear all colors, restore factory defaults.
- **Project Settings page** and a dedicated **Color Studio window**, both showing the same controls.
- **Keyboard shortcuts** through Unity's Shortcut Manager.
- **Light and dark Editor themes**, with no hard-coded text colors.
- **Editor-only** — one assembly, restricted to the Editor platform. It cannot enter a player build.

---

## Settings

Both **Tools → Hierarchy Color Studio → Color Studio Window** and
**Edit → Project Settings → Hierarchy Color Studio** show the same four sections.

### Appearance

| Setting | Description |
| --- | --- |
| Enable Hierarchy Colors | Turns drawing on or off without deleting any assigned color. |
| Display Mode | Any combination of Marker, Row Tint and Label Color. |
| Marker Shape / Placement / Size | Dot, bar or square; at the row end or before the icon; 4–14 pt. |
| Background Opacity | 0.04–0.65. Capped so row text stays readable. |
| Background Extent | Whole row, or starting at the object's indentation. |
| Text Brightness | Multiplies the color used for the GameObject name. |
| Fill Text Background | Fills the name area before drawing colored text. Off by default. |
| Selected Rows | Draw all decorations, marker only (default), or nothing. |
| Hovered Rows | Ignore (default), emphasize the background, or suppress it. |
| Default Apply Scope | Selection only (default), direct children, or all descendants. |
| Text Offset *(Advanced)* | Distance from the row rect to the name. Only needed if colored names look misaligned. |
| Row Color (Dark / Light Theme) *(Advanced)* | Fill color used by *Fill Text Background*. |
| Debug Logging *(Advanced)* | Verbose Console diagnostics. Off by default. |

### Presets

Add, rename, recolor, reorder and delete presets. Search filters the list by name or hexadecimal value;
reordering is disabled while a search filter is active so the ordering buttons cannot act on a filtered
view. **Apply** assigns a preset to the current selection.

### Assignments

Counts of stored and currently visible colors, plus **Select Colored Objects**,
**Remove Missing Entries**, **Export Colors**, **Import Colors**, **Clear All Colors** and
**Restore Factory Defaults**.

### About

Version, author, website and support address, and a shortcut to this documentation. The website and the
address are selectable text rather than links, because opening a browser or a mail client would mean the
plugin starts an external process — see [Privacy and offline use](#privacy-and-offline-use).

---

## Where your data is stored

Colors, presets and appearance settings are written to:

```
ProjectSettings/HierarchyColorStudio.asset
```

That location is deliberate:

- It is **outside `Assets`**, so the file never enters the AssetDatabase. It costs no import time, owns
  no GUID that could change, and cannot be accidentally referenced by a build.
- It is **text**, so it diffs and merges in source control.
- It is **per project**, next to Unity's own project settings, which is where a project-wide Editor
  preference belongs.
- It is **not a scene or a prefab**, so assigning a color never marks your scenes dirty and never touches
  your GameObjects.

Per-user conveniences that are not project data — the last color you used, the apply scope you selected —
are kept in `EditorPrefs` instead, so they do not create source-control noise.

The reasoning behind the alternatives that were considered is in
[UserGuide.md](UserGuide.md#why-projectsettings).

---

## Source control and teams

Commit `ProjectSettings/HierarchyColorStudio.asset` to share colors with your team. Each record is one
key/color pair on its own lines, so concurrent edits usually merge cleanly.

If you prefer not to commit the file, use **Export Colors** / **Import Colors** to exchange a color
scheme as a standalone JSON file.

---

## Performance

The Hierarchy GUI callback runs for every visible row of every repaint, so it does exactly one dictionary
lookup and, for a colored row, at most three draw calls. It allocates nothing.

All identity resolution happens outside the callback, in the Editor update loop, and only for colors that
belong to a scene that is currently open. A project with 50,000 colored objects across 200 scenes resolves
only the entries of the scenes you actually have loaded.

The full strategy is documented in [UserGuide.md](UserGuide.md#performance-strategy).

---

## Privacy and offline use

The plugin is entirely local. It makes no network requests, contains no analytics, telemetry, ads,
license check or activation step, starts no external processes, and reads and writes nothing outside your
Unity project except the file you choose in an explicit Import or Export dialog.

---

## Known limitations

- **Colored names are drawn over Unity's own label.** Unity draws the row before the plugin is called, so
  *Label Color* paints the name a second time on top. With opaque colors this is visually clean; if you
  want a crisper result, enable *Fill Text Background* and check the two row colors under *Advanced*.
- **Text Offset is a constant, not a query.** Unity does not expose the exact position at which it draws
  a row's name. The default of 18 pt matches every supported version; the *Advanced* slider exists in
  case a future release changes the layout.
- **Colors are not inherited.** A parent's color does not automatically apply to its children. Applying
  to children is an explicit action, so no operation can silently change hundreds of rows. See
  [UserGuide.md](UserGuide.md#parent-and-child-coloring).
- **A prefab colored in Prefab Mode does not color its scene instances.** The two are separate objects
  with separate identities, and colouring every instance of a prefab from one edit would be surprising.
- **Objects that have never been saved use session-scoped colors.** A color assigned to a GameObject in an
  unsaved scene, or to an object created since the last save, is remembered for the current Editor session
  and becomes permanent when you save the scene. This is explained in the window when it applies.
- **The undo history does not survive a script recompilation.** Unity clears the undo stack for non-asset
  objects on domain reload. The colors themselves persist; only the ability to undo past that point does.
- **There is no Inspector section.** This was a deliberate omission — see
  [UserGuide.md](UserGuide.md#why-there-is-no-inspector-section).

---

## Troubleshooting

Common issues and their causes are in [Troubleshooting.md](Troubleshooting.md).

---

## Support

Bug reports, questions and feature requests are welcome. Please include your Unity version and, if the
Console logged anything, the message.

| | |
| --- | --- |
| Website | <https://crynet.dev/> |
| Email | <ucrynet@proton.me> |
| Author | CryNet |

---

## Documentation index

| Document | Contents |
| --- | --- |
| [GettingStarted.md](GettingStarted.md) | Four-minute setup and first colors. |
| [UserGuide.md](UserGuide.md) | Every feature, the architecture, and the design decisions. |
| [Troubleshooting.md](Troubleshooting.md) | Symptoms, causes and fixes. |
| [Changelog.md](Changelog.md) | Version history. |
| [../LICENSE.md](../LICENSE.md) | License terms. |
