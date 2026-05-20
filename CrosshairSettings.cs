using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace xcrosshair
{
    public class CrosshairProfile
    {
        public string Name { get; set; } = "Default";
        public string Color { get; set; } = "Lime";
        public double Size { get; set; } = 20;
        public double Thickness { get; set; } = 2;
        public string ValorantProfileCode { get; set; } = "";

        public CrosshairProfile Clone(string newName)
        {
            return new CrosshairProfile
            {
                Name = newName,
                Color = Color,
                Size = Size,
                Thickness = Thickness,
                ValorantProfileCode = ValorantProfileCode
            };
        }
    }

    public class CrosshairSettings
    {
        public int MonitorIndex { get; set; } = 0;
        public string MenuPosition { get; set; } = "TopRight";
        
        public List<CrosshairProfile> Profiles { get; set; } = new List<CrosshairProfile> { new CrosshairProfile() };
        public string CurrentProfileName { get; set; } = "Default";

        [System.Text.Json.Serialization.JsonIgnore]
        public CrosshairProfile CurrentProfile
        {
            get 
            {
                var profile = Profiles.FirstOrDefault(p => p.Name == CurrentProfileName);
                if (profile == null)
                {
                    profile = Profiles.FirstOrDefault() ?? new CrosshairProfile();
                    CurrentProfileName = profile.Name;
                }
                return profile;
            }
        }

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
                    var settings = JsonSerializer.Deserialize<CrosshairSettings>(json);
                    if (settings != null)
                    {
                        if (settings.Profiles == null || settings.Profiles.Count == 0)
                        {
                            settings.Profiles = new List<CrosshairProfile> { new CrosshairProfile() };
                        }
                        return settings;
                    }
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
