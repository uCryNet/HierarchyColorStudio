# Changelog

All notable changes to Hierarchy Color Studio are documented here.
This project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
