using System;
using System.IO;
using System.Text.Json;

namespace Launcher.Core.Services
{
    public class LocalState
    {
        public string InstalledVersion { get; set; }
    }

    public interface ILocalStateStore
    {
        void Save(LocalState state);
        LocalState Load();
    }

    public class LocalStateStore : ILocalStateStore
    {
        private readonly string _filePath;

        public LocalStateStore(string directoryPath)
        {
            _filePath = Path.Combine(directoryPath, "local.json");
        }

        public void Save(LocalState state)
        {
            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        public LocalState Load()
        {
            if (!File.Exists(_filePath))
                return new LocalState();

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<LocalState>(json);
        }
    }
}
