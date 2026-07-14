using System;
using UnityEngine;

namespace Chris.PachiRogue.Core
{
    /// <summary>
    /// JSON implementation of <see cref="ISaveService"/> on top of an
    /// injected <see cref="ISaveBackend"/>. Serialization uses
    /// <see cref="JsonUtility"/> against the versioned DTO layer.
    /// </summary>
    public sealed class JsonSaveService : ISaveService
    {
        private readonly ISaveBackend _backend;

        public JsonSaveService(ISaveBackend backend)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        }

        public bool HasSave => _backend.Exists();

        public void Save(SaveModelV1 model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            model.version = SaveModelV1.CurrentVersion;
            string json = JsonUtility.ToJson(model);
            _backend.Write(json);
        }

        public bool TryLoad(out SaveModelV1 model)
        {
            model = null;

            string json = _backend.Read();
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            SaveModelV1 parsed;
            try
            {
                parsed = JsonUtility.FromJson<SaveModelV1>(json);
            }
            catch (ArgumentException)
            {
                // Malformed JSON — treat as no save rather than crashing boot.
                return false;
            }

            // Future versions add migration steps here (V1 → V2 → …).
            if (parsed == null || parsed.version != SaveModelV1.CurrentVersion)
            {
                return false;
            }

            model = parsed;
            return true;
        }

        public void DeleteSave()
        {
            _backend.Delete();
        }
    }
}
