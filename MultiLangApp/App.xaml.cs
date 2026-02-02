using System;
using System.Windows;

namespace ClassicDownloader
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Global exception handler
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                MessageBox.Show(string.Format("Fatal Error:\n{0}\n\nStack:\n{1}", ex != null ? ex.Message : "Unknown", ex != null ? ex.StackTrace : "N/A"), 
                    "ClassicDownloader - Startup Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            };
            
            DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show(string.Format("Error:\n{0}\n\nStack:\n{1}", args.Exception.Message, args.Exception.StackTrace), 
                    "ClassicDownloader - Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                args.Handled = true;
            };
        }
    }
}
