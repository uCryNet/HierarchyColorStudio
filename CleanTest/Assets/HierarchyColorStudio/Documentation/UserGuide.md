# User Guide

Complete feature reference and architecture notes for Hierarchy Color Studio 1.0.0.

By CryNet · [crynet.dev](https://crynet.dev/) · [ucrynet@proton.me](mailto:ucrynet@proton.me)

---

## Contents

**Using the plugin**
- [Assigning colors](#assigning-colors)
- [The color palette](#the-color-palette)
- [Presets](#presets)
- [Display modes](#display-modes)
- [Appearance settings](#appearance-settings)
- [Parent and child coloring](#parent-and-child-coloring)
- [Multi-selection](#multi-selection)
- [Undo and redo](#undo-and-redo)
- [Keyboard shortcuts](#keyboard-shortcuts)
- [Scenes, additive scenes and Prefab Mode](#scenes-additive-scenes-and-prefab-mode)
- [Import and export](#import-and-export)
- [Maintenance](#maintenance)
- [About and support](#about-and-support)

**How it works**
- [Architecture overview](#architecture-overview)
- [Persistence](#persistence)
- [Why ProjectSettings](#why-projectsettings)
- [GameObject identification](#gameobject-identification)
- [Hierarchy rendering](#hierarchy-rendering)
- [Undo implementation](#undo-implementation)
- [Performance strategy](#performance-strategy)
- [Domain reload](#domain-reload)
- [Error handling and logging](#error-handling-and-logging)
- [Unity version compatibility](#unity-version-compatibility)
- [Editor-only isolation](#editor-only-isolation)
- [Package layout](#package-layout)
- [Public API](#public-api)

**Design decisions**
- [Why there is no Inspector section](#why-there-is-no-inspector-section)
- [Why presets reorder with buttons](#why-presets-reorder-with-buttons)
- [Why the palette has its own color picker](#why-the-palette-has-its-own-color-picker)

---

## Assigning colors

Three routes, all equivalent:

| Route | Where |
| --- | --- |
| Hierarchy context menu | Right-click → **Hierarchy Color → Set Color…** |
| Color Studio window | **Selection** section → **Color** field |
| Keyboard | **Edit → Shortcuts → Hierarchy Color Studio → Set Color** (unbound by default) |

The **Color** field in the window applies live while you drag inside Unity's color picker. A continuous
drag collapses into one undo step; pausing for about three quarters of a second starts a new one.

---

## The color palette

**Hierarchy Color → Set Color…** opens a dropdown anchored at the mouse:

- A header stating how many GameObjects the action will affect.
- **Apply To** — Selection Only, Direct Children or All Descendants.
- A grid of preset swatches. Hovering shows the preset name and hexadecimal value; clicking applies it
  and closes the dropdown.
- **Custom Color** — a preview swatch, a validated hexadecimal field, R/G/B sliders, **Apply**, and
  **Save As Preset**.
- **Clear** — removes the color from the targets. Disabled when none of them has one.
- **Open Color Studio…** — closes the dropdown and opens the window.

Accepted hexadecimal forms: `#RRGGBB`, `RRGGBB`, `#RRGGBBAA`, `RRGGBBAA`, `#RGB`, `#RGBA`. Leading and
trailing whitespace and the `#` are optional. Invalid input never changes anything; the field shows a hint
instead.

---

## Presets

Ten presets ship by default: Red, Orange, Amber, Green, Teal, Blue, Indigo, Violet, Pink, Slate.

In the **Presets** section of either the window or Project Settings:

| Control | Action |
| --- | --- |
| Swatch | Opens Unity's color picker. |
| Name field | Rename. |
| Hex field | Type a color. The border turns red while the text is not a valid color. |
| **Apply** | Assigns the preset to the current selection. Disabled with an empty selection. |
| **▲ ▼** | Move the preset up or down. |
| **×** | Delete the preset. |
| **Add Preset** | Appends a preset using your last used color. |
| **Restore Default Presets** | Replaces the list with the ten defaults. |
| Search | Filters by name or hexadecimal value. |

Deleting a preset never affects objects already colored with it: an assignment stores its own color, and
the preset id it records is only used to display the preset's name. A dangling preset id is harmless.

---

## Display modes

**Display Mode** is a combination of three independent decorations.

### Marker

A colored shape on the row.

```
  Player                                        ●
  Enemy                                         ●
  Environment                                   ●
```

Shape: **Dot**, **Bar** (a rounded vertical strip) or **Square**. Placement: **Row End** (default) or
**Before Icon**. Size: 4–14 pt.

### Row Tint

A translucent wash of the color across the row.

```
┌────────────────────────────────────────────────┐
│ Player                                         │
└────────────────────────────────────────────────┘
```

Opacity is capped at 0.65 so Unity's row text always stays readable. Extent is either the whole row or
from the object's indentation rightwards.

### Label Color

The GameObject's name re-drawn in the assigned color.

```
  Player          (blue)
  Enemy           (red)
  Environment     (green)
```

Unity draws the row before the plugin is called, so this paints the name a second time on top of Unity's
own. With opaque colors the result is clean. If you want crisper text, enable **Fill Text Background**,
which fills the name area with the configured row color first; check **Row Color (Dark Theme)** and
**Row Color (Light Theme)** under *Advanced* so the fill matches your Editor.

The default is **Marker + Row Tint**, chosen because neither decoration modifies Unity's own text, so the
Hierarchy stays exactly as readable as it was before installation.

---

## Appearance settings

### Selected rows

Unity draws its own highlight on selected rows. **Selected Rows** controls what the plugin adds on top:

- **Draw All** — every enabled decoration.
- **Marker Only** *(default)* — only the marker, so the selection highlight stays intact.
- **Hide** — nothing.

### Hovered rows

- **Ignore** *(default)* — hovering changes nothing.
- **Emphasize** — the row tint gets stronger under the pointer.
- **Suppress** — the row tint is hidden under the pointer, exposing Unity's hover highlight.

### Indentation handling

**Background Extent** decides whether the tint spans the full row width regardless of nesting depth
(**Full Row**, default), or starts where the object's icon starts and therefore steps in with each level
of nesting (**From Indent**).

### Text offset

Unity does not expose the exact x position at which it draws a row's name. The plugin uses 18 pt from the
row rect, which matches Unity's own tree view metrics (a 16 pt icon plus 2 pt of spacing) in every
supported version. The *Advanced* slider exists so you can correct it without a plugin update if a future
Unity release changes the layout.

---

## Parent and child coloring

Colors are **not** inherited. A child is colored only if it has its own assignment.

That is deliberate. Inheritance means a single click can silently repaint hundreds of rows, and it makes
"why is this object green?" hard to answer. Instead, applying to children is an explicit choice made per
operation through **Apply To**:

| Scope | Effect |
| --- | --- |
| **Selection Only** *(default)* | Only the objects you selected. |
| **Direct Children** | The selection plus each object's immediate children. |
| **All Descendants** | The selection plus every object beneath it. |

Overlapping selections are de-duplicated, so selecting both a parent and its child with
**All Descendants** produces one assignment each, not two for the child.

The default scope is configurable in **Appearance → Default Apply Scope**.

---

## Multi-selection

Every command works on the whole selection. Unity invokes a `GameObject/` menu item once per selected
object, so the plugin routes each command through a single-execution guard: one click produces one
operation, one store write and one undo step, no matter how many objects are selected.

---

## Undo and redo

Undoable operations:

- Assign a color
- Change a color
- Clear a color
- Apply a preset
- Apply to a multi-selection
- Clear all colors
- Add, rename, recolor, reorder or delete a preset
- Change appearance settings
- Import a color set
- Remove missing entries

Unity's own Undo system is used throughout; there is no parallel history. The undo step names are the ones
you see in **Edit → Undo …**.

Two behaviours worth knowing:

- **Continuous edits collapse.** Dragging a color picker or a slider produces one undo step, not one per
  frame.
- **A recompilation clears the undo stack.** Unity discards undo records for non-asset objects on domain
  reload. Your colors persist; the ability to undo past the reload does not.

---

## Keyboard shortcuts

Registered with Unity's Shortcut Manager under the category **Hierarchy Color Studio**, so they appear in
**Edit → Shortcuts** and can be rebound or removed.

| Command | Default binding |
| --- | --- |
| Open Color Studio | **Alt + Shift + H** |
| Set Color | *unbound* |
| Apply Last Color | *unbound* |
| Clear Color | *unbound* |

Only one default binding is claimed, and it uses a modifier combination Unity does not use, so installing
the plugin cannot take a shortcut away from you.

---

## Scenes, additive scenes and Prefab Mode

| Situation | Behaviour |
| --- | --- |
| Switching scenes | Colors of the newly opened scene appear; the previous scene's records stay stored. |
| Additive scenes | Every loaded scene's colors are shown simultaneously. |
| Unloading a scene | Its rows disappear; its records are untouched. |
| Unsaved (untitled) scene | Colors work, session-scoped, and become permanent on save. |
| Objects created since the last save | Same as above. |
| Saving a scene | Session-scoped colors are promoted to permanent ones, and any identifier Unity changed during the save is rewritten. |
| Entering Prefab Mode | Colors assigned inside that prefab appear. |
| Leaving Prefab Mode | Scene colors reappear. |
| Deleting a colored GameObject | Its row disappears. The stored record remains until you use **Remove Missing Entries** — deliberately, so that undoing a deletion restores the color too. |

A prefab colored in Prefab Mode does not color its instances in scenes: the prefab asset's objects and an
instance's objects have different identities, and coloring every instance from one edit would be
surprising. Color an instance in the scene to color that instance.

---

## Import and export

**Export Colors** writes a readable JSON file containing every preset and every persistent color
assignment:

```json
{
    "kind": "CryNet.HierarchyColorStudio.ColorSet",
    "version": 1,
    "presets": [ { "id": "…", "name": "Blue", "color": "3498DBFF" } ],
    "assignments": [ { "key": "GlobalObjectId_V1-2-…", "color": "3498DBFF", "preset": "…" } ]
}
```

**Import Colors** offers **Merge** (keeps existing colors, adds and overwrites by key) or **Replace**
(discards current colors and presets first). Either way the import is one undo step. Files that are not
color sets, and malformed files, are rejected without changing anything. Individual entries with an
unparsable color are skipped rather than aborting the import.

Because a key contains the owning scene's GUID and the object's local file id, an exported set is
meaningful in any project that has the same scene assets — which is what makes it useful for sharing a
scheme across a team. Session-scoped keys are never exported.

---

## Maintenance

| Command | Effect |
| --- | --- |
| **Select Colored Objects** | Selects every colored GameObject in the open scenes. |
| **Remove Missing Entries** | Deletes records whose GameObject no longer exists. Only scenes that are currently open are inspected, so records belonging to closed scenes are never lost. Undoable. |
| **Clear All Colors** | Removes every stored color. Confirmed by dialog, undoable. |
| **Restore Factory Defaults** | Resets appearance, presets and colors. Confirmed by dialog, undoable. |
| **Save Now** | Writes pending changes immediately. Only enabled when there are any. |

---

## About and support

The **About** section of the Color Studio window, and the matching block at the foot of the Project
Settings page, show the version, the author, and where to get help:

| | |
| --- | --- |
| Website | <https://crynet.dev/> |
| Email | <ucrynet@proton.me> |

Both values are drawn with `EditorGUILayout.SelectableLabel`, so you can select and copy them, but the
plugin never opens a browser or a mail client for you. A clickable link would mean calling
`Application.OpenURL`, which starts an external process — something the plugin states that it never does,
and a promise worth more than saving the reader one paste. The same section carries the
**Documentation** button, which reveals this folder in the Project window.

---

## Architecture overview

```
  Hierarchy window
        │  row callback (identifier, rect)
        ▼
  HierarchyRowRenderer ───────► AssignmentIndex          (one dictionary lookup, zero allocations)
        │                             ▲
        │                             │ rebuilt from the Editor update loop
        │                             │
  HierarchyColorService ──────────────┘                  (public API, undo, scope expansion)
        │
        ├──► ObjectIdentity            (GlobalObjectId keys, round-trip verified)
        ├──► HierarchyColorStore       (serialized settings, presets, assignments)
        └──► HierarchyColorStoreProvider ──► HierarchyColorStoreFile
                                                └─► ProjectSettings/HierarchyColorStudio.asset
```

`HierarchyColorStudioBootstrap` wires the Editor events. `EditorCompat` and `RowId` contain every piece of
Unity-version-specific code. The GUI layer (`ColorPaletteWindow`, `HierarchyColorStudioWindow`,
`AppearanceSectionGUI`, `PresetSectionGUI`, `HierarchyColorSettingsProvider`) reads and writes only
through the service.

---

## Persistence

Everything the project needs is in one serialized `ScriptableObject`, `HierarchyColorStore`:

- a schema version,
- appearance settings,
- the preset list,
- the assignment list, where each record is `{ key, color, presetId }` and the color is stored as an
  `RRGGBBAA` hexadecimal string.

`HierarchyColorStoreFile` reads and writes it with
`InternalEditorUtility.LoadSerializedFileAndForget` / `SaveToSerializedFileAndForget` in text mode — the
same mechanism Unity uses for its own project settings files.

`HierarchyColorStoreProvider` owns the single live instance, reloads it after a domain reload, and writes
changes on a short debounce so a bulk edit produces one file write. A pending write is always flushed
before an assembly reload and before the Editor quits.

Every load runs `Sanitize()`, which drops records that have no key or no parsable color, removes duplicate
keys, repairs presets with invalid colors, clamps every numeric setting into range and normalizes an
out-of-range schema version. A file that cannot be read at all is moved aside with a `.corrupt` suffix, a
warning is logged once, and the Editor continues with defaults. There is no code path in which bad data in
that file can stop the Editor from working.

---

## Why ProjectSettings

Four options were considered.

| Option | Verdict |
| --- | --- |
| Components on GameObjects | Rejected. Adds runtime data to scenes, dirties scenes, ships into builds, breaks on prefab overrides. The plugin's core promise is that this does not happen. |
| `ScriptableObject` asset under `Assets` | Rejected. Enters the AssetDatabase, so it costs import time, owns a GUID that can change or conflict, appears in the Project window as something a user can delete or accidentally reference, and can be pulled into a build by a stray reference. |
| `EditorPrefs` | Rejected for project data. It is per user and per machine, is not versionable, cannot be shared with a team, and has no meaningful size or structure guarantees. Used only for genuinely per-user state such as the last color used. |
| Text file in `ProjectSettings` | **Chosen.** Outside the AssetDatabase, so zero import cost and no GUID. Text, so it diffs and merges. Per project and committed with the project. Cannot enter a build. This is where Unity itself puts project-wide settings. |

`ScriptableSingleton<T>` would give roughly the same file handling with less code, but it does not let the
plugin control the instance's lifetime, its `hideFlags`, or the recovery path when the file is corrupt —
all of which matter here, and the last of which is a correctness requirement rather than a nicety.

---

## GameObject identification

Instance ids are not stable across Editor sessions, so they are never persisted. The persistent key is
`GlobalObjectId`, which combines the owning asset's GUID (the scene or the prefab) with the object's local
file id, and round-trips through a documented string form.

Assignment goes through `ObjectIdentity.TryBuildKeys`, which for a whole selection at once:

1. computes identifiers with `GlobalObjectId.GetGlobalObjectIdsSlow` (one batch call),
2. resolves them straight back to live objects (one batch call), and
3. accepts the identifier only if it resolves to the object it came from and its asset GUID is not empty.

Step 3 matters. An object in a scene that has never been saved has no asset GUID, and an object created
since the last save has a local file id that Unity will change when the scene is written. Persisting either
would silently lose the color. Objects that fail verification get a **session-scoped key** instead — the
live identifier, prefixed with `Session:` — which works for the rest of the Editor session and is
discarded when the Editor restarts. The window states clearly when any such color is in play.

When a scene is saved, `ReconcileAfterSceneSave` walks the colored objects of that scene, recomputes their
identifiers and rewrites any key that changed. Session-scoped colors become permanent at that moment. This
also covers **Save As**, where every identifier in the scene changes at once.

Prefab assets are supported through the same key: an object edited in Prefab Mode yields an identifier
referencing the prefab asset's GUID.

---

## Hierarchy rendering

The plugin draws from the Hierarchy row GUI callback, which Unity invokes for every visible row after it
has drawn the row itself.

The callback:

1. records the mouse position on a context click, so the palette can be anchored there later;
2. returns immediately unless the current event is a repaint;
3. reads the assigned color with a single dictionary lookup, and returns if there is none;
4. applies the selected-row and hovered-row rules;
5. draws up to three things.

Rounded shapes are drawn with `GUI.DrawTexture` and a corner radius over Unity's built-in white texture,
so no textures are created. The product icon is generated procedurally in code, so the package contains no
binary image assets.

Colored names are drawn with a cached `GUIStyle` cloned from `EditorStyles.label`. Unity's shared styles
are never mutated. The style is rebuilt if the Editor theme changes.

---

## Undo implementation

Every mutation calls `Undo.RegisterCompleteObjectUndo` on the store before changing it, then writes. A
complete-object snapshot is the right granularity here: the store is a small object, and one snapshot
covers an operation that touches any number of assignments, which is exactly what makes a multi-selection
assignment a single undo step.

Continuous interactions — a color picker drag, a slider drag — call `Undo.IncrementCurrentGroup` once at
the start and `Undo.CollapseUndoOperations` on every change, so the whole gesture is one step.

After an undo or redo, the plugin invalidates its key lookup and its resolution cache and repaints the
Hierarchy, because Unity restores serialized fields without notifying the object.

Operations that would change nothing do not register an undo step at all, so no undo entry is ever a no-op.

---

## Performance strategy

The Hierarchy GUI callback is the only hot path, and it is treated as such.

**In the callback**
- One `Dictionary` lookup keyed by the row identifier. Rows without a color cost a hash lookup and a
  return.
- No `Selection` access. `Selection.instanceIDs` allocates a new array per call, so the selection is
  cached in a `HashSet` and invalidated by `Selection.selectionChanged`.
- No `name` access. `UnityEngine.Object.name` allocates a string per call, so row names and their measured
  widths are cached and invalidated by `EditorApplication.hierarchyChanged`.
- No `AssetDatabase` calls, no `FindObjectsOfType`, no `Resources.FindObjectsOfTypeAll`, no scene
  traversal, no LINQ, no closures, no string concatenation, no `GUIContent` or `GUIStyle` construction.
- Non-repaint events return before doing any work.

**Outside the callback**
- Identity resolution happens in `EditorApplication.update`, and only when a dirty flag is set. Many
  invalidations in one frame collapse into one rebuild.
- Rebuilding has two stages with independent dirty flags: parsing persisted keys (invalidated by data
  changes) and resolving them to live identifiers (invalidated by scene, prefab-stage and Hierarchy
  changes). Editing a color does not re-resolve scene identifiers, and changing scenes does not re-parse
  keys.
- Resolution considers only entries whose asset GUID belongs to a scene that is currently open, or to the
  prefab open in Prefab Mode. A project with tens of thousands of colored objects across many scenes
  resolves only what is loaded. All of them go through one batched Unity call.
- In Prefab Mode, persisted asset identifiers do not resolve to the preview-scene objects on screen, so
  the plugin walks the prefab's contents instead — and only when the prefab that is open actually has
  colors. The contents of a single prefab are small and bounded.
- Saves are debounced, so dragging a color picker writes the file once.
- The Hierarchy is repainted when the resolved set actually changes, not on every frame.

**Scaling**

Cost is proportional to the number of *colored* objects in the *open* scenes, not to project size. Rows
without a color, scenes that are closed and assignments that do not resolve cost nothing per frame.

---

## Domain reload

No static state is assumed to survive a reload.

`HierarchyColorStudioBootstrap` is an `[InitializeOnLoad]` type whose installer removes each subscription
before adding it, so a reload — or a second call from another entry point — cannot produce duplicate
callbacks. The store instance is reloaded from disk on first access after a reload, and every cache is
rebuilt lazily.

Pending changes are flushed on `AssemblyReloadEvents.beforeAssemblyReload` and on
`EditorApplication.quitting`, so a recompilation cannot lose an edit.

Session-scoped keys must survive a reload but not a restart. A marker in `SessionState`, which itself
survives reloads but not restarts, distinguishes the two: on the first load of a new Editor session,
session-scoped records are discarded.

The plugin works with **Enter Play Mode Options** and domain reload disabled, because it holds no state
that a reload would otherwise reset.

---

## Error handling and logging

The Hierarchy callback wraps its work in a `try`/`catch`. The first three failures are reported once each
with a deduplication key; after that, drawing is disabled for the session and a single explanatory warning
is logged. A repaint loop can never flood the Console.

The same once-per-key rule covers unreadable and unwritable settings files, unparsable identifiers, failed
imports and exports, and failures to register an undo step. Every one of them degrades to a working Editor
with defaults.

`Debug.Log` is never called unless **Debug Logging** is enabled in *Advanced*, which is off by default.
Warnings that indicate real data problems are always reported, but only once each per session.

---

## Unity version compatibility

Two API differences require conditional compilation, and both boundaries are declared as `versionDefines`
in `Editor/CryNet.HierarchyColorStudio.Editor.asmdef`:

| Define | From | What changes |
| --- | --- | --- |
| `HCS_ENTITY_ID_API` | Unity 6000.5 | Unity replaced 32-bit instance ids with `UnityEngine.EntityId`, added `Object.GetEntityId`, `Selection.entityIds`, `EditorUtility.EntityIdToObject`, `GlobalObjectId.GlobalObjectIdentifiersToEntityIdsSlow` and `EditorApplication.hierarchyWindowItemByEntityIdOnGUI`, and deprecated the previous forms. |
| `HCS_PREFAB_STAGE_ASSET_PATH` | Unity 6000.0 | Unity 6 reinstated `PrefabStage.assetPath` and deprecated `PrefabStage.prefabAssetPath`, reversing the 2020.1 change. |

Everything that depends on the first is inside one type, `RowId`, which wraps the identifier so it is
never truncated and so the rest of the plugin can use it as a dictionary key without knowing its width.
Everything that depends on the second, plus the callback subscription and the undo-notification API, is
inside `EditorCompat`.

If a future Unity release moves one of these boundaries, the fix is to edit the expression in the assembly
definition. No other file needs to change.

Unity releases in the transition to `EntityId` deprecate the 32-bit API while not yet exposing `EntityId`
itself. In those versions the deprecated API is the only one available, so the notice is suppressed at the
three call sites in `RowId` rather than project-wide.

---

## Editor-only isolation

The package contains exactly one assembly, `CryNet.HierarchyColorStudio.Editor`, whose
`includePlatforms` is `["Editor"]`. There is no runtime assembly, no `MonoBehaviour`, no runtime script, no
manager object and no prefab. The plugin cannot be referenced from runtime code and cannot enter a player
build.

A single assembly was chosen deliberately. Splitting an Editor-only tool into core, GUI and persistence
assemblies would add compile-time coupling to manage and give the customer three assemblies to reason
about, with no functional benefit.

The `InternalsVisibleTo` attribute in `Editor/AssemblyInfo.cs` targets the development-only test assembly,
which is not part of the distributed package. It has no effect in a project where that assembly is absent.
The same file records the product title, company and copyright on the compiled assembly, so the DLL can be
attributed on its own.

---

## Package layout

```
Assets/HierarchyColorStudio/
├── Editor/
│   ├── CryNet.HierarchyColorStudio.Editor.asmdef
│   ├── AssemblyInfo.cs
│   ├── Core/          identity, index, service, store, bootstrap
│   ├── GUI/           row renderer, window, palette, settings page, menus
│   ├── Persistence/   settings file, provider, import/export, per-user state
│   └── Utilities/      hex, logging, undo, version compatibility, strings
├── Documentation/     README, GettingStarted, UserGuide, Troubleshooting, Changelog
└── LICENSE.md
```

The package contains no scenes, prefabs, materials, shaders or image files. Every graphic in the user
interface, including the window icon, is generated in code from Unity's own Editor GUI APIs.

There is no `package.json`. The plugin is distributed as a `.unitypackage` that installs under `Assets`,
which is not a UPM package layout; adding a manifest there would be misleading. If you prefer to consume
the plugin as an embedded UPM package, move the folder to `Packages/com.crynet.hierarchycolorstudio` and
add a manifest — the code makes no assumption about its own location.

The plugin locates its own documentation through its assembly definition asset rather than a hard-coded
path, so you can move or rename the top-level folder.

---

## Public API

The plugin is usable from your own Editor scripts. `CryNet.HierarchyColorStudio.HierarchyColorService`
exposes:

```csharp
bool  TryGetColor(GameObject target, out Color color);
void  Assign(GameObject target, Color color);
int   Assign(IReadOnlyList<GameObject> targets, Color color, string presetId = null,
             ApplyScope scope = ApplyScope.SelectionOnly, bool continuousEdit = false);
int   Clear(IReadOnlyList<GameObject> targets, ApplyScope scope = ApplyScope.SelectionOnly);
bool  AnyHasColor(IReadOnlyList<GameObject> targets);
int   ClearAll();
int   RemoveMissingAssignments();
int   SelectColoredObjects();
bool  ExportColors(string absolutePath);
int   ImportColors(string absolutePath, bool replace);
void  SaveNow();

IReadOnlyList<ColorPreset> Presets { get; }
bool  TryGetPreset(string name, out ColorPreset preset);
int   StoredAssignmentCount { get; }
int   ResolvedAssignmentCount { get; }
bool  HasSessionScopedAssignments { get; }
event Action Changed;
```

All of it is undoable and safe to call from menu items, windows and custom tooling. Calls with a null or
empty target list are no-ops rather than errors.

---

## Why there is no Inspector section

Showing the assigned color in the Inspector would require one of:

- a `[CustomEditor(typeof(GameObject))]` or `typeof(Transform)`, which replaces Unity's own inspector for
  every object in the project and would have to reimplement it;
- Unity's internal `Editor.finishedDefaultHeaderGUI` event, which is not public API and can change
  between releases.

Both trade a working Inspector, or forward compatibility, for a convenience. Stability matters more here,
so the same information is presented in the **Selection** section of the Color Studio window: the
selection count, a live color field, a per-object list with swatches, and Clear. Dock that window next to
the Inspector and the workflow is equivalent, with none of the risk.

---

## Why presets reorder with buttons

Unity's `ReorderableList` mutates the list before any callback runs, so an undo snapshot taken in the
callback would already describe the changed state, and undoing a reorder would not restore the previous
order. Move buttons let the snapshot be taken before the change, so reordering undoes correctly like every
other edit.

---

## Why the palette has its own color picker

A dropdown closes as soon as it loses focus. Opening Unity's modal color picker from inside one closes the
dropdown and discards the pending choice. The palette therefore offers channel sliders and a validated
hexadecimal field, both of which keep focus inside the dropdown, and defers to Unity's full color picker
in the Color Studio window and the Project Settings page, where it works correctly.
