namespace YoutubeTagger
{
    partial class Program
    {
        //NOTE: using initializers as defaults
        //if prompts should be used from command line entry
        private static bool NoPrompts = false;
        //if it should run the scripts
        private static bool RunScripts = false;
        //if we should parse tags
        private static bool ParseTags = false;
        //if we should copy files
        private static bool CopyFiles = false;
        //if we shuld copy binary files
        private static bool CopyBinaries = true;
        //if we should delete binary files
        private static bool DeleteBinaries = true;
        //if we should update youtubedl
        private static bool UpdateYoutubeDL = true;
        //if we are saving the new date for the last time the script was run
        private static bool SaveNewDate = true;
        //if we should stop the application at error message prompts
        private static bool NoErrorPrompts = false;
        //if we should force write ff binaries to disk
        private static bool ForceWriteFFBinaries = false;
        //the default command line args passed into youtube-dl
        private static string DefaultCommandLine = "-i --playlist-reverse --youtube-skip-dash-manifest {0} {1} --match-filter \"{2}\" -o \"%(autonumber)s-%(title)s.%(ext)s\" --format m4a --embed-thumbnail {3}";
        //the start of the dateafter command line arg
        private static string DateAfterCommandLine = "--dateafter";
        //the duration match filter for youtube songs (600 = 600 seconds -> 10 mins, less indicates a song)
        private static string YoutubeSongDurationCommandLine = "duration < 600";
        //the duration match filter for youtube mixes (600 = 600 seconds -> 10 mins, more indicates a mix)
        private static string YoutubeMixDurationCommandLine = "duration > 600";
        //default download URL of youtube-dl program
        private static string YoutubeDlUrl = "https://yt-dl.org/latest/youtube-dl.exe";
    }
}