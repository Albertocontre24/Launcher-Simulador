using System;
using System.IO;
using System.Text.Json;

namespace Launcher.Core.Services
{
    public class LocalConfig
    {
        public string VersionInstalada { get; set; } = "1.0.0";
        public DateTime UltimaActualizacion { get; set; } = DateTime.Now;
        public bool MostrarLog { get; set; } = false;
        public string RutaInstalacion { get; set; } = string.Empty;
    }

    public class LocalConfigService
    {
        private readonly string _configPath;
        private readonly JsonSerializerOptions _options;

        // Se inicializa con null! para evitar la advertencia CS8618
        public LocalConfig Config { get; private set; } = null!;

        public LocalConfigService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string folderPath = Path.Combine(appData, "Empresa", "Launcher");
            Directory.CreateDirectory(folderPath);

            _configPath = Path.Combine(folderPath, "local.json");

            _options = new JsonSerializerOptions
            {
                WriteIndented = true,
                AllowTrailingCommas = true
            };

           Load();
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    Console.WriteLine("[Config] local.json no existe. Creando valores por defecto...");
                    Config = new LocalConfig();
                    Save();
                    return;
                }

                string json = File.ReadAllText(_configPath);
                Config = JsonSerializer.Deserialize<LocalConfig>(json, _options)
                         ?? new LocalConfig();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Config] Error al leer local.json: {ex.Message}");
                Console.WriteLine("[Config] El archivo estaba dañado y se restauró con valores por defecto.");

                Config = new LocalConfig();
                Save();
            }
        }

        public void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(Config, _options);
                File.WriteAllText(_configPath, json);
                Console.WriteLine($"[Config] Guardado en {_configPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Config] Error al guardar local.json: {ex.Message}");
            }
        }

        public string GetConfigPath() => _configPath;
    }
}
