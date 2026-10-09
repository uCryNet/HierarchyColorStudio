using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>Groups of the icon picker.</summary>
    internal enum IconCategory
    {
        General,
        Status,
        Rendering,
        Audio,
        Physics,
        Animation,
        UserInterface,
        Navigation,
        Scripting
    }

    /// <summary>An entry of the icon picker: a Unity built-in icon name and how it is presented.</summary>
    internal readonly struct BuiltinIcon
    {
        /// <summary>Name of the icon as Unity knows it, without the dark-theme <c>d_</c> prefix.</summary>
        internal readonly string Name;

        /// <summary>Short label shown as a tooltip.</summary>
        internal readonly string Label;

        /// <summary>Picker group.</summary>
        internal readonly IconCategory Category;

        internal BuiltinIcon(string name, string label, IconCategory category)
        {
            Name = name;
            Label = label;
            Category = category;
        }
    }

    /// <summary>
    /// Resolves Unity's own built-in Editor icons by name. The plugin ships no icon of its own.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Icons are loaded with <see cref="EditorGUIUtility.Load(string)"/>, which is public, looks the name
    /// up in the Editor's resource bundle, and returns <c>null</c> without logging when the name does not
    /// exist. <see cref="EditorGUIUtility.IconContent(string)"/> was rejected: it logs an error for an
    /// unknown name and caches its result per name, so it keeps returning the previous theme's texture
    /// after the Editor theme is switched.
    /// </para>
    /// <para>
    /// The lookup reproduces what Unity does for its own icons: the dark theme prefers the <c>d_</c>
    /// variant, and a high-density display prefers the <c>@2x</c> variant, each falling back to the plain
    /// name. Results — including misses — are cached per name and invalidated when the theme or the display
    /// density changes, so a lookup happens once per icon rather than once per repaint.
    /// </para>
    /// <para>
    /// Every name in <see cref="Catalog"/> was verified to exist in Unity 2022.3, 6000.3 and 6000.5. Names
    /// outside the catalog are accepted, and simply draw nothing in a Unity version that lacks them.
    /// </para>
    /// </remarks>
    internal static class BuiltinIcons
    {
        private const string DarkPrefix = "d_";
        private const string HighDensitySuffix = "@2x";

        private static readonly BuiltinIcon[] s_Catalog =
        {
            new BuiltinIcon("Folder Icon", "Folder", IconCategory.General),
            new BuiltinIcon("FolderOpened Icon", "Open Folder", IconCategory.General),
            new BuiltinIcon("FolderEmpty Icon", "Empty Folder", IconCategory.General),
            new BuiltinIcon("FolderFavorite Icon", "Favorite Folder", IconCategory.General),
            new BuiltinIcon("Favorite Icon", "Favorite", IconCategory.General),
            new BuiltinIcon("GameObject Icon", "GameObject", IconCategory.General),
            new BuiltinIcon("Prefab Icon", "Prefab", IconCategory.General),
            new BuiltinIcon("PrefabVariant Icon", "Prefab Variant", IconCategory.General),
            new BuiltinIcon("PrefabModel Icon", "Model", IconCategory.General),
            new BuiltinIcon("SceneAsset Icon", "Scene", IconCategory.General),
            new BuiltinIcon("Transform Icon", "Transform", IconCategory.General),
            new BuiltinIcon("Settings Icon", "Settings", IconCategory.General),
            new BuiltinIcon("GameManager Icon", "Manager", IconCategory.General),
            new BuiltinIcon("Search Icon", "Search", IconCategory.General),

            new BuiltinIcon("console.infoicon", "Info", IconCategory.Status),
            new BuiltinIcon("console.warnicon", "Warning", IconCategory.Status),
            new BuiltinIcon("console.erroricon", "Error", IconCategory.Status),

            new BuiltinIcon("Camera Icon", "Camera", IconCategory.Rendering),
            new BuiltinIcon("Light Icon", "Light", IconCategory.Rendering),
            new BuiltinIcon("DirectionalLight Icon", "Directional Light", IconCategory.Rendering),
            new BuiltinIcon("Spotlight Icon", "Spot Light", IconCategory.Rendering),
            new BuiltinIcon("ReflectionProbe Icon", "Reflection Probe", IconCategory.Rendering),
            new BuiltinIcon("LightProbeGroup Icon", "Light Probes", IconCategory.Rendering),
            new BuiltinIcon("MeshRenderer Icon", "Mesh Renderer", IconCategory.Rendering),
            new BuiltinIcon("SkinnedMeshRenderer Icon", "Skinned Mesh", IconCategory.Rendering),
            new BuiltinIcon("SpriteRenderer Icon", "Sprite", IconCategory.Rendering),
            new BuiltinIcon("ParticleSystem Icon", "Particles", IconCategory.Rendering),
            new BuiltinIcon("LineRenderer Icon", "Line", IconCategory.Rendering),
            new BuiltinIcon("TrailRenderer Icon", "Trail", IconCategory.Rendering),
            new BuiltinIcon("Material Icon", "Material", IconCategory.Rendering),
            new BuiltinIcon("Mesh Icon", "Mesh", IconCategory.Rendering),
            new BuiltinIcon("LODGroup Icon", "LOD Group", IconCategory.Rendering),
            new BuiltinIcon("Terrain Icon", "Terrain", IconCategory.Rendering),
            new BuiltinIcon("Tree Icon", "Tree", IconCategory.Rendering),
            new BuiltinIcon("WindZone Icon", "Wind", IconCategory.Rendering),

            new BuiltinIcon("AudioSource Icon", "Audio Source", IconCategory.Audio),
            new BuiltinIcon("AudioListener Icon", "Audio Listener", IconCategory.Audio),
            new BuiltinIcon("AudioMixerController Icon", "Audio Mixer", IconCategory.Audio),
            new BuiltinIcon("AudioReverbZone Icon", "Reverb Zone", IconCategory.Audio),

            new BuiltinIcon("Rigidbody Icon", "Rigidbody", IconCategory.Physics),
            new BuiltinIcon("Rigidbody2D Icon", "Rigidbody 2D", IconCategory.Physics),
            new BuiltinIcon("BoxCollider Icon", "Box Collider", IconCategory.Physics),
            new BuiltinIcon("SphereCollider Icon", "Sphere Collider", IconCategory.Physics),
            new BuiltinIcon("CharacterController Icon", "Character", IconCategory.Physics),

            new BuiltinIcon("Animator Icon", "Animator", IconCategory.Animation),
            new BuiltinIcon("Animation Icon", "Animation", IconCategory.Animation),
            new BuiltinIcon("AnimatorController Icon", "Animator Controller", IconCategory.Animation),
            new BuiltinIcon("PlayableDirector Icon", "Timeline", IconCategory.Animation),
            new BuiltinIcon("Avatar Icon", "Avatar", IconCategory.Animation),

            new BuiltinIcon("Canvas Icon", "Canvas", IconCategory.UserInterface),
            new BuiltinIcon("EventSystem Icon", "Event System", IconCategory.UserInterface),
            new BuiltinIcon("RectTransform Icon", "Rect Transform", IconCategory.UserInterface),
            new BuiltinIcon("Image Icon", "Image", IconCategory.UserInterface),
            new BuiltinIcon("Text Icon", "Text", IconCategory.UserInterface),
            new BuiltinIcon("Button Icon", "Button", IconCategory.UserInterface),

            new BuiltinIcon("NavMeshAgent Icon", "NavMesh Agent", IconCategory.Navigation),
            new BuiltinIcon("NavMeshObstacle Icon", "NavMesh Obstacle", IconCategory.Navigation),
            new BuiltinIcon("Grid Icon", "Grid", IconCategory.Navigation),
            new BuiltinIcon("Tilemap Icon", "Tilemap", IconCategory.Navigation),
            new BuiltinIcon("SortingGroup Icon", "Sorting Group", IconCategory.Navigation),

            new BuiltinIcon("cs Script Icon", "C# Script", IconCategory.Scripting),
            new BuiltinIcon("ScriptableObject Icon", "Scriptable Object", IconCategory.Scripting),
            new BuiltinIcon("TextAsset Icon", "Text Asset", IconCategory.Scripting),
            new BuiltinIcon("VideoPlayer Icon", "Video Player", IconCategory.Scripting)
        };

        private static readonly Dictionary<string, int> s_Handles = new Dictionary<string, int>(32, StringComparer.OrdinalIgnoreCase);
        private static readonly List<string> s_Names = new List<string>(32);
        private static readonly List<Texture> s_Textures = new List<Texture>(32);
        private static readonly List<bool> s_Resolved = new List<bool>(32);

        private static bool s_StateCaptured;
        private static bool s_ProSkin;
        private static bool s_HighDensity;

        /// <summary>The curated icons offered by the icon picker, in display order.</summary>
        internal static IReadOnlyList<BuiltinIcon> Catalog => s_Catalog;

        /// <summary>
        /// Puts an icon name in canonical form: trimmed, without a <c>@2x</c> suffix, and an empty string for
        /// none. The dark-theme prefix is kept, because a name may exist only in that form.
        /// </summary>
        /// <param name="name">Name to normalize.</param>
        internal static string NormalizeName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            string trimmed = name.Trim();
            if (trimmed.EndsWith(HighDensitySuffix, StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed.Substring(0, trimmed.Length - HighDensitySuffix.Length).TrimEnd();

            return trimmed;
        }

        /// <summary>Returns the label of a catalog icon, or the name itself for any other icon.</summary>
        /// <param name="name">Icon name.</param>
        internal static string GetLabel(string name)
        {
            string normalized = NormalizeName(name);
            for (int i = 0; i < s_Catalog.Length; i++)
            {
                if (string.Equals(s_Catalog[i].Name, normalized, StringComparison.OrdinalIgnoreCase))
                    return s_Catalog[i].Label;
            }

            return normalized;
        }

        /// <summary>
        /// Returns a small integer standing for an icon name, so the Hierarchy callback can find the texture
        /// with an array access instead of hashing a string on every repaint.
        /// </summary>
        /// <param name="name">Icon name.</param>
        /// <returns>The handle, or <c>-1</c> for an empty name.</returns>
        internal static int GetHandle(string name)
        {
            string normalized = NormalizeName(name);
            if (normalized.Length == 0)
                return -1;

            if (s_Handles.TryGetValue(normalized, out int handle))
                return handle;

            handle = s_Names.Count;
            s_Names.Add(normalized);
            s_Textures.Add(null);
            s_Resolved.Add(false);
            s_Handles.Add(normalized, handle);
            return handle;
        }

        /// <summary>Returns the texture of a handle for the current theme, or <c>null</c> when Unity has no such icon.</summary>
        /// <param name="handle">Handle returned by <see cref="GetHandle"/>.</param>
        internal static Texture GetTexture(int handle)
        {
            if (handle < 0 || handle >= s_Names.Count)
                return null;

            EnsureCacheMatchesDisplay();

            var texture = s_Textures[handle];
            // A cached texture can only become invalid if Unity unloads its own resources; reload it then.
            if (!s_Resolved[handle] || (!ReferenceEquals(texture, null) && texture == null))
            {
                texture = Load(s_Names[handle]);
                s_Textures[handle] = texture;
                s_Resolved[handle] = true;
            }

            return texture;
        }

        /// <summary>Returns the texture of an icon for the current theme, or <c>null</c> when Unity has no such icon.</summary>
        /// <param name="name">Icon name.</param>
        internal static Texture GetTexture(string name)
        {
            return GetTexture(GetHandle(name));
        }

        /// <summary><c>true</c> when the running Unity version has an icon with this name.</summary>
        /// <param name="name">Icon name.</param>
        internal static bool Exists(string name)
        {
            return GetTexture(name) != null;
        }

        /// <summary>Drops every cached texture. The next lookup reloads it.</summary>
        internal static void ClearCache()
        {
            for (int i = 0; i < s_Resolved.Count; i++)
            {
                s_Resolved[i] = false;
                s_Textures[i] = null;
            }
        }

        private static void EnsureCacheMatchesDisplay()
        {
            bool proSkin = EditorGUIUtility.isProSkin;
            bool highDensity = EditorGUIUtility.pixelsPerPoint > 1f;
            if (s_StateCaptured && proSkin == s_ProSkin && highDensity == s_HighDensity)
                return;

            s_StateCaptured = true;
            s_ProSkin = proSkin;
            s_HighDensity = highDensity;
            ClearCache();
        }

        private static Texture Load(string name)
        {
            Texture texture = null;
            if (s_ProSkin && !name.StartsWith(DarkPrefix, StringComparison.Ordinal))
                texture = LoadDensityVariant(DarkPrefix + name);

            return texture != null ? texture : LoadDensityVariant(name);
        }

        private static Texture LoadDensityVariant(string name)
        {
            if (s_HighDensity)
            {
                var highDensity = LoadTexture(name + HighDensitySuffix);
                if (highDensity != null)
                    return highDensity;
            }

            return LoadTexture(name);
        }

        private static Texture LoadTexture(string name)
        {
            try
            {
                return EditorGUIUtility.Load(name) as Texture2D;
            }
            catch (Exception exception)
            {
                // Unity throws only when its own resource bundle is unavailable. Treat it as a missing icon.
                StudioLog.ExceptionOnce("icon-load", exception, "A built-in icon could not be loaded.");
                return null;
            }
        }
    }
}
