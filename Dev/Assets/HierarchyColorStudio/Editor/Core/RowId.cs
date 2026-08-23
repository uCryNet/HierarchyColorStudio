using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Identifies a live Editor object for the duration of an Editor session.
    /// </summary>
    /// <remarks>
    /// Unity changed this identifier from a 32-bit instance id to <c>UnityEngine.EntityId</c> during the
    /// Unity 6 cycle, and hard-deprecated the older API. Wrapping it keeps that difference in a single
    /// type instead of spreading conditional compilation across the plugin, and keeps the underlying
    /// value strongly typed so a 64-bit identifier is never silently truncated.
    /// The identifier is only ever used as a runtime cache key. Persisted data uses
    /// <see cref="GlobalObjectId"/>, except for objects that do not have a file identifier yet — see
    /// <see cref="ObjectIdentity"/>.
    /// </remarks>
    internal readonly struct RowId : IEquatable<RowId>
    {
#if HCS_ENTITY_ID_API
        private readonly EntityId m_Value;

        /// <summary>Wraps a native identifier.</summary>
        /// <param name="value">Native identifier value.</param>
        internal RowId(EntityId value)
        {
            m_Value = value;
        }

        /// <summary>The wrapped native identifier.</summary>
        internal EntityId Value => m_Value;

        /// <summary><c>true</c> when the identifier refers to an object.</summary>
        internal bool IsValid => m_Value.IsValid();

        /// <summary>Returns the identifier of an object.</summary>
        /// <param name="target">Object to identify.</param>
        internal static RowId Of(UnityEngine.Object target)
        {
            return target != null ? new RowId(target.GetEntityId()) : default;
        }

        /// <summary>Resolves the identifier back to an object, or <c>null</c>.</summary>
        internal UnityEngine.Object ToObject()
        {
            return IsValid ? EditorUtility.EntityIdToObject(m_Value) : null;
        }

        /// <summary>Formats the identifier for storage in a session-scoped key.</summary>
        internal string ToPersistString()
        {
            return EntityId.ToULong(m_Value).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Parses an identifier produced by <see cref="ToPersistString"/>.</summary>
        /// <param name="text">Text to parse.</param>
        /// <param name="id">Receives the parsed identifier.</param>
        internal static bool TryParsePersistString(string text, out RowId id)
        {
            if (ulong.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong raw))
            {
                id = new RowId(EntityId.FromULong(raw));
                return true;
            }

            id = default;
            return false;
        }

        /// <summary>Fills <paramref name="output"/> with the identifiers the global ids resolve to.</summary>
        /// <param name="globalIds">Identifiers to resolve.</param>
        /// <param name="output">Receives one identifier per input, invalid when unresolved.</param>
        internal static void Resolve(GlobalObjectId[] globalIds, RowId[] output)
        {
            var native = new EntityId[globalIds.Length];
            GlobalObjectId.GlobalObjectIdentifiersToEntityIdsSlow(globalIds, native);
            for (int i = 0; i < native.Length; i++)
                output[i] = new RowId(native[i]);
        }

        /// <summary>Returns the identifiers of the objects in the current selection.</summary>
        internal static RowId[] GetSelection()
        {
            var native = Selection.entityIds;
            var result = new RowId[native.Length];
            for (int i = 0; i < native.Length; i++)
                result[i] = new RowId(native[i]);
            return result;
        }

        /// <inheritdoc/>
        public bool Equals(RowId other)
        {
            return m_Value.Equals(other.m_Value);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return m_Value.GetHashCode();
        }
#else
        // Unity releases in the transition to EntityId mark the 32-bit identifier API as deprecated while
        // not yet exposing EntityId itself. In those versions this is the only available API, so the
        // deprecation notice is suppressed at the three call sites rather than project-wide.
        private readonly int m_Value;

        /// <summary>Wraps a native identifier.</summary>
        /// <param name="value">Native identifier value.</param>
        internal RowId(int value)
        {
            m_Value = value;
        }

        /// <summary>The wrapped native identifier.</summary>
        internal int Value => m_Value;

        /// <summary><c>true</c> when the identifier refers to an object.</summary>
        internal bool IsValid => m_Value != 0;

        /// <summary>Returns the identifier of an object.</summary>
        /// <param name="target">Object to identify.</param>
        internal static RowId Of(UnityEngine.Object target)
        {
            return target != null ? new RowId(target.GetInstanceID()) : default;
        }

        /// <summary>Resolves the identifier back to an object, or <c>null</c>.</summary>
        internal UnityEngine.Object ToObject()
        {
#pragma warning disable 618
            return IsValid ? EditorUtility.InstanceIDToObject(m_Value) : null;
#pragma warning restore 618
        }

        /// <summary>Formats the identifier for storage in a session-scoped key.</summary>
        internal string ToPersistString()
        {
            return m_Value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Parses an identifier produced by <see cref="ToPersistString"/>.</summary>
        /// <param name="text">Text to parse.</param>
        /// <param name="id">Receives the parsed identifier.</param>
        internal static bool TryParsePersistString(string text, out RowId id)
        {
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int raw))
            {
                id = new RowId(raw);
                return true;
            }

            id = default;
            return false;
        }

        /// <summary>Fills <paramref name="output"/> with the identifiers the global ids resolve to.</summary>
        /// <param name="globalIds">Identifiers to resolve.</param>
        /// <param name="output">Receives one identifier per input, invalid when unresolved.</param>
        internal static void Resolve(GlobalObjectId[] globalIds, RowId[] output)
        {
            var native = new int[globalIds.Length];
#pragma warning disable 618
            GlobalObjectId.GlobalObjectIdentifiersToInstanceIDsSlow(globalIds, native);
#pragma warning restore 618
            for (int i = 0; i < native.Length; i++)
                output[i] = new RowId(native[i]);
        }

        /// <summary>Returns the identifiers of the objects in the current selection.</summary>
        internal static RowId[] GetSelection()
        {
#pragma warning disable 618
            var native = Selection.instanceIDs;
#pragma warning restore 618
            var result = new RowId[native.Length];
            for (int i = 0; i < native.Length; i++)
                result[i] = new RowId(native[i]);
            return result;
        }

        /// <inheritdoc/>
        public bool Equals(RowId other)
        {
            return m_Value == other.m_Value;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return m_Value;
        }
#endif

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is RowId other && Equals(other);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return ToPersistString();
        }
    }
}
