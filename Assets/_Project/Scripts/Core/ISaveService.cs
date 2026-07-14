namespace Chris.PachiRogue.Core
{
    /// <summary>
    /// Persistence facade. Implementations serialize <see cref="SaveModelV1"/>
    /// to JSON and hand it to a platform backend (<see cref="ISaveBackend"/>):
    /// PlayerPrefs on WebGL, a file under Application.persistentDataPath on
    /// Android/desktop.
    /// </summary>
    public interface ISaveService
    {
        /// <summary>True if a save exists in the backend.</summary>
        bool HasSave { get; }

        /// <summary>Serializes and persists the model.</summary>
        void Save(SaveModelV1 model);

        /// <summary>
        /// Loads and deserializes the save. Returns false (model = null) if no
        /// save exists, the JSON is unreadable, or the version is unsupported.
        /// </summary>
        bool TryLoad(out SaveModelV1 model);

        /// <summary>Removes the persisted save, if any.</summary>
        void DeleteSave();
    }
}
