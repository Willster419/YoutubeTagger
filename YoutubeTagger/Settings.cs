﻿namespace YoutubeTagger
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

        //if we should copy binary files
        private static bool CopyBinaries = true;

        //if we should delete binary files
        private static bool DeleteBinaries = true;

        //if we should update youtube-dl
        private static bool UpdateYoutubeDL = true;

        //if we are saving the new date for the last time the script was run
        private static bool SaveNewDate = true;

        //if we should stop the application at error message prompts
        private static bool NoErrorPrompts = false;

        //if we should force write ff binaries to disk
        private static bool ForceWriteFFBinaries = false;

        //if we should force the download of youtube-dl from the youtube-dl website
        private static bool ForceDownloadYoutubeDl = false;

        //if we should delete the output log files from each info folder
        private static bool DeleteOutputLogs = false;

        //the default command line args passed into youtube-dl
        private static string DefaultCommandLine = "-i --playlist-reverse --youtube-skip-dash-manifest {0} {1} --match-filter \\\"{2}\\\" -o \\\"%(autonumber)s-%(title)s.%(ext)s\\\" --format m4a --embed-thumbnail {3}";

        //the start of the dateafter command line arg
        private static string DateAfterCommandLine = "--dateafter";

        //the duration match filter for youtube songs (600 = 600 seconds -> 10 mins, less indicates a song)
        private static string YoutubeSongDurationCommandLine = "duration < 600";

        //the duration match filter for youtube mixes (600 = 600 seconds -> 10 mins, more indicates a mix)
        private static string YoutubeMixDurationCommandLine = "duration > 600";

        //default download URL of youtube-dl program. can be updated from the xml file
        private static string YoutubeDlUrl = "https://yt-dl.org/latest/youtube-dl.exe";

        //the regex search when reading the youtube-dl command log for creating the archive text
        private static string CreateArchiveRegex = @"^\[youtube\] .+:";

        //if true, for each copy folder, the program will read all song titles and perform the regex replace on the title
        private static bool ApplyRegexToCopyFolders = false;

        //if true, for each copy folder, the program will check for and correct all duplicate indexes
        //NOTE: the program does not perform a sort by date, all it does is take duplicate indexes and move them to the bottom
        //(i.e. the second song of index 42 will be made 69, if there are a total of 68 songs in the list currently)
        private static bool CheckAndFixDuplicateTrackNumbers = false;

        //if true, for each copy folder, the program will check all files for correct padding
        //(i.e. if song 1000 was added to a folder, then songs 000-999 will be renamed to 0000-0999)
        private static bool CheckAndFixFilePadding = false;
    }
}