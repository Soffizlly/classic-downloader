using System;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace ClassicDownloader.Services
{
    public class ThemeManager
    {
        public string CurrentTheme { get; private set; }

        public ThemeManager()
        {
            CurrentTheme = "Navy";
        }

        public void ApplyThemePreference(string preference)
        {
            if (preference == "Auto")
            {
                bool isSystemDark = DetectSystemDarkTheme();
                SwitchTheme(isSystemDark ? "Navy" : "Sky");
            }
            else
            {
                SwitchTheme(preference);
            }
        }

        public bool DetectSystemDarkTheme()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("AppsUseLightTheme");
                        if (val != null)
                        {
                            return (int)val == 0; // 0 = Dark, 1 = Light
                        }
                    }
                }
            }
            catch { }
            return true; // Default to Dark if detection fails
        }

        public void SwitchTheme(string themeName)
        {
            try
            {
                // Normalize theme name
                if (themeName != "Navy" && themeName != "Sky") themeName = "Navy";

                // Use absolute Pack URI
                string uriStr = string.Format("pack://application:,,,/ClassicDownloader;component/Themes/Theme.{0}.xaml", themeName);
                var dictUri = new Uri(uriStr, UriKind.Absolute);
                
                ResourceDictionary newDict = new ResourceDictionary();
                newDict.Source = dictUri;

                if (newDict != null)
                {
                    // Remove old theme
                    var oldDict = Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Source != null && d.Source.ToString().Contains("Theme."));
                    if (oldDict != null)
                    {
                        Application.Current.Resources.MergedDictionaries.Remove(oldDict);
                    }

                    Application.Current.Resources.MergedDictionaries.Add(newDict);
                    CurrentTheme = themeName;
                }
            }
            catch (Exception ex) 
            {
                 MessageBox.Show("Error switching theme: " + ex.Message);
            }
        }
    }
}
