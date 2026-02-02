using System;
using System.IO;

// Going with simple manual JSON or text for minimal dependencies if possible, 
// BUT users often have Newtonsoft or System.Text.Json. 
// Given the environment, I'll use a simple data contract with a custom minimal parser/serializer or standard XML Serialization to avoid external DLL dependency hell in portable simple apps.
// Actually, System.Web.Extensions is standard in .NET Framework 4+. Let's check refs.
// The csproj has System.Core. 
// I'll stick to XML Serialization for out-of-the-box support without external DLLs if System.Web.Extensions isn't added. 
// Wait, user wants "config.json". I should use System.Web.Extensions (JavaScriptSerializer) if I can, or just basic string parsing for simple config.
// Let's add reference to System.Web.Extensions in .csproj or use XmlSerializer and rename to config.xml? User specifically said nothing about format, just "configuraciones".
// I'll use JSON with a simple helper to avoid adding references if possible, OR just add the reference.
// Adding reference is safer for robust JSON.
// Let's try to add reference to System.Web.Extensions in the csproj first if I go that route.
// ACTUALLY, I will use a simple XML format for now to ensure 100% compatibility without modifying csproj refs for now, OR I will modify csproj.
// I'll modify csproj to add System.Web.Extensions, it's standard.

// Wait, I can't easily run "add reference" commands. I have to edit csproj.
// Let's stick to XML or a very simple custom JSONizer.
// "config.json" is expected by modern standards.

using System.Collections.Generic;

namespace ClassicDownloader.Services
{
    public class AppSettings
    {
        public string ThemePreference { get; set; }
        public bool CheckUpdatesOnStartup { get; set; }
        public string LastDownloadPath { get; set; }

        public AppSettings()
        {
            ThemePreference = "Auto";
            CheckUpdatesOnStartup = true;
            LastDownloadPath = "";
        }
    }

    public class SettingsService
    {
        private readonly string _configPath;
        public AppSettings CurrentSettings { get; private set; }

        public SettingsService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appData, "ClassicDownloader");
            if (!Directory.Exists(appFolder)) Directory.CreateDirectory(appFolder);
            
            _configPath = Path.Combine(appFolder, "config.json");
            LoadSettings();
        }

        public void LoadSettings()
        {
            if (File.Exists(_configPath))
            {
                try
                {
                    string json = File.ReadAllText(_configPath);
                    // Minimal JSON parser for this specific structure to avoid dependencies
                    CurrentSettings = SimpleJsonParse(json);
                }
                catch
                {
                    CurrentSettings = new AppSettings();
                }
            }
            else
            {
                CurrentSettings = new AppSettings();
                SaveSettings();
            }
        }

        public void SaveSettings()
        {
            try
            {
                string json = SimpleJsonSerialize(CurrentSettings);
                File.WriteAllText(_configPath, json);
            }
            catch { /* Ignore save errors */ }
        }

        // Extremely simple parser/serializer to avoid external deps for now
        private AppSettings SimpleJsonParse(string json)
        {
            var settings = new AppSettings();
            try 
            {
                // Remove braces and newlines
                json = json.Replace("{", "").Replace("}", "").Replace("\r", "").Replace("\n", "").Trim();
                var pairs = json.Split(',');
                
                foreach (var pair in pairs)
                {
                    var parts = pair.Split(':');
                    if (parts.Length < 2) continue;
                    
                    var key = parts[0].Trim().Trim('"');
                    var val = parts[1].Trim().Trim('"');

                    if (key == "ThemePreference") settings.ThemePreference = val;
                    if (key == "CheckUpdatesOnStartup") settings.CheckUpdatesOnStartup = val.ToLower() == "true";
                    if (key == "LastDownloadPath") settings.LastDownloadPath = val;
                }
            }
            catch {}
            return settings;
        }

        private string SimpleJsonSerialize(AppSettings settings)
        {
            return string.Format("{{\n  \"ThemePreference\": \"{0}\",\n  \"CheckUpdatesOnStartup\": {1},\n  \"LastDownloadPath\": \"{2}\"\n}}",
                settings.ThemePreference,
                settings.CheckUpdatesOnStartup.ToString().ToLower(),
                settings.LastDownloadPath.Replace("\\", "\\\\") // Basic escape
            );
        }
    }
}
