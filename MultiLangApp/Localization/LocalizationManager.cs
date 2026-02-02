using System;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace ClassicDownloader.Localization
{
    public class LocalizationManager
    {
        public CultureInfo CurrentCulture { get; private set; }

        public LocalizationManager()
        {
            // Idioma por defecto
            SwitchLanguage("en");
        }

        public void SwitchLanguage(string cultureCode)
        {
            try
            {
                CurrentCulture = new CultureInfo(cultureCode);
                // Use absolute Pack URI
                string uriStr = string.Format("pack://application:,,,/ClassicDownloader;component/Localization/StringResources.{0}.xaml", cultureCode);
                var dictUri = new Uri(uriStr, UriKind.Absolute);
                
                ResourceDictionary newDict = new ResourceDictionary();
                newDict.Source = dictUri;

                if (newDict != null)
                {
                     var oldDict = Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Contains("HeaderTitle") || d.Contains("LabelUrl") || d.Source != null && d.Source.ToString().Contains("StringResources"));
                     if (oldDict != null) Application.Current.Resources.MergedDictionaries.Remove(oldDict);
                     Application.Current.Resources.MergedDictionaries.Add(newDict);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading language (" + cultureCode + "): " + ex.Message);
            }
        }
    }
}
