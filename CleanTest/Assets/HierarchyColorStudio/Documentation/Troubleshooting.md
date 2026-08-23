# Troubleshooting

---

## The Tools menu does not appear after import

**Cause.** Unity does not run any Editor code while the project has an unresolved compile error, and the
error is usually in unrelated code.

**Fix.** Clear the Console and check for compile errors from other scripts or packages. Once the project
compiles, the menu appears immediately.

---

## Colors do not show in the Hierarchy

Check in this order:

1. **Is drawing enabled?** The toggle at the top of the Color Studio window, and
   **Appearance → Enable Hierarchy Colors**, both control it. When it is off, the window shows a notice.
2. **Is a display mode selected?** If **Display Mode** has nothing ticked, nothing is drawn. Set it back to
   *Marker* + *Row Tint*.
3. **Is the row selected?** With **Selected Rows** set to *Marker Only* (the default) or *Hide*, a selected
   row shows less, or nothing. Click elsewhere to check.
4. **Is the scene the color belongs to open?** Colors resolve only for scenes that are currently loaded.
   Open the Color Studio window's **Assignments** section: it reports how many colors are stored and how
   many are visible in the open scenes.
5. **Was the object recreated?** A color belongs to a specific object. Deleting and re-creating a
   GameObject with the same name produces a different object, which has no color.

---

## A color disappeared after I saved the scene

This should not happen, and the plugin actively prevents it: when a scene is saved, colored objects in that
scene have their identifiers recomputed and rewritten if Unity changed them.

If you do see it, the object was almost certainly deleted or replaced rather than saved. Undo the change
and check the **Assignments** count before and after.

---

## A color disappeared after restarting Unity

**Cause.** The object had never been saved to a scene when you colored it, so its color was session-scoped.
The Color Studio window shows an explicit notice while any such color exists.

**Fix.** Save the scene before restarting; session-scoped colors are promoted to permanent ones at that
moment.

**Why.** An object in an unsaved scene, or created since the last save, has no stable file identifier yet.
The plugin refuses to persist a key it has verified as unstable, because doing so would appear to work and
then fail silently later.

---

## Colored names look shifted by a few pixels

**Cause.** Unity does not expose the exact x position at which it draws a row's name, so the plugin uses a
constant of 18 pt, which matches Unity's tree view metrics in all supported versions.

**Fix.** **Appearance → Advanced → Text Offset**. Nudge it until the colored name lines up.

---

## Colored names look slightly muddy

**Cause.** Unity draws the row before the plugin is called, so **Label Color** paints the name on top of
Unity's own. Anti-aliased glyph edges blend the two.

**Fix.** Either enable **Appearance → Fill Text Background** and set **Row Color (Dark Theme)** /
**Row Color (Light Theme)** under *Advanced* to match your Editor, or use *Marker* and *Row Tint* instead,
which never touch Unity's text.

---

## Row text is hard to read

**Cause.** **Background Opacity** is set high.

**Fix.** Lower it. The default is 0.22 and the maximum is capped at 0.65 for this reason. *Marker* mode
never affects readability at all.

---

## The row tint covers the scene visibility toggles

**Cause.** **Background Extent** is *Full Row*, which spans the whole row width.

**Fix.** Set it to *From Indent*, or lower the opacity.

---

## Colors do not appear in Prefab Mode

Colors assigned to a GameObject **inside** a prefab appear when that prefab is opened in Prefab Mode.
Colors assigned to a prefab **instance in a scene** belong to that instance and appear in the scene, not in
Prefab Mode. The two are different objects with different identities.

To color the prefab itself, open it in Prefab Mode and assign the color there.

---

## Coloring a prefab did not color its instances

That is by design. Each instance is a distinct object, and repainting every instance in the project from a
single edit would be surprising and hard to undo mentally. Color the instances you want, or use
**Apply To → All Descendants** on a parent.

---

## Undo does not go back far enough after a script change

**Cause.** Unity clears the undo stack for non-asset objects on domain reload, which happens on every
recompilation.

**Effect.** Your colors persist; only undo history before the reload is gone. This is a Unity behaviour and
applies to project settings generally.

---

## Undo restored the color but the Hierarchy still looks wrong

**Fix.** Click in the Hierarchy window to force a repaint. If it persists, this is a bug — the plugin
requests a Hierarchy repaint after every undo. Enable **Advanced → Debug Logging** and check the Console.

---

## Two people edited colors and the merge conflicted

**Cause.** `ProjectSettings/HierarchyColorStudio.asset` was edited on both sides.

**Fix.** The file is text and each record occupies its own lines, so conflicts are usually resolvable by
keeping both sides' records. If it is easier, take one side, then use **Import Colors** with **Merge** on
an export from the other.

---

## The Console warns that the settings file was moved to `.corrupt`

**Cause.** The file could not be read — usually a bad merge or an interrupted write.

**Effect.** The plugin moved it aside, logged one warning and continued with defaults. The Editor is not
harmed.

**Fix.** Inspect `ProjectSettings/HierarchyColorStudio.asset.corrupt`. If the damage is a merge marker you
can repair, fix it and rename the file back. Otherwise re-import an exported color set, or reassign.

---

## The Console warns that records were repaired or dropped

**Cause.** The settings file contained records with no key, no valid color, or duplicate keys — typically
after a hand edit or a merge.

**Effect.** They were removed on load and everything else was kept. The warning is shown once per session.

---

## Hierarchy color drawing was disabled for this session

**Cause.** Drawing raised the same exception several times, and the plugin disabled itself rather than log
from a repaint loop.

**Fix.** Enable **Advanced → Debug Logging**, then **Restore Factory Defaults** in the **Assignments**
section, which also re-enables drawing. Please report the logged exception.

---

## A preset I deleted is still shown on some objects

That is correct. An assignment stores its own color; the preset id it records is only used to display the
preset's name. Deleting a preset never changes objects that already have a color. Select them
(**Select Colored Objects**) and reassign if you want them changed.

---

## The Hierarchy feels slower in a very large scene

The plugin's per-row cost is one dictionary lookup, and it draws only for colored rows. If you can measure
a difference:

1. Turn off **Enable Hierarchy Colors** and compare. This isolates the plugin completely.
2. Set **Hovered Rows** to *Ignore*, which removes a per-row hit test.
3. Use **Remove Missing Entries** to drop records whose objects are gone.

If the difference persists with drawing disabled, the cause is elsewhere.

---

## The keyboard shortcut does nothing

Only **Open Color Studio** (**Alt + Shift + H**) has a default binding. *Set Color*, *Apply Last Color* and
*Clear Color* are intentionally left unbound so they cannot collide with your existing bindings. Assign
them in **Edit → Shortcuts → Hierarchy Color Studio**.

Shortcuts that act on a selection do nothing when nothing is selected.

---

## Can I remove the plugin cleanly?

Yes. Delete `Assets/HierarchyColorStudio`. Optionally delete
`ProjectSettings/HierarchyColorStudio.asset`, which holds your colors — keep it if you might reinstall.
Nothing else is left behind: no components, no scene changes and no files elsewhere in the project.

---

## Still stuck?

Get in touch — <ucrynet@proton.me> or <https://crynet.dev/>. Both are shown in the **About** section of
the Color Studio window, ready to copy.

To make a report actionable, please include:

1. Your Unity version, from **Help → About Unity**.
2. Anything the Console logged. Turning on **Appearance → Advanced → Debug Logging** first usually
   produces the detail that matters.
3. The **Assignments** counts from the Color Studio window, if colors are missing rather than misdrawn.
