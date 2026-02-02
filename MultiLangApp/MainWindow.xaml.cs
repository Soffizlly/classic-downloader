using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ClassicDownloader.Localization;
using ClassicDownloader.Services;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ClassicDownloader
{
    public partial class MainWindow : Window
    {
        private LocalizationManager _localizationManager;
        private ThemeManager _themeManager;
        private SettingsService _settingsService;
        private UpdateService _updateService;
        private string _selectedFile;
        private CancellationTokenSource _cts;
        private System.Text.StringBuilder _logHistory;
        private bool _isInitialized = false;

        public MainWindow()
        {
            InitializeComponent();
            _localizationManager = new LocalizationManager();
            _themeManager = new ThemeManager();
            _settingsService = new SettingsService();
            _updateService = new UpdateService();
            _logHistory = new System.Text.StringBuilder();
            
            // Initialization Logic
            InitializeSettings();
            
            this.Loaded += MainWindow_Loaded;
        }

        private void InitializeSettings()
        {
             // Set default paths
            string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (!string.IsNullOrEmpty(_settingsService.CurrentSettings.LastDownloadPath) && Directory.Exists(_settingsService.CurrentSettings.LastDownloadPath))
                defaultPath = _settingsService.CurrentSettings.LastDownloadPath;
            
            TxtDownloadPath.Text = defaultPath;
            TxtConvertPath.Text = defaultPath;

            // Apply Theme
            string themePref = _settingsService.CurrentSettings.ThemePreference;
            _themeManager.ApplyThemePreference(themePref);
            
            // Set UI State for Settings
            _isInitialized = false; // Prevent event firing during init
            foreach (ComboBoxItem item in CboTheme.Items)
            {
                if (item.Tag.ToString() == themePref)
                {
                    CboTheme.SelectedItem = item;
                    break;
                }
            }
            ChkUpdatesStartup.IsChecked = _settingsService.CurrentSettings.CheckUpdatesOnStartup;
            TxtVersion.Text = "v" + AppInfo.Version;
            _isInitialized = true;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Log(GetStr("LogAppStarted"));
            
            if (_settingsService.CurrentSettings.CheckUpdatesOnStartup)
            {
                await CheckUpdatesSilent();
            }
        }
        
        private async Task CheckUpdatesSilent()
        {
            Log(GetStr("LogCheckingUpdates"));
            bool updateAvailable = await _updateService.CheckForUpdatesAsync();
            if (updateAvailable)
            {
                TxtUpdateStatus.Text = GetStr("LogUpdateAvailable");
                Log(GetStr("LogUpdateAvailable"));
                // Optional: Show notification dot or prompt
            }
            else
            {
                TxtUpdateStatus.Text = GetStr("LogUpToDate") + " (v" + AppInfo.Version + ")";
                Log(GetStr("LogUpToDate") + ".");
            }
        }

        private string GetStr(string key)
        {
            return (string)Application.Current.Resources[key];
        }

        private void Log(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string entry = string.Format("[{0}] {1}", timestamp, message);
            _logHistory.AppendLine(entry);
            Dispatcher.Invoke(new Action(() => { TxtLogs.Text = message; TxtLogs.ToolTip = message; }));
        }

        // --- Settings UI Logic ---
        
        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
             ViewSettings.Visibility = Visibility.Visible;
        }

        private void BtnCloseSettings_Click(object sender, RoutedEventArgs e)
        {
            ViewSettings.Visibility = Visibility.Collapsed;
        }

        private void CboTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            ComboBoxItem item = CboTheme.SelectedItem as ComboBoxItem;
            if (item != null)
            {
                string tag = item.Tag.ToString();
                _settingsService.CurrentSettings.ThemePreference = tag;
                _settingsService.SaveSettings();
                _themeManager.ApplyThemePreference(tag);
            }
        }

        private void ChkUpdatesStartup_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            _settingsService.CurrentSettings.CheckUpdatesOnStartup = ChkUpdatesStartup.IsChecked == true;
            _settingsService.SaveSettings();
        }

        private async void BtnCheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            BtnCheckUpdates.IsEnabled = false;
            TxtUpdateStatus.Text = GetStr("StatusChecking");
            await CheckUpdatesSilent();
            BtnCheckUpdates.IsEnabled = true;
        }

        private void BtnFeedback_Click(object sender, RoutedEventArgs e)
        {
            try { Process.Start(AppInfo.GitHubUrl + "/issues/new"); } catch { }
        }

        // --- Other Events Preserved logic below ---
        
        private void BtnEnglish_Click(object sender, RoutedEventArgs e) { _localizationManager.SwitchLanguage("en"); }
        private void BtnSpanish_Click(object sender, RoutedEventArgs e) { _localizationManager.SwitchLanguage("es"); }
        private void BtnShowLogs_Click(object sender, RoutedEventArgs e) 
        {
             var win = new Window { Title = "Application Logs", Width = 600, Height = 400, WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = this };
             var txt = new TextBox { Text = _logHistory.ToString(), IsReadOnly = true, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, FontFamily = new System.Windows.Media.FontFamily("Consolas"), Padding = new Thickness(10) };
             win.Content = txt; win.ShowDialog();
        }

        private void BtnLucida_Click(object sender, RoutedEventArgs e) { Process.Start("https://lucida.to"); }
        
        // --- Browse Logic ---
        private void BtnBrowseDownload_Click(object sender, RoutedEventArgs e) { using (var dialog = new System.Windows.Forms.FolderBrowserDialog()) { if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) { TxtDownloadPath.Text = dialog.SelectedPath; _settingsService.CurrentSettings.LastDownloadPath = dialog.SelectedPath; _settingsService.SaveSettings(); } } }
        private void BtnBrowseConvert_Click(object sender, RoutedEventArgs e) { using (var dialog = new System.Windows.Forms.FolderBrowserDialog()) { if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) { TxtConvertPath.Text = dialog.SelectedPath; } } }

        // --- CONVERTER WIZARD LOGIC ---
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Media Files|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.flv;*.webm;*.mp3;*.wav;*.flac;*.m4a;*.ogg;*.wma;*.aac|Video Files|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.flv;*.webm|Audio Files|*.mp3;*.wav;*.flac;*.m4a;*.ogg;*.wma;*.aac|All Files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true) OnInputFileSelected(openFileDialog.FileName);
        }

        private void Border_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) { string[] files = (string[])e.Data.GetData(DataFormats.FileDrop); if (files.Length > 0) OnInputFileSelected(files[0]); }
        }

        private void OnInputFileSelected(string filePath)
        {
            _selectedFile = filePath;
            TxtInputFile.Text = System.IO.Path.GetFileName(_selectedFile);
            ViewConverterInput.Visibility = Visibility.Collapsed;
            ViewConverterOptions.Visibility = Visibility.Visible;
        }
        
        private void BtnBackConverter_Click(object sender, RoutedEventArgs e)
        {
            ViewConverterOptions.Visibility = Visibility.Collapsed;
            ViewConverterInput.Visibility = Visibility.Visible;
            _selectedFile = null;
            TxtStatusConvert.Text = "";
            PbStatusConvert.Value = 0;
        }

        private async void BtnConvert_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFile)) return;
            if (!DependencyManager.IsFfmpegInstalled()) { MessageBox.Show((string)Application.Current.Resources["MsgFfmpegNotFound"]); return; }

            Log(GetStr("MsgStarting"));
            PbStatusConvert.IsIndeterminate = true;
            BtnConvertAction.IsEnabled = false;
            _cts = new CancellationTokenSource();

            var progressPercent = new Progress<double>(p => { PbStatusConvert.IsIndeterminate = false; PbStatusConvert.Value = p; });
            var progressLog = new Progress<string>(log => { Log(log); });

            try
            {
                string format = "mp4";
                ComboBoxItem item = CboConvertFormat.SelectedItem as ComboBoxItem;
                if (item != null && item.Tag != null)
                {
                    format = item.Tag.ToString();
                }

                string outputFolder = TxtConvertPath.Text;
                var ffmpeg = new FfmpegService();
                await ffmpeg.ConvertAsync(_selectedFile, format, outputFolder, progressLog, progressPercent, _cts.Token, null, null);

                Log((string)Application.Current.Resources["MsgSuccess"]);
                PbStatusConvert.Value = 100;
                MessageBox.Show((string)Application.Current.Resources["MsgSuccess"]);
            }
            catch (Exception ex) { Log(GetStr("MsgErrorPrefix") + " " + ex.Message); MessageBox.Show(ex.Message); }
            finally { BtnConvertAction.IsEnabled = true; PbStatusConvert.IsIndeterminate = false; _cts = null; }
        }

        // --- DOWNLOADER WIZARD LOGIC ---

        private async void BtnAnalyze_Click(object sender, RoutedEventArgs e)
        {
            string url = TxtUrl.Text;
            if (string.IsNullOrWhiteSpace(url)) { MessageBox.Show((string)Application.Current.Resources["LabelUrl"]); return; }
            if (!DependencyManager.IsYtDlpInstalled()) { MessageBox.Show((string)Application.Current.Resources["MsgYtDlpNotFound"]); return; }

            ViewDownloaderInput.Visibility = Visibility.Collapsed;
            ViewDownloaderLoading.Visibility = Visibility.Visible;
            Log(GetStr("LogAnalyzing"));

            var ytdlp = new YtDlpService();
            try 
            {
                 var meta = await ytdlp.GetRemoteMetadataAsync(url);
                 if (meta != null)
                 {
                     LblTitle.Text = meta.Title;
                     LblDuration.Text = GetStr("LogDuration") + meta.Duration;
                     LblUploader.Text = meta.Uploader;
                     Log(GetStr("LogMetaFetched"));
                     try { if (!string.IsNullOrEmpty(meta.ThumbnailUrl)) ImgThumb.Source = new BitmapImage(new Uri(meta.ThumbnailUrl)); } catch { }

                     ViewDownloaderLoading.Visibility = Visibility.Collapsed;
                     ViewDownloaderOptions.Visibility = Visibility.Visible;
                 }
                 else throw new Exception(GetStr("LogMetaError"));
            }
            catch (Exception ex)
            {
                Log(GetStr("LogAnalysisFailed") + ex.Message); MessageBox.Show(GetStr("MsgAnalysisFailed") + ex.Message);
                ViewDownloaderLoading.Visibility = Visibility.Collapsed; ViewDownloaderInput.Visibility = Visibility.Visible;
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            ViewDownloaderOptions.Visibility = Visibility.Collapsed;
            ViewDownloaderInput.Visibility = Visibility.Visible;
            TxtUrl.Clear();
            ImgThumb.Source = null;
            Log(GetStr("StatusReady"));
        }

        private async void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            string url = TxtUrl.Text;
            Log(GetStr("MsgStarting"));
            BtnDownloadAction.IsEnabled = false;
            _cts = new CancellationTokenSource();

            var progressLog = new Progress<string>(log => { Log(log); });
            var progressPercent = new Progress<double>(p => { });

            try
            {
                var selectedItem = CboDownloadFormat.SelectedItem as ComboBoxItem;
                if (selectedItem == null || selectedItem.Tag == null) return;
                string formatSelection = selectedItem.Tag.ToString();
                
                bool addMetadata = ChkAddMetadata.IsChecked == true;
                string outputFolder = TxtDownloadPath.Text;

                var ytdlp = new YtDlpService();
                await ytdlp.DownloadAsync(url, formatSelection, addMetadata, outputFolder, progressLog, progressPercent, _cts.Token);

                Log((string)Application.Current.Resources["MsgSuccess"]);
                MessageBox.Show((string)Application.Current.Resources["MsgSuccess"]);
            }
            catch (Exception ex) { Log(GetStr("MsgErrorPrefix") + " " + ex.Message); MessageBox.Show(ex.Message); }
            finally { BtnDownloadAction.IsEnabled = true; _cts = null; }
        }

        // --- METADATA Logic Stubbed ---
        private void BtnBrowseMetadata_Click(object sender, RoutedEventArgs e) { }
        private void BtnBackMetadata_Click(object sender, RoutedEventArgs e) { ViewMetadataEditor.Visibility = Visibility.Collapsed; ViewMetadataInput.Visibility = Visibility.Visible; }
        private void Border_MetadataDrop(object sender, DragEventArgs e) { }
        private void Border_MetadataMouseLeftButtonDown(object sender, MouseButtonEventArgs e) { }
    }
}
