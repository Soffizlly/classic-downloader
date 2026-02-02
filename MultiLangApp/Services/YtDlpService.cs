using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace ClassicDownloader.Services
{
    public class YtDlpService
    {
        public async Task DownloadAsync(string url, string formatLabel, bool addMetadata, string outputFolder, IProgress<string> logProgress, IProgress<double> percentProgress, CancellationToken token)
        {
            if (!DependencyManager.IsYtDlpInstalled())
                throw new System.IO.FileNotFoundException("yt-dlp.exe not found.");

            string args = "";
            // Fix: Point to the directory containing ffmpeg.exe (BaseDirectory), not ToolsPath
            string ffmpegLoc = System.IO.Path.GetDirectoryName(DependencyManager.GetFfmpegPath());
            string ffmpegArg = string.Format("--ffmpeg-location \"{0}\"", ffmpegLoc);
            string baseArgs = string.Format("{0} \"{1}\"", ffmpegArg, url);

            // 403 Forbidden Workaround: REMOVED (yt-dlp updated to 2026.01.31)
            // baseArgs += " --extractor-args \"youtube:player_client=ios\" ..."; 
            
            // Still ignore cert checks to be safe? Maybe not needed, but harmless.
            baseArgs += " --no-check-certificates";

            // Metadata logic: Avoid for WAV to prevent errors/warnings, enable for others
            if (addMetadata && !formatLabel.Contains("WAV"))
            {
                baseArgs += " --add-metadata --embed-thumbnail";
            }

            if (formatLabel.StartsWith("VIDEO"))
            {
                if (formatLabel.Contains("MP4"))
                    args = string.Format("{0} -f \"bestvideo[ext=mp4]+bestaudio/bestvideo+bestaudio/best\" --recode-video mp4 --postprocessor-args \"VideoConvertor:-c:a aac -b:a 320k\"", baseArgs);              
                else if (formatLabel.Contains("MKV"))
                    args = string.Format("{0} -f \"bestvideo+bestaudio/best\" --merge-output-format mkv", baseArgs);
                else if (formatLabel.Contains("WEBM"))
                    args = string.Format("{0} -f \"bestvideo+bestaudio/best\" --merge-output-format webm", baseArgs);
                else 
                    args = string.Format("{0} -f \"bestvideo+bestaudio/best\"", baseArgs); 
            }
            else if (formatLabel.StartsWith("AUDIO"))
            {
                if (formatLabel.Contains("MP3"))
                    args = string.Format("{0} -x --audio-format mp3 --audio-quality 0", baseArgs);
                else if (formatLabel.Contains("FLAC"))
                    args = string.Format("{0} -x --audio-format flac", baseArgs);
                else if (formatLabel.Contains("WAV"))
                    args = string.Format("{0} -x --audio-format wav", baseArgs);
                else if (formatLabel.Contains("M4A"))
                    args = string.Format("{0} -x --audio-format m4a", baseArgs);
                else if (formatLabel.Contains("WEBM"))
                     args = string.Format("{0} -f \"bestaudio[ext=webm]/bestaudio\"", baseArgs);
            }

            // Output template
            args += string.Format(" -o \"{0}\\%(title)s.%(ext)s\"", outputFolder);

            // Unbuffer output
            args = "--no-mtime --newline " + args;

            await RunProcessAsync(DependencyManager.GetYtDlpPath(), args, logProgress, percentProgress, token);
        }

        private Task RunProcessAsync(string fileName, string arguments, IProgress<string> log, IProgress<double> percent, CancellationToken token)
        {
            var tcs = new TaskCompletionSource<bool>();
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            // Regex para capturar porcentaje: [download]  25.0% of 10.00MiB...
            var regexProgress = new Regex(@"\[download\]\s+(\d+\.?\d*)%", RegexOptions.Compiled);

            Action<string> outputHandler = (line) =>
            {
                if (string.IsNullOrEmpty(line)) return;
                
                // Log completo
                if (log != null) log.Report(line);

                // Parsear porcentaje
                var match = regexProgress.Match(line);
                if (match.Success)
                {
                    double val;
                    if (double.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out val))
                    {
                        if (percent != null) percent.Report(val);
                    }
                }
            };

            process.OutputDataReceived += (s, e) => outputHandler(e.Data);
            process.ErrorDataReceived += (s, e) => outputHandler(e.Data);

            process.EnableRaisingEvents = true;
            process.Exited += (s, e) =>
            {
                if (process.ExitCode == 0) tcs.TrySetResult(true);
                else tcs.TrySetException(new Exception("Process exited with code " + process.ExitCode));
                process.Dispose();
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Manejo de Cancelación
            token.Register(() =>
            {
                if (!process.HasExited)
                {
                    try 
                    {
                        if (log != null) log.Report("!!! CANCELANDO PROCESO !!!");
                        process.Kill(); 
                    } 
                    catch { }
                    tcs.TrySetCanceled();
                }
            });

            return tcs.Task;
        }
        public async Task<RemoteMetadata> GetRemoteMetadataAsync(string url)
        {
            if (!DependencyManager.IsYtDlpInstalled()) return null;

            // --print "%(title)s|%(duration_string)s|%(thumbnail)s"
            // Use --flat-playlist to avoid downloading playlist items info if it's a playlist
            string args = string.Format("\"{0}\" --print \"%(title)s|%(duration_string)s|%(thumbnail)s|%(uploader)s\" --no-playlist --flat-playlist --skip-download --no-warnings", url);
            
            var tcs = new TaskCompletionSource<RemoteMetadata>();

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = DependencyManager.GetYtDlpPath(),
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8 // Ensure UTF8 for titles
                }
            };

            string output = "";

            process.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) output += e.Data; };
            process.EnableRaisingEvents = true;
            process.Exited += (s, e) =>
            {
                if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    var parts = output.Split('|');
                    if (parts.Length >= 3)
                    {
                        var meta = new RemoteMetadata
                        {
                            Title = parts[0].Trim(),
                            Duration = parts[1].Trim(),
                            ThumbnailUrl = parts[2].Trim(),
                            Uploader = parts.Length > 3 ? parts[3].Trim() : ""
                        };
                        tcs.TrySetResult(meta);
                    }
                    else
                    {
                         tcs.TrySetResult(null);
                    }
                }
                else
                {
                    tcs.TrySetResult(null);
                }
                process.Dispose();
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine(); // Consume error to prevent deadlock

            return await tcs.Task;
        }
    }

    public class RemoteMetadata
    {
        public string Title { get; set; }
        public string Duration { get; set; }
        public string ThumbnailUrl { get; set; }
        public string Uploader { get; set; }
    }
}
