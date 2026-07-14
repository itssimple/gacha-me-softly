namespace Chris.PachiRogue.Core
{
    /// <summary>
    /// Raw string storage seam beneath <see cref="ISaveService"/>. Keeps
    /// platform differences (PlayerPrefs vs file I/O) out of the save logic
    /// and lets tests substitute an in-memory backend.
    /// </summary>
    public interface ISaveBackend
    {
        bool Exists();

        /// <summary>Returns the stored payload, or null if nothing is stored.</summary>
        string Read();

        void Write(string payload);

        void Delete();
    }
}
