using System;
using UnityEngine;

namespace Chris.PachiRogue.Core
{
    /// <summary>
    /// PlayerPrefs-backed storage, used on WebGL where there is no reliable
    /// System.IO persistence. On WebGL, PlayerPrefs live in the browser's
    /// IndexedDB and writes are asynchronous — <see cref="PlayerPrefs.Save"/>
    /// is called after every write to force the flush, because the browser
    /// gives no reliable application-quit event (verified against Unity 6
    /// WebGL docs, see NOTES.md Phase 0).
    /// </summary>
    public sealed class PlayerPrefsSaveBackend : ISaveBackend
    {
        private readonly string _key;

        public PlayerPrefsSaveBackend(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("PlayerPrefs key must be non-empty.", nameof(key));
            }

            _key = key;
        }

        public bool Exists()
        {
            return PlayerPrefs.HasKey(_key);
        }

        public string Read()
        {
            return PlayerPrefs.HasKey(_key) ? PlayerPrefs.GetString(_key) : null;
        }

        public void Write(string payload)
        {
            PlayerPrefs.SetString(_key, payload);
            PlayerPrefs.Save();
        }

        public void Delete()
        {
            PlayerPrefs.DeleteKey(_key);
            PlayerPrefs.Save();
        }
    }
}
