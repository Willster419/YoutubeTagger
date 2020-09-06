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
            YoutubeDL,
            CommandShellWrapper
        };

        //name of command shell wrapper application
        private const string CommandShellWrapper = "CommandShellWrapper.exe";

        //name of youtube-dl application
        private const string YoutubeDL = "youtube-dl.exe";

        //name of folder to keep above binary files
        private const string BinaryFolder = "bin";

        //name of atomic parsley
        private const string AtomicParsley = "AtomicParsley.exe";

        //name of xml file containing all download information
        private const string DownloadInfoXml = "DownloadInfo.xml";

        //logile for the application
        private const string Logfile = "logfile.log";

        //name of logfile from command line shell wrapper
        private const string CommandLineWrapperLogfile = "Output.Log";
    }
}