using System;
using System.IO;
using System.Text.Json;

namespace xcrosshair
{
    public class CrosshairSettings
    {
        public int MonitorIndex { get; set; } = 0;
        public string MenuPosition { get; set; } = "TopRight";
        public string Color { get; set; } = "Lime";
        public double Size { get; set; } = 20;
        public double Thickness { get; set; } = 2;

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "xcrosshair",
            "settings.json"
        );

        public static CrosshairSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<CrosshairSettings>(json) ?? new CrosshairSettings();
                }
            }
            catch { }
            return new CrosshairSettings();
        }

        public void Save()
        {
            try
            {
                string? directory = Path.GetDirectoryName(SettingsPath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }
    }
}
