using System.IO;
using System.Text.Json;

namespace RecordatorioTareas.Data
{
    public static class SettingsRepository
    {
        public static AppSettings Load(string path)
        {
            if (!File.Exists(path))
                return new AppSettings();

            try
            {
                var json = File.ReadAllText(path);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                return loaded ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public static void Save(string path, AppSettings settings)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(path, json);
        }
    }
}
