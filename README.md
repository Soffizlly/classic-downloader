# Classic Downloader (Alpha)

A minimalist yet powerful media downloader and converter for Windows. Built with WPF, FFMPEG, and ExifTool.

> [!WARNING]
> **ALPHA VERSION**
> This software is currently in Alpha state. It may contain bugs, incomplete features, or stability issues.
> Use at your own risk.

## Reporting Bugs
If you encounter any errors:
1.  Expand the **"Show Logs"** console at the bottom of the application.
2.  Copy the error details.
3.  Send them to **@soffizlly** on Discord.

## Features
*   **Video Downloader**: Download from YouTube and other sites in MP4, MKV, or WEBM (Best Quality).
*   **Audio Extractor**: High-quality audio extraction (MP3 320kbps, FLAC Lossless, WAV, AAC).
*   **Metadata Inspector**: Deep dive into file internals using **ExifTool** and **FFProbe** (requires `exiftool` installation via app).
*   **Media Converter**: Convert Audio+Image to Video, or transcode between formats.
*   **Minimalist UI**: Clean, responsive dashboard design with "Dark Mode" aesthetics.
*   **Portable**: No installation required. Just unzip and run.

## Requirements
*   Windows 10/11
*   .NET Framework 4.7.2 or later
*   Internet connection (for downloading dependencies like ExifTool on first run, if needed)

## Installation
1.  Download the latest `ClassicDownloader_Alpha.zip` from Releases.
2.  Extract the ZIP file to a folder.
3.  Run `MultiLangApp.exe`.

## License
[MIT License](LICENSE) - Free for personal use.

## Credits
*   **FFMPEG** for media conversion.
*   **yt-dlp** for video downloading.
*   **ExifTool** by Phil Harvey for metadata reading.
