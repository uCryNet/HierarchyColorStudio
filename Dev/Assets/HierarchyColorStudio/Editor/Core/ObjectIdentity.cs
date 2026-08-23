using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Produces the stable identity keys used to remember which GameObject owns which color.
    /// </summary>
    /// <remarks>
    /// The primary key is <see cref="GlobalObjectId"/>, which combines the owning asset GUID
    /// (scene or prefab) with the object's local file identifier. Instance ids are never persisted,
    /// with one deliberate exception: an object that has no local file identifier yet — because its
    /// scene has never been saved, or because the object was created after the last save — receives a
    /// session-scoped key. Those keys are promoted to real <see cref="GlobalObjectId"/> keys as soon as
    /// the scene is saved, and are discarded when the Editor is restarted.
    /// </remarks>
    internal static class ObjectIdentity
    {
        private const string EmptyGuid = "00000000000000000000000000000000";

        private static readonly List<UnityEngine.Object> s_ObjectBuffer = new List<UnityEngine.Object>(64);

        /// <summary>
        /// Builds identity keys for a set of GameObjects, verifying that each key resolves back to the
        /// object it was created from. Objects that fail verification receive a session-scoped key.
        /// </summary>
        /// <param name="targets">GameObjects to identify. Null entries are skipped.</param>
        /// <param name="keys">Receives one key per accepted GameObject, in the same order.</param>
        /// <param name="accepted">Receives the accepted GameObjects.</param>
        /// <returns><c>true</c> when at least one key was produced.</returns>
        internal static bool TryBuildKeys(IReadOnlyList<GameObject> targets, List<string> keys, List<GameObject> accepted)
        {
            keys.Clear();
            accepted.Clear();
            if (targets == null || targets.Count == 0)
                return false;

            s_ObjectBuffer.Clear();
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null)
                {
                    s_ObjectBuffer.Add(targets[i]);
                    accepted.Add(targets[i]);
                }
            }

            int count = s_ObjectBuffer.Count;
            if (count == 0)
                return false;

            var ids = new GlobalObjectId[count];
            try
            {
                GlobalObjectId.GetGlobalObjectIdsSlow(s_ObjectBuffer.ToArray(), ids);
            }
            catch (Exception exception)
            {
                StudioLog.ExceptionOnce("identity-build", exception, "Could not compute object identities.");
                for (int i = 0; i < count; i++)
                    keys.Add(ColorAssignment.MakeSessionKey(RowId.Of(accepted[i])));
                return true;
            }

            var roundTrip = new RowId[count];
            try
            {
                RowId.Resolve(ids, roundTrip);
            }
            catch (Exception exception)
            {
                StudioLog.ExceptionOnce("identity-verify", exception, "Could not verify object identities.");
                for (int i = 0; i < count; i++)
                    roundTrip[i] = default;
            }

            for (int i = 0; i < count; i++)
            {
                var rowId = RowId.Of(accepted[i]);
                bool stable = ids[i].assetGUID.ToString() != EmptyGuid && roundTrip[i].Equals(rowId);
                keys.Add(stable ? ids[i].ToString() : ColorAssignment.MakeSessionKey(rowId));
            }

            s_ObjectBuffer.Clear();
            return true;
        }

        /// <summary>Builds the identity key for a single GameObject.</summary>
        /// <param name="target">GameObject to identify.</param>
        /// <param name="key">Receives the identity key.</param>
        /// <returns><c>true</c> when a key could be produced.</returns>
        internal static bool TryBuildKey(GameObject target, out string key)
        {
            key = null;
            if (target == null)
                return false;

            var single = new[] { target };
            var keys = new List<string>(1);
            var accepted = new List<GameObject>(1);
            if (!TryBuildKeys(single, keys, accepted) || keys.Count == 0)
                return false;

            key = keys[0];
            return true;
        }

        /// <summary>Parses a persisted <see cref="GlobalObjectId"/> key.</summary>
        /// <param name="key">Key to parse.</param>
        /// <param name="id">Receives the parsed identifier.</param>
        /// <returns><c>true</c> when the key is a valid identifier string.</returns>
        internal static bool TryParseGlobalId(string key, out GlobalObjectId id)
        {
            id = default;
            if (string.IsNullOrEmpty(key))
                return false;

            try
            {
                return GlobalObjectId.TryParse(key, out id);
            }
            catch (Exception exception)
            {
                StudioLog.ExceptionOnce("identity-parse", exception, "Could not parse a stored object identity.");
                return false;
            }
        }

        /// <summary>Returns the asset GUID a parsed identifier belongs to.</summary>
        /// <param name="id">Parsed identifier.</param>
        internal static string GetAssetGuid(GlobalObjectId id)
        {
            return id.assetGUID.ToString();
        }

        /// <summary><c>true</c> when the GUID refers to no asset, which means "unsaved scene".</summary>
        /// <param name="guid">GUID string to test.</param>
        internal static bool IsEmptyGuid(string guid)
        {
            return string.IsNullOrEmpty(guid) || guid == EmptyGuid;
        }
    }
}
