using System;
using System.Collections.Generic;
using UnityEngine;

namespace CryNet.HierarchyColorStudio
{
    /// <summary>
    /// Console output for the plugin. Informational messages require the user to opt in through
    /// <see cref="AppearanceSettings.DebugLogging"/>; warnings and exceptions are reported at most
    /// once per key per Editor session so no Editor loop can ever spam the Console.
    /// </summary>
    internal static class StudioLog
    {
        private const string Prefix = "[Hierarchy Color Studio] ";

        private static readonly HashSet<string> s_ReportedKeys = new HashSet<string>();

        /// <summary><c>true</c> when verbose logging is enabled in the settings.</summary>
        internal static bool VerboseEnabled
        {
            get
            {
                var store = HierarchyColorStoreProvider.Store;
                return store != null && store.Appearance.DebugLogging;
            }
        }

        /// <summary>Logs a diagnostic message when verbose logging is enabled.</summary>
        /// <param name="message">Message body.</param>
        internal static void Info(string message)
        {
            if (VerboseEnabled)
                Debug.Log(Prefix + message);
        }

        /// <summary>Logs a warning at most once per key per Editor session.</summary>
        /// <param name="key">Deduplication key.</param>
        /// <param name="message">Message body.</param>
        internal static void WarnOnce(string key, string message)
        {
            if (s_ReportedKeys.Add(key))
                Debug.LogWarning(Prefix + message);
        }

        /// <summary>Logs an exception at most once per key per Editor session.</summary>
        /// <param name="key">Deduplication key.</param>
        /// <param name="exception">Exception to report.</param>
        /// <param name="message">Context describing what failed.</param>
        internal static void ExceptionOnce(string key, Exception exception, string message)
        {
            if (!s_ReportedKeys.Add(key))
                return;

            Debug.LogWarning(Prefix + message + " " + (exception != null ? exception.Message : string.Empty));
            if (VerboseEnabled && exception != null)
                Debug.LogException(exception);
        }

        /// <summary>Allows a deduplicated message to be reported again, used when settings are reset.</summary>
        internal static void ResetDeduplication()
        {
            s_ReportedKeys.Clear();
        }
    }
}
