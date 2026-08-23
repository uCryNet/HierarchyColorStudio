# Getting Started

Four minutes from import to a color-coded Hierarchy.

---

## 1. Import (30 seconds)

**Assets → Import Package → Custom Package…** → select `HierarchyColorStudio.unitypackage` → **Import**.

Wait for the Editor to finish compiling. There is nothing else to install and nothing to configure.

You should see a new menu: **Tools → Hierarchy Color Studio**. If it is missing, check the Console for
compile errors from other packages in your project — Unity does not run any Editor code while the project
has an unresolved compile error.

---

## 2. Color your first object (30 seconds)

1. Select any GameObject in the Hierarchy.
2. Right-click it → **Hierarchy Color → Set Color…**
3. Click a swatch.

The row now shows a colored dot at its right edge and a soft wash of that color across the row. That is
the default display mode.

Press **Ctrl/Cmd + Z**. The color is removed — every change goes through Unity's own Undo system.
Press **Ctrl/Cmd + Shift + Z** to bring it back.

---

## 3. Color a whole group (1 minute)

1. Select several GameObjects (**Ctrl/Cmd + click**, or **Shift + click** for a range).
2. Right-click → **Hierarchy Color → Set Color…**
3. Click a swatch.

All of them are colored at once, and a single undo removes the whole operation.

To color a parent together with everything beneath it, set **Apply To** in the palette to
**All Descendants** before clicking a swatch. It is set to **Selection Only** by default so that no click
can unexpectedly change hundreds of rows.

---

## 4. Choose how colors look (1 minute)

Open **Tools → Hierarchy Color Studio → Color Studio Window**, or
**Edit → Project Settings → Hierarchy Color Studio**. Both show the same controls.

Under **Appearance → Display Mode**, tick any combination of:

- **Marker** — a colored dot, bar or square on the row.
- **Row Tint** — a translucent wash across the row.
- **Label Color** — the GameObject's name drawn in the assigned color.

Good starting points:

| Preference | Display Mode | Notes |
| --- | --- | --- |
| Subtle (default) | Marker + Row Tint | Readable at any opacity, never touches Unity's own text. |
| Strongest signal | Row Tint + Label Color | Raise *Background Opacity* to about 0.35. |
| Minimal | Marker only, shape *Bar* | A thin colored strip at the row's edge. |

---

## 5. Make the presets yours (1 minute)

In the **Presets** section:

- Click a swatch to open Unity's color picker.
- Type over the name.
- Type a hexadecimal value (`#3498DB`, `3498DBFF` and `#39D` are all accepted). The field turns red while
  the value is not a valid color.
- Use **▲ ▼** to reorder, **×** to delete, **Add Preset** to create one.
- **Apply** assigns a preset to the current selection.

Every preset edit is undoable.

---

## Next steps

- Assign keyboard shortcuts in **Edit → Shortcuts → Hierarchy Color Studio**. Only *Open Color Studio*
  (**Alt + Shift + H**) has a default binding; *Set Color*, *Apply Last Color* and *Clear Color* are left
  unbound so they cannot collide with an existing binding of yours.
- Commit `ProjectSettings/HierarchyColorStudio.asset` to share colors with your team.
- Read [UserGuide.md](UserGuide.md) for the full feature reference.

---

## Support

Questions, bug reports and feature requests are welcome.

| | |
| --- | --- |
| Website | <https://crynet.dev/> |
| Email | <ucrynet@proton.me> |

Both are also shown in the **About** section of the Color Studio window, where you can copy them.
