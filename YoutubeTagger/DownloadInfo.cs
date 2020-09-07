namespace YoutubeTagger
{
    public enum DownloadType
    {
        YoutubeMix,
        YoutubeSong,
        Other1
    }
    public class DownloadInfo
    {
        //the type of download type to use
        public DownloadType DownloadType;
        //the name of the folder to process files
        public string Folder;
        public string Album;
        public string AlbumArtist;
        //from saved in xml file
        public uint LastTrackNumber;
        public string Genre;
        public string[] CopyPaths;
        public string LastDate;
        public bool FirstRun;
        public string DownloadURL;
        public uint BackupLastTrackNumber;
        public string CustomYoutubedlCommands = string.Empty;
        public bool Enabled = true;
    }
}
