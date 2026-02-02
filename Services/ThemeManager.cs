using System;
using System.Linq;
using System.Windows;

namespace MultiLangApp.Services
{
    public class ThemeManager
    {
        public string CurrentTheme { get; private set; }

        public ThemeManager()
        {
            CurrentTheme = "Navy";
        }

        public void SwitchTheme(string themeName)
        {
            try
            {
                // Use absolute Pack URI
                string uriStr = string.Format("pack://application:,,,/MultiLangApp;component/Themes/Theme.{0}.xaml", themeName);
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
