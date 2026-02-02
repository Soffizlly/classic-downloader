using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Text;

namespace MultiLangApp.Services
{
    public class FfmpegService
    {
        public async Task ConvertAsync(string inputFile, string outputFormat, string outputFolder, IProgress<string> logProgress, IProgress<double> percentProgress, CancellationToken token, string imageFile = null, string resolution = null)
        {
            if (!DependencyManager.IsFfmpegInstalled())
                throw new FileNotFoundException("ffmpeg.exe not found.");

            string inputFileName = Path.GetFileNameWithoutExtension(inputFile);
            string outputFile = Path.Combine(outputFolder, inputFileName + "." + outputFormat.ToLower());
            
            string args = "";

            // Check if input is audio and we are converting to video with an image
            bool isAudioInput = IsAudioFile(inputFile);
            bool isVideoOutput = outputFormat.ToLower() == "mp4" || outputFormat.ToLower() == "mkv" || outputFormat.ToLower() == "avi";

            if (isAudioInput && isVideoOutput && !string.IsNullOrEmpty(imageFile))
            {
                 string scaleFilter = "";
                 if (!string.IsNullOrEmpty(resolution) && resolution.Contains(":"))
                 {
                     string[] parts = resolution.Split(':');
                     if (parts.Length == 2)
                     {
                         string w = parts[0];
                         string h = parts[1];
                         scaleFilter = string.Format(" -vf \"scale={0}:{1}:force_original_aspect_ratio=decrease,pad={0}:{1}:(ow-iw)/2:(oh-ih)/2\"", w, h);
                     }
                 }

                 if (string.IsNullOrEmpty(scaleFilter))
                 {
                     scaleFilter = " -vf \"pad=ceil(iw/2)*2:ceil(ih/2)*2\"";
                 }

                 args = string.Format("-loop 1 -framerate 2 -i \"{0}\" -i \"{1}\" -c:v libx264 -tune stillimage -c:a aac -b:a 192k -pix_fmt yuv420p{2} -shortest \"{3}\"", imageFile, inputFile, scaleFilter, outputFile);
            }
            else
            {
                switch (outputFormat.ToLower())
                {
                    case "mp4":
                        args = string.Format("-i \"{0}\" -c:v libx264 -crf 18 -preset medium -c:a aac -b:a 192k \"{1}\"", inputFile, outputFile);
                        break;
                    case "mkv":
                        args = string.Format("-i \"{0}\" -c:v copy -c:a copy \"{1}\"", inputFile, outputFile);
                        break;
                    case "mp3":
                        args = string.Format("-i \"{0}\" -vn -q:a 0 \"{1}\"", inputFile, outputFile);
                        break;
                    case "wav":
                        args = string.Format("-i \"{0}\" -vn \"{1}\"", inputFile, outputFile);
                        break;
                    default:
                        args = string.Format("-i \"{0}\" \"{1}\"", inputFile, outputFile);
                        break;
                }
            }

            args = "-y " + args;

            await RunProcessAsync(DependencyManager.GetFfmpegPath(), args, logProgress, percentProgress, token);
        }

        private bool IsAudioFile(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            return ext == ".mp3" || ext == ".wav" || ext == ".flac" || ext == ".m4a" || ext == ".aac" || ext == ".ogg" || ext == ".wma";
        }

        public async Task<MediaMetadata> GetMetadataAsync(string inputFile)
        {
            if (!DependencyManager.IsFfmpegInstalled()) return null;

            // Use a StringBuilder to capture stderr
            StringBuilder outputBuilder = new StringBuilder();

            var args = string.Format("-i \"{0}\"", inputFile);
            
            // Run process and capture stderr
            // We can reuse RunProcessAsync but we need the output string.
            // Let's create a custom runner for this simpler case.
            
            return await Task.Run(() =>
            {
                try
                {
                    var p = new Process();
                    p.StartInfo.FileName = DependencyManager.GetFfmpegPath();
                    p.StartInfo.Arguments = args;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.RedirectStandardError = true;
                    
                    p.Start();
                    string output = p.StandardError.ReadToEnd();
                    p.WaitForExit();

                    var meta = new MediaMetadata();
                    meta.Title = ExtractTag(output, "title");
                    meta.Artist = ExtractTag(output, "artist");
                    meta.Album = ExtractTag(output, "album");
                    meta.Year = ExtractTag(output, "date"); 
                    if (string.IsNullOrEmpty(meta.Year)) meta.Year = ExtractTag(output, "year");
                    meta.Genre = ExtractTag(output, "genre");
                    
                    return meta;
                }
                catch (Exception)
                {
                    return null;
                }
            });
        }

        private string ExtractTag(string output, string tagName)
        {
            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith(tagName, StringComparison.OrdinalIgnoreCase))
                {
                    var parts = trimmed.Split(new[] { ':' }, 2);
                    if (parts.Length > 1) return parts[1].Trim();
                }
            }
            return "";
        }

        public async Task<string> GetProbeDataAsync(string inputFile)
        {
            if (!DependencyManager.IsFfprobeInstalled()) return "ffprobe.exe not found.";

            string ffprobePath = DependencyManager.GetFfprobePath();
            string args = string.Format("-v quiet -print_format xml -show_format -show_streams -show_chapters -show_private_data \"{0}\"", inputFile);

            return await Task.Run(() =>
            {
                try
                {
                    var p = new Process();
                    p.StartInfo.FileName = ffprobePath;
                    p.StartInfo.Arguments = args;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.RedirectStandardOutput = true; 
                    p.StartInfo.StandardOutputEncoding = Encoding.UTF8;

                    p.Start();
                    string output = p.StandardOutput.ReadToEnd(); // ffprobe xml goes to stdout
                    p.WaitForExit();
                    
                    return output;
                }
                catch (Exception)
                {
                    return null;
                }
            });
        }

        public async Task ExtractCoverArtAsync(string inputFile, string outputPath)
        {
            string args = string.Format("-y -i \"{0}\" -an -vcodec copy \"{1}\"", inputFile, outputPath);
            await RunProcessAsync(DependencyManager.GetFfmpegPath(), args, null, null, CancellationToken.None);
        }

        public async Task SaveMetadataAsync(string inputFile, MediaMetadata meta, string coverPath, string outputFolder, IProgress<string> log)
        {
            string tempFile = Path.Combine(outputFolder, "temp_" + Path.GetFileName(inputFile));
            
            string metaArgs = "";
            if (meta.Title != null) metaArgs += string.Format(" -metadata title=\"{0}\"", meta.Title);
            if (meta.Artist != null) metaArgs += string.Format(" -metadata artist=\"{0}\"", meta.Artist);
            if (meta.Album != null) metaArgs += string.Format(" -metadata album=\"{0}\"", meta.Album);
            if (meta.Year != null) metaArgs += string.Format(" -metadata date=\"{0}\"", meta.Year);
            if (meta.Genre != null) metaArgs += string.Format(" -metadata genre=\"{0}\"", meta.Genre);

            string args = "";
            if (!string.IsNullOrEmpty(coverPath))
            {
                // New cover art
                args = string.Format("-i \"{0}\" -i \"{1}\" -map 0:a -map 1 -c:a copy -c:v copy -id3v2_version 3 {2} \"{3}\"", inputFile, coverPath, metaArgs, tempFile);
            }
            else
            {
                // Just tags
                args = string.Format("-i \"{0}\" -c copy {1} \"{2}\"", inputFile, metaArgs, tempFile);
            }

            args = "-y " + args;
            await RunProcessAsync(DependencyManager.GetFfmpegPath(), args, log, null, CancellationToken.None);

            if (File.Exists(tempFile))
            {
                if (File.Exists(inputFile)) File.Delete(inputFile);
                File.Move(tempFile, inputFile);
            }
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
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            bool durationFound = false;
            TimeSpan totalDuration = TimeSpan.Zero;
            var regexDuration = new Regex(@"Duration: (\d{2}):(\d{2}):(\d{2})");
            var regexTime = new Regex(@"time=(\d{2}):(\d{2}):(\d{2})");

            Action<string> outputHandler = (line) =>
            {
                if (string.IsNullOrEmpty(line)) return;
                if (log != null) log.Report(line);

                // Find Duration
                if (!durationFound)
                {
                    var mDur = regexDuration.Match(line);
                    if (mDur.Success)
                    {
                        totalDuration = TimeSpan.Parse(string.Format("{0}:{1}:{2}", mDur.Groups[1], mDur.Groups[2], mDur.Groups[3]));
                        durationFound = true;
                    }
                }

                // Find Progress
                if (durationFound && totalDuration.TotalSeconds > 0)
                {
                    var mTime = regexTime.Match(line);
                    if (mTime.Success)
                    {
                        var currentTime = TimeSpan.Parse(string.Format("{0}:{1}:{2}", mTime.Groups[1], mTime.Groups[2], mTime.Groups[3]));
                        double p = (currentTime.TotalSeconds / totalDuration.TotalSeconds) * 100;
                        if (p > 100) p = 100;
                        if (percent != null) percent.Report(p);
                    }
                }
            };

            process.OutputDataReceived += (s, e) => outputHandler(e.Data);
            process.ErrorDataReceived += (s, e) => outputHandler(e.Data);

            process.Exited += (s, e) =>
            {
                if (process.ExitCode == 0) tcs.TrySetResult(true);
                else tcs.TrySetException(new Exception("Process exited with code " + process.ExitCode));
                process.Dispose();
            };

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                token.Register(() => 
                {
                    if (!process.HasExited)
                    {
                        try { process.Kill(); } catch {}
                    }
                    tcs.TrySetCanceled();
                });
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }

            return tcs.Task;
        }
    }
}
