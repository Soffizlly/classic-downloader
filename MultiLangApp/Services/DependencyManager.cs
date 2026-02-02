using System.IO;

namespace ClassicDownloader.Services
{
    public static class DependencyManager
    {
        private static string _toolsPath;

        static DependencyManager()
        {
            _toolsPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Tools");
        }

        public static string ToolsPath
        {
            get { return _toolsPath; }
        }

        public static string GetFfmpegPath()
        {
            return Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
        }

        public static string GetFfprobePath()
        {
            return Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "ffprobe.exe");
        }

        public static string GetYtDlpPath()
        {
            return Path.Combine(ToolsPath, "yt-dlp.exe");
        }

        public static bool IsFfmpegInstalled()
        {
            return File.Exists(GetFfmpegPath());
        }

        public static bool IsFfprobeInstalled()
        {
            return File.Exists(GetFfprobePath());
        }

        public static bool IsYtDlpInstalled()
        {
            return File.Exists(GetYtDlpPath());
        }
    }
}
