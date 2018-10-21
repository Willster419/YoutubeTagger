using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagLib.Mpeg4;
using System.Xml;
using System.IO;

namespace YoutubePlaylistAlbumDownload
{
    
    class Program
    {
        public static readonly string[] ValidExtensions = new string[]
        {
            ".m4a",
            ".mp3"
        };
        private const string DownloadInfoXml = "DownloadInfo.xml";
        private static string CommandLine = "";
        private static List<DownloadInfo> DownloadInfos = new List<DownloadInfo>();
        static void Main(string[] args)
        {
            Console.WriteLine("Press enter to continue");
            //https://stackoverflow.com/questions/11512821/how-to-stop-c-sharp-console-applications-from-closing-automatically
            Console.ReadLine();
            if (!System.IO.File.Exists(DownloadInfoXml))
            {
                Console.WriteLine(string.Format("{0} is missing, application cannot continue",DownloadInfoXml));
                Console.ReadLine();
                return;
            }
            //TODO: here would be to update youtube-dl
            Console.WriteLine("Loading XML document");
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(DownloadInfoXml);
            }
            catch (XmlException ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
                return;
            }
            try
            {
                //https://www.freeformatter.com/xpath-tester.html#ad-output
                string xpath = string.Format("//{0}/@{1}", DownloadInfoXml, nameof(CommandLine));
                CommandLine = doc.SelectSingleNode(xpath).Value;
                foreach (XmlNode infosNode in doc.SelectNodes(string.Format("//{0}/{1}", DownloadInfoXml, nameof(DownloadInfo))))
                {
                    DownloadInfos.Add(new DownloadInfo
                    {
                        Folder = infosNode.Attributes[nameof(DownloadInfo.Folder)].Value,
                        Album = infosNode.Attributes[nameof(DownloadInfo.Album)].Value,
                        AlbumArtist = infosNode.Attributes[nameof(DownloadInfo.AlbumArtist)].Value,
                        Genre = infosNode.Attributes[nameof(DownloadInfo.Genre)].Value,
                        //LastDateDownloaded = infosNode.Attributes[nameof(DownloadInfo.LastDateDownloaded)].Value,
                        LastTrackNumber = int.Parse(infosNode.Attributes[nameof(DownloadInfo.LastTrackNumber)].Value),
                        //LengthFilter = infosNode.Attributes[nameof(DownloadInfo.LengthFilter)].Value,
                        //PlaylistURL = infosNode.Attributes[nameof(DownloadInfo.PlaylistURL)].Value
                    });
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
                return;
            }
            //run command to download them
            //TODO
            //edit the files
            //get a list of files from the directory listed 
            for(int j = 0; j < DownloadInfos.Count; j++)
            {
                DownloadInfo info = DownloadInfos[j];
                Console.WriteLine("Parsing directory" + info.Folder);
                if (!Directory.Exists(info.Folder))
                {
                    Console.WriteLine("Directory" + info.Folder + " does not exist");
                    Console.ReadLine();
                    continue;
                }
                //make and filter out the lists
                List<string> files = Directory.GetFiles(info.Folder).ToList();
                for(int i = 0; i < files.Count; i++)
                {
                    string file = files[i];
                    if (!ValidExtensions.Contains(Path.GetExtension(file)))
                    {
                        files.RemoveAt(i);
                        i--;
                        continue;
                    }
                }
                if(files.Count == 0)
                {
                    Console.WriteLine("files.Count=0");
                    Console.ReadLine();
                    return;
                }
                //parse the list of files
                for (int i = 0; i < files.Count; i++)
                {
                    string fileName = files[i];
                    Console.WriteLine("Parsing " + fileName);
                    TagLib.Mpeg4.File file = new TagLib.Mpeg4.File(fileName);
                    TagLib.Tag tag = file.Tag;
                    //assign tag infos
                    //album
                    tag.Album = info.Album;
                    //album artist
                    //https://stackoverflow.com/questions/17292142/taglib-sharp-not-editing-artist
                    if (!string.IsNullOrEmpty(info.AlbumArtist))
                    {
                        tag.AlbumArtists = null;
                        tag.AlbumArtists = new string[] { info.AlbumArtist };
                    }
                    tag.Track = (uint)++info.LastTrackNumber;
                    //parse from name
                    //title and artist
                    //idea: split the name using -
                    //if two, then it's a mix, artist is VA
                    //else index 1 is artist and 2 is track
                    string[] splitFileName = Path.GetFileNameWithoutExtension(fileName).Split('-');
                    if(splitFileName.Count() > 2)
                    {
                        //0 = dumb, 1 = artist, 2 = title
                        tag.Performers = null;
                        tag.Performers = new string[] { splitFileName[1] };
                        tag.Title = splitFileName[2];
                    }
                    else
                    {
                        //0 = dumb, 1 = title
                        tag.Performers = null;
                        tag.Performers = new string[] { "VA" };
                        tag.Title = splitFileName[1];
                    }
                    tag.Genres = null;
                    tag.Genres = new string[] { info.Genre };
                    tag.Year = (uint)DateTime.Now.Year;
                    file.Save();
                }
                //save the last date back at today
                //https://www.c-sharpcorner.com/blogs/date-and-time-format-in-c-sharp-programming1
                //yyyyMMdd
                //info.LastDateDownloaded = DateTime.Now.ToString("yyyyMMdd");
                //last track number already saved...

                //rename the tracks to be correct
                //pad the track string first
                int padNum = info.LastTrackNumber.ToString().Length;
                //make and filter out the lists
                files = Directory.GetFiles(info.Folder).ToList();
                for (int i = 0; i < files.Count; i++)
                {
                    string file = files[i];
                    if (!ValidExtensions.Contains(Path.GetExtension(file)))
                    {
                        files.RemoveAt(i);
                        i--;
                        continue;
                    }
                }
                foreach (string fileName in files)
                {
                    Console.WriteLine(string.Format("renaming {0}", fileName));
                    TagLib.Mpeg4.File file = new TagLib.Mpeg4.File(fileName);
                    string paddedNum = info.LastTrackNumber.ToString().PadLeft(padNum, '0');
                    string folderPath = Path.GetDirectoryName(fileName);
                    string oldFileName = Path.GetFileNameWithoutExtension(fileName);
                    //if artist[0] is VA, don't put it
                    string newFileName = string.Empty;
                    if (file.Tag.Performers[0].Equals("VA"))
                    {
                        newFileName = string.Format("{0}-{1}",file.Tag.Track.ToString(),file.Tag.Title);
                    }
                    else
                    {
                        newFileName = string.Format("{0}-{1} - {2}", file.Tag.Track.ToString(), file.Tag.Performers[0], file.Tag.Title);
                    }
                    string completeOldPath = Path.Combine(folderPath, oldFileName+Path.GetExtension(fileName));
                    string completeNewPath = Path.Combine(folderPath, newFileName+Path.GetExtension(fileName));
                    System.IO.File.Move(completeOldPath, completeNewPath);
                }
            }
            //save xml back
            //save last track count and last date saved
            //xpath select node where folder matches
            foreach (DownloadInfo info in DownloadInfos)
            {
                string xpath = string.Format("//{0}/{1}[@Folder='{2}']", DownloadInfoXml, nameof(DownloadInfo), info.Folder);
                XmlNode infoNode = doc.SelectSingleNode(xpath);
                if(infoNode == null)
                {
                    Console.WriteLine("failed to save node back folder=" + info.Folder);
                    continue;
                }
                //infoNode.Attributes["LastDateDownloaded"].Value = info.LastDateDownloaded;
                infoNode.Attributes["LastTrackNumber"].Value = info.LastTrackNumber.ToString();
            }
            doc.Save(DownloadInfoXml);
        }
    }
}
