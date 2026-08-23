using System.Reflection;
using System.Runtime.CompilerServices;

// The development-only test assembly exercises internal types directly. It is not part of the
// distributed package, and this attribute has no effect in projects where that assembly is absent.
[assembly: InternalsVisibleTo("CryNet.HierarchyColorStudio.Tests.Editor")]

// Authorship recorded on the compiled assembly, so the plugin can be attributed from the DLL alone —
// for example in Library/ScriptAssemblies or a decompiler — without the documentation next to it.
[assembly: AssemblyTitle("Hierarchy Color Studio")]
[assembly: AssemblyDescription("Colors GameObjects in Unity's Hierarchy window. Editor-only. https://crynet.dev/")]
[assembly: AssemblyCompany("CryNet")]
[assembly: AssemblyProduct("Hierarchy Color Studio")]
[assembly: AssemblyCopyright("Copyright © 2026 CryNet. ucrynet@proton.me")]
