using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoutubePlaylistAlbumDownload
{
    public class DownloadInfo
    {
        public string Folder;
        public string Album;
        //public int Year;//from datetime now
        //public string Title;//from filename
        //public string Artist;//from filename or VA
        public string AlbumArtist;
        //public int TrackNumber;//from saved in xml file
        //public string PlaylistURL;//not needed at this time
        //public string LengthFilter;//not needed at thie time
        //public string LastDateDownloaded;//not needed a this time
        public int LastTrackNumber;
        public string Genre;
    }
}
