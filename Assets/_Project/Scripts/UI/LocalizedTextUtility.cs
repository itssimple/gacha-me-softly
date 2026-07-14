using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace Chris.PachiRogue.UI
{
    /// <summary>
    /// Coroutine helpers for fetching localized strings. Always yields on the
    /// async handle — WaitForCompletion is unsupported on WebGL (Phase 0 note
    /// in NOTES.md).
    /// </summary>
    internal static class LocalizedTextUtility
    {
        public static IEnumerator Get(string table, string key, object[] arguments, Action<string> onDone)
        {
            AsyncOperationHandle<string> handle =
                LocalizationSettings.StringDatabase.GetLocalizedStringAsync(table, key, arguments);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded && !string.IsNullOrEmpty(handle.Result))
            {
                onDone(handle.Result);
            }
            else
            {
                Debug.LogError(
                    $"Missing localized string '{key}' in table '{table}'. " +
                    "Run 'PachiRogue > Localization > Create Story Tables' in the editor.");
            }
        }

        public static IEnumerator SetText(Text target, string table, string key, object[] arguments = null)
        {
            return Get(table, key, arguments, value => target.text = value);
        }
    }
}
