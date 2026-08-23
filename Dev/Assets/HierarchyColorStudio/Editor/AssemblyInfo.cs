using System.Runtime.CompilerServices;

// The development-only test assembly exercises internal types directly. It is not part of the
// distributed package, and this attribute has no effect in projects where that assembly is absent.
[assembly: InternalsVisibleTo("CryNet.HierarchyColorStudio.Tests.Editor")]
