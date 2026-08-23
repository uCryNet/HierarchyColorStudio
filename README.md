# Hierarchy Color Studio — development repository

Source repository for **Hierarchy Color Studio**, an Editor-only Unity extension that colors GameObjects
in the Hierarchy window.

This is the *development* repository. It holds the plugin's source, its tests, and the projects used to
verify it against several Unity versions. The customer-facing documentation lives with the plugin itself,
in [`Dev/Assets/HierarchyColorStudio/Documentation/`](Dev/Assets/HierarchyColorStudio/Documentation/) —
start with its [README](Dev/Assets/HierarchyColorStudio/Documentation/README.md) if you want to know what
the plugin does rather than how it is built.

By CryNet · <https://crynet.dev/> · <ucrynet@proton.me>

---

## Contents

- [Repository layout](#repository-layout)
- [Why three Unity projects](#why-three-unity-projects)
- [Requirements](#requirements)
- [Running the tests](#running-the-tests)
- [Verifying the older API branch](#verifying-the-older-api-branch)
- [Building the .unitypackage](#building-the-unitypackage)
- [Release checklist](#release-checklist)
- [What is deliberately not in this repository](#what-is-deliberately-not-in-this-repository)
- [License](#license)

---

## Repository layout

```
HierarchyColorStudio/
├── Dev/                      Unity 6000.5.5f1 — the working project
│   └── Assets/
│       ├── HierarchyColorStudio/    the shipped plugin; this folder is the product
│       │   ├── Editor/              one Editor-only assembly
│       │   ├── Documentation/       README, GettingStarted, UserGuide, Troubleshooting, Changelog
│       │   └── LICENSE.md
│       └── _Development/            never shipped
│           ├── Tests/Editor/        EditMode tests
│           └── Build/Editor/        the .unitypackage exporter
├── Dev63/                    Unity 6000.3.6f1 — compile check for the older-API branch
├── CleanTest/                Unity 6000.5.5f1 — empty project, for verifying a clean import
├── Dist/                     build and log output (not tracked)
└── README.md                 this file
```

The single rule that matters: **everything under `Assets/HierarchyColorStudio` ships, everything under
`Assets/_Development` does not.** The exporter enforces the boundary in one direction — it exports only
the first folder, and refuses to run if development files have drifted inside it.

---

## Why three Unity projects

The plugin compiles two different code paths, selected by `versionDefines` in
`Editor/CryNet.HierarchyColorStudio.Editor.asmdef`:

| Define | Unity | What changes |
| --- | --- | --- |
| `HCS_ENTITY_ID_API` | 6000.5 and later | `UnityEngine.EntityId` replaces 32-bit instance ids, including in the Hierarchy GUI callback |
| `HCS_PREFAB_STAGE_ASSET_PATH` | 6000.0 and later | `PrefabStage.assetPath` is reinstated and `prefabAssetPath` deprecated |

A single project can only ever compile one side of each `#if`. `Dev63` exists so the other side is
compiled too — without it, roughly half of `RowId.cs` and `EditorCompat.cs` would never reach the
compiler, and a typo there would ship.

`CleanTest` is an empty project kept for one purpose: importing the built `.unitypackage` into something
that has never seen the plugin, which is the only way to catch a missing meta file or a stray dependency.

---

## Requirements

- Unity **6000.5.5f1** for `Dev` and `CleanTest`
- Unity **6000.3.6f1** for `Dev63`
- Unity Test Framework (already in each project's manifest)

No other tooling, no package manager step, and no network access is needed to build or test.

---

## Running the tests

58 EditMode tests cover hex parsing, the store, persistence, import/export and the service API.

From the Editor: **Window → General → Test Runner → EditMode → Run All**.

Headless:

```bash
UNITY=/path/to/6000.5.5f1/Editor/Unity
REPO=/path/to/HierarchyColorStudio

"$UNITY" -batchmode -nographics -silent-crashes -disable-assembly-updater \
  -projectPath "$REPO/Dev" \
  -runTests -testPlatform EditMode \
  -testResults "$REPO/Dist/tests.xml" \
  -logFile "$REPO/Dist/tests.log"
```

Check `Dist/tests.xml` — the root element carries `total`, `passed` and `failed`. Do not rely on the exit
code alone.

---

## Verifying the older API branch

`Dev63` must be re-synced from `Dev` before it proves anything; it is a copy, not a link:

```bash
rsync -a --delete \
  "$REPO/Dev/Assets/HierarchyColorStudio/Editor/" \
  "$REPO/Dev63/Assets/HierarchyColorStudio/Editor/"

"$UNITY63" -batchmode -nographics -silent-crashes -disable-assembly-updater \
  -projectPath "$REPO/Dev63" -quit \
  -logFile "$REPO/Dist/compile-6000.3.log"
```

Then confirm the log is clean:

```bash
grep -c "error CS" "$REPO/Dist/compile-6000.3.log"   # expect 0
```

`Dev63` intentionally has no `Documentation/`, no `AssemblyInfo.cs` and no tests — it is a compiler
target, not a working project.

---

## Building the .unitypackage

The exporter is [`_Development/Build/Editor/PackageExporter.cs`](Dev/Assets/_Development/Build/Editor/PackageExporter.cs).

From the Editor: **Tools → Hierarchy Color Studio → Development → Export .unitypackage**.

Headless:

```bash
"$UNITY" -batchmode -nographics -silent-crashes -disable-assembly-updater \
  -projectPath "$REPO/Dev" \
  -executeMethod CryNet.HierarchyColorStudio.Build.PackageExporter.ExportFromCommandLine \
  -logFile "$REPO/Dist/export.log"
```

The result is `Dist/HierarchyColorStudio.unitypackage`. The method sets the process exit code, so this one
*can* be trusted in a script.

Two details are worth knowing before changing the exporter:

- It calls `AssetDatabase.Refresh` first. `AssetDatabase.ExportPackage` reads the AssetDatabase, not the
  file system, so a file Unity has not imported is skipped **silently**. Documentation edited outside the
  Editor has no meta file until that refresh runs, and would simply be absent from the package.
- It passes `ExportPackageOptions.Recurse` and deliberately not `IncludeDependencies`. For an Editor-only
  tool with no asset references, dependency walking can only add things that should not be there.

Verify the contents of a built package without importing it — a `.unitypackage` is a gzipped tar of one
directory per asset, each holding the asset, its `asset.meta` and its `pathname`:

```bash
tar tzf "$REPO/Dist/HierarchyColorStudio.unitypackage" | wc -l
for d in $(tar tzf "$REPO/Dist/HierarchyColorStudio.unitypackage" | grep pathname); do
  tar xzfO "$REPO/Dist/HierarchyColorStudio.unitypackage" "$d"; echo
done | sort
```

The listing must contain no `_Development` path.

---

## Release checklist

1. Bump `UiStrings.Version` and the version in `Documentation/README.md` and `Documentation/UserGuide.md`.
2. Add the release to `Documentation/Changelog.md`.
3. Run the tests on `Dev` — all must pass.
4. Re-sync `Dev63` and compile — zero errors.
5. Export the `.unitypackage`.
6. Inspect the archive listing; confirm no `_Development`.
7. Import it into `CleanTest` and confirm the Tools menu appears and a color can be assigned.

---

## What is deliberately not in this repository

- **No `Dist/` output.** The logs there record absolute paths and the build machine's user name, and the
  packages are reproducible from source. The whole folder is ignored.
- **No `Library/`, `Temp/`, `obj/`, `Logs/` or `UserSettings/`.** Unity regenerates all of them.
- **No `.csproj` or `.sln`.** Generated by Unity from the assembly definitions.
- **No `package.json`.** The plugin ships as a `.unitypackage` that installs under `Assets`, which is not
  a UPM layout; a manifest there would be misleading. Consuming it as an embedded UPM package works —
  move the folder to `Packages/com.crynet.hierarchycolorstudio` and add a manifest yourself. The code
  makes no assumption about its own location.

---

## License

The plugin is proprietary. See
[`Dev/Assets/HierarchyColorStudio/LICENSE.md`](Dev/Assets/HierarchyColorStudio/LICENSE.md) for the terms
that apply to distributed copies.

Copyright © 2026 CryNet. All rights reserved.
