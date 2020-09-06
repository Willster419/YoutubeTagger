using System.Collections.Generic;

namespace YoutubeTagger
{
    partial class Program
    {
        //list of valid audio extensions for the scope of this project
        private static readonly string[] ValidExtensions = new string[]
        {
            ".m4a",
            ".M4A",
            ".mp3",
            ".MP3"
        };
        //list of binaries used for the scope of this project
        private static readonly string[] BinaryFiles = new string[]
        {
            "AtomicParsley.exe",
            "ffmpeg.exe",
            "ffprobe.exe",
            YoutubeDL
        };
        //name of youtube-dl application
        private const string YoutubeDL = "youtube-dl.exe";
        //name of folder to keep above binary files
        private const string BinaryFolder = "bin";
        //name of atomic parsley
        private const string AtomicParsley = "AtomicParsley.exe";
        //name of xml file containing all download information
        private const string DownloadInfoXml = "DownloadInfo.xml";
        //list to be parse of info from above defined xml file
        private static List<DownloadInfo> DownloadInfos = new List<DownloadInfo>();
        //logile for the application
        private const string Logfile = "logfile.log";
    }
}