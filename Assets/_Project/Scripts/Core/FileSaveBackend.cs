using System;
using System.IO;

namespace Chris.PachiRogue.Core
{
    /// <summary>
    /// File-backed storage for Android/desktop/editor. The directory is
    /// injected (Application.persistentDataPath at runtime, a temp directory
    /// in tests) so this class stays free of UnityEngine calls. Writes go to a
    /// temp file first and are then moved into place, so a crash mid-write
    /// cannot corrupt an existing save.
    /// </summary>
    public sealed class FileSaveBackend : ISaveBackend
    {
        private readonly string _path;
        private readonly string _tempPath;

        public FileSaveBackend(string directory, string fileName = "save_v1.json")
        {
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentException("Save directory must be non-empty.", nameof(directory));
            }

            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException("Save file name must be non-empty.", nameof(fileName));
            }

            _path = Path.Combine(directory, fileName);
            _tempPath = _path + ".tmp";
        }

        public bool Exists()
        {
            return File.Exists(_path);
        }

        public string Read()
        {
            return File.Exists(_path) ? File.ReadAllText(_path) : null;
        }

        public void Write(string payload)
        {
            string directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_tempPath, payload);
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }

            File.Move(_tempPath, _path);
        }

        public void Delete()
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }

            if (File.Exists(_tempPath))
            {
                File.Delete(_tempPath);
            }
        }
    }
}
