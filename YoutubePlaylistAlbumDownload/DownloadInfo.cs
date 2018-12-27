using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoutubePlaylistAlbumDownload
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
        //public int Year;//from datetime now
        //public string Title;//from filename
        //public string Artist;//from filename or VA
        public string AlbumArtist;
        //from saved in xml file
        public int LastTrackNumber;
        public string Genre;
    }
}
