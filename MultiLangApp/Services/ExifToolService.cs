using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ClassicDownloader.Services
{
    public class ExifToolService
    {
        private const string ExifToolExe = "exiftool.exe";

        public bool IsExifToolAvailable()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ExifToolExe);
            return File.Exists(path);
        }

        public async Task<List<MetadataSection>> GetExifMetadataAsync(string filePath)
        {
            var sections = new List<MetadataSection>();
            string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ExifToolExe);

            if (!File.Exists(exePath)) return sections;

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = string.Format("-g -s \"{0}\"", filePath),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8 // Ensure encoding
            };

            string output = "";

            await Task.Run(() =>
            {
                using (var process = Process.Start(startInfo))
                {
                    output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                }
            });

            // Parse Output
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            MetadataSection currentSection = null;
            
            // Regex for Group Header: ---- GroupName ---- (flexible dashes and spaces)
            var groupRegex = new Regex(@"^\s*-+\s+(.+?)\s+-+\s*$");
            // Regex for Key Value: Key : Value (flexible spaces)
            var kvRegex = new Regex(@"^\s*([^:]+?)\s*:\s*(.*)$");

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var groupMatch = groupRegex.Match(line);
                if (groupMatch.Success)
                {
                    currentSection = new MetadataSection { Title = groupMatch.Groups[1].Value.Trim() };
                    sections.Add(currentSection);
                    continue;
                }

                if (currentSection != null)
                {
                    var kvMatch = kvRegex.Match(line);
                    if (kvMatch.Success)
                    {
                        var key = kvMatch.Groups[1].Value.Trim();
                        var val = kvMatch.Groups[2].Value.Trim();
                        if (!string.IsNullOrEmpty(key))
                        {
                            currentSection.Items.Add(new MetadataItem { Key = key, Value = val });
                        }
                    }
                }
            }

            return sections;
        }
    }
}
