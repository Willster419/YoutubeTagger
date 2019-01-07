using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagLib.Mpeg4;
using System.Xml;
using System.IO;
using System.Diagnostics;
using TagLib;

namespace YoutubePlaylistAlbumDownload
{
    
    class Program
    {
        public static readonly string[] ValidExtensions = new string[]
        {
            ".m4a",
            ".M4A",
            ".mp3",
            ".MP3"
        };

        static readonly string[] DownloadStepsFiles = new string[]
        {
            "1_update_youtube-dl_copyToFolders.bat",
            "2_run_scripts.bat",
            "3_save_new_date.bat",
            "4_cleanup.bat"
        };
        
        private const string DownloadInfoXml = "DownloadInfo.xml";
        //private static string CommandLine = "";
        private static List<DownloadInfo> DownloadInfos = new List<DownloadInfo>();
        private const string logfile = "logfile.log";

        private static void WriteToLog(string logMessage)
        {
            Console.WriteLine(logMessage);
            System.IO.File.AppendAllText(logfile, logMessage + Environment.NewLine);
        }

        static void Main(string[] args)
        {
            WriteToLog("Press enter to start");
            /* if(true)
            {
                using (WebClient client = new WebClient())
                {
                    HtmlDocument document = new HtmlDocument();
                    document.LoadHtml(client.DownloadString("https://hearthis.at/djflybeat/"));
                    //"/html[1]/body[1]/div[4]/div[1]/div[1]/section[1]/section[2]/div[1]/div[2]/div[1]/div[1]/ul[1]"
                    //https://stackoverflow.com/questions/15826875/html-agility-pack-using-xpath-to-get-a-single-node-object-reference-not-set
                    HtmlNode node = document.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[4]/div[1]/div[1]/section[1]/section[2]/div[1]/div[2]/div[1]/div[1]/ul[1]");
                    List<HtmlNode> musicEntries = node.ChildNodes.Skip(1).ToList();
                    //.Where(element => element.properties
                    musicEntries = musicEntries.Where(entry => entry.Name.Equals("li")).ToList();
                    foreach(HtmlNode musicEntry in musicEntries)
                    {
                        HtmlNode article = musicEntry.ChildNodes[1];
                        HtmlNode span = article.ChildNodes[1];
                        HtmlNode entity = span.ChildNodes[0];
                        string page = entity.Attributes["href"].Value;
                        HtmlDocument songPage = new HtmlDocument();
                        songPage.LoadHtml(client.DownloadString(page));
                    }
                }

                return;
            }
            */
            //https://stackoverflow.com/questions/11512821/how-to-stop-c-sharp-console-applications-from-closing-automatically
            Console.ReadLine();
            
            //run command to download them
            //ask user if we will run the scripts first
            WriteToLog("Run batch scripts?");
            bool runScripts = false;
            while(true)
            {
                if(bool.TryParse(Console.ReadLine(), out bool res))
                {
                    runScripts = res;
                    break;
                }
                else
                {
                    WriteToLog("Response must be true or false");
                }
            }

            //if yes, then run them!
            if(runScripts)
            {
                //tell the user which script we are running
                foreach(string s in DownloadStepsFiles)
                {
                    WriteToLog(string.Format("Running script {0}, press enter when done", s));
                    try
                    {
                        Process.Start(s);
                        Console.ReadLine();
                    }
                    catch (Exception ex)
                    {
                        WriteToLog(ex.ToString());
                        return;
                    }
                }
            }

            //init tag parsing, load xml data
            //check to make sure download info xml file is present
            if (!System.IO.File.Exists(DownloadInfoXml))
            {
                WriteToLog(string.Format("{0} is missing, application cannot continue", DownloadInfoXml));
                Console.ReadLine();
                return;
            }
            WriteToLog("Loading XML document");
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(DownloadInfoXml);
            }
            catch (XmlException ex)
            {
                WriteToLog(ex.ToString());
                Console.ReadLine();
                return;
            }
            try
            {
                //https://www.freeformatter.com/xpath-tester.html#ad-output
                //string xpath = string.Format("//{0}/@{1}", DownloadInfoXml, nameof(CommandLine));
                //CommandLine = doc.SelectSingleNode(xpath).Value;
                foreach (XmlNode infosNode in doc.SelectNodes(string.Format("//{0}/{1}", DownloadInfoXml, nameof(DownloadInfo))))
                {
                    DownloadInfos.Add(new DownloadInfo
                    {
                        Folder = infosNode.Attributes[nameof(DownloadInfo.Folder)].Value,
                        Album = infosNode.Attributes[nameof(DownloadInfo.Album)].Value,
                        AlbumArtist = infosNode.Attributes[nameof(DownloadInfo.AlbumArtist)].Value,
                        Genre = infosNode.Attributes[nameof(DownloadInfo.Genre)].Value,
                        LastTrackNumber = int.Parse(infosNode.Attributes[nameof(DownloadInfo.LastTrackNumber)].Value),
                        DownloadType = (DownloadType)Enum.Parse(typeof(DownloadType), infosNode.Attributes[nameof(DownloadInfo.DownloadType)].Value)
                    });
                }
            }
            catch (Exception ex)
            {
                WriteToLog(ex.ToString());
                Console.ReadLine();
                return;
            }

            //start naming and tagging
            //get a list of files from the directory listed 
            for (int j = 0; j < DownloadInfos.Count; j++)
            {
                DownloadInfo info = DownloadInfos[j];
                WriteToLog("-----------------------Parsing directory " + info.Folder + "----------------------");
                if (!Directory.Exists(info.Folder))
                {
                    WriteToLog("Directory " + info.Folder + " does not exist");
                    Console.ReadLine();
                    continue;
                }
                //make and filter out the lists
                List<string> files = Directory.GetFiles(info.Folder).Where(file => ValidExtensions.Contains(Path.GetExtension(file))).ToList();
                //check to make sure there are valid audio files before proceding
                if(files.Count == 0)
                {
                    WriteToLog("files.Count=0 (no valid audio files in directory)");
                    Console.ReadLine();
                    continue;
                }

                //step 0: check for if padding is needed
                //get the number of track numbers for the first and last files
                int firstEntryNumTrackNums = Path.GetFileName(files[0]).Split('-')[0].Length;
                int maxEntryNumTrackNums = 0;
                foreach (string s in files)
                {
                    string filename = Path.GetFileName(s);
                    int currentEntryNumTrackNums = filename.Split('-')[0].Length;
                    if (currentEntryNumTrackNums > maxEntryNumTrackNums)
                        maxEntryNumTrackNums = currentEntryNumTrackNums;
                }
                WriteToLog(string.Format("first entry, track number padding = {0}\nmax entry, track number padding = {1}\n",
                    firstEntryNumTrackNums,maxEntryNumTrackNums));
                if (firstEntryNumTrackNums != maxEntryNumTrackNums)
                {
                    //inform and ask
                    WriteToLog("Not equal! Pad entries?");
                    bool continuePad = false;
                    while (true)
                    {
                        if (bool.TryParse(Console.ReadLine(), out bool res))
                        {
                            continuePad = res;
                            break;
                        }
                        else
                        {
                            WriteToLog("Response must be true or false");
                        }
                    }
                    if(continuePad)
                    {
                        //use the last entry as reference point for how many paddings to do
                        for (int i = 0; i < files.Count; i++)
                        {
                            string oldFileName = Path.GetFileName(files[i]);
                            int numToPadOut = maxEntryNumTrackNums - oldFileName.Split('-')[0].Length;
                            if(numToPadOut > 0)
                            {
                                string newFileName = oldFileName.PadLeft(oldFileName.Length + numToPadOut, '0');
                                WriteToLog(string.Format("{0}\nrenamed to\n{1}", oldFileName, newFileName));
                                System.IO.File.Move(Path.Combine(info.Folder, oldFileName), Path.Combine(info.Folder, newFileName));
                                files[i] = Path.Combine(info.Folder, newFileName);
                            }
                            else
                            {
                                WriteToLog(string.Format("{0}\nnot renamed", oldFileName));
                            }
                        }
                        //and re-sort if afterwards
                        files.Sort();
                    }
                    else
                    {
                        WriteToLog("Exiting!");
                        System.Threading.Thread.Sleep(1000);
                        return;
                    }
                }

                //step 1: parse the tag info
                for (int i = 0; i < files.Count; i++)
                {
                    string fileName = files[i];
                    WriteToLog("Parsing " + fileName);

                    TagLib.Tag tag = null;
                    TagLib.File file = null;
                    try
                    {
                        //https://stackoverflow.com/questions/40826094/how-do-i-use-taglib-sharp
                        file = TagLib.File.Create(fileName);
                        tag = file.Tag;
                    }
                    catch (Exception ex)
                    {
                        WriteToLog(ex.ToString());
                        System.Threading.Thread.Sleep(1000);
                        return;
                    }

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
                    //last saved number in the xml will be the last track number applied
                    //so up it first, then use it
                    tag.Track = (uint)++info.LastTrackNumber;
                    string fileNameToParse = Path.GetFileNameWithoutExtension(fileName);
                    //hotfix - replace "–" with "-", as well as "?-" with "-"
                    fileNameToParse = fileNameToParse.Replace('–', '-').Replace("?-", "-");
                    string[] splitFileName = fileNameToParse.Split('-');
                    //parse from name
                    switch (info.DownloadType)
                    {
                        case DownloadType.Other1:
                            //0 = track (discard), 1 = title
                            tag.Performers = null;
                            tag.Performers = new string[] { "VA" };
                            //tag.Title = splitFileName[1];//these already have title parsed
                            WriteToLog("Song treated as heartAtThis mix");
                            break;
                        case DownloadType.YoutubeMix:
                            //0 = track (discard), 1 = title
                            tag.Performers = null;
                            tag.Performers = new string[] { "VA" };
                            //trim is as well, just in case
                            //and join the whole thing back together, in case the jackass publisher uses "-" in the title
                            //https://stackoverflow.com/questions/12961868/split-and-join-c-sharp-string
                            tag.Title = string.Join("-", splitFileName.Skip(1)).Trim();
                            WriteToLog("Song treated as youtube mix");
                            break;
                        case DownloadType.YoutubeSong:
                            //0 = track (discard), 1 = artist, 2 = title
                            //need at least 3 entries for this to work
                            if(splitFileName.Count() < 3)
                            {
                                WriteToLog("ERROR: not enough split entries for parsing, please enter manually! (count is " + splitFileName.Count() + " )");
                                WriteToLog("Original: " + Path.GetFileNameWithoutExtension(fileName));
                                WriteToLog("Enter new:");
                                while (true)
                                {
                                    string newFileName = Console.ReadLine();
                                    if(newFileName.Split('-').Count() < 3)
                                    {
                                        WriteToLog(string.Format("'{0}' does not have enough delimiters (need at least 3 to split)",newFileName));
                                    }
                                    else
                                    {
                                        splitFileName = newFileName.Split('-');
                                        break;
                                    }
                                }
                            }
                            tag.Performers = null;
                            tag.Performers = new string[] { splitFileName[1].Trim() };
                            //trim it as well, just in case
                            //tag.Title = splitFileName[2].Trim();
                            tag.Title = string.Join("-", splitFileName.Skip(2)).Trim();
                            //WriteToLog("DEBUG: IS THIS NAME CORRECT?");
                            //WriteToLog(tag.Title);
                            //Console.ReadLine();
                            WriteToLog("Song treated as youtube song");
                            break;
                        default:
                            WriteToLog("Invalid downloadtype: " + info.DownloadType.ToString());
                            continue;
                    }
                    //genre and year applied the same ways for all
                    tag.Genres = null;
                    tag.Genres = new string[] { info.Genre };
                    tag.Year = (uint)DateTime.Now.Year;
                    file.Save();
                }

                //step 2: parse the filenames from the tags
                foreach (string fileName in files)
                {
                    //load the file again
                    TagLib.File file = null;
                    try
                    {
                        //https://stackoverflow.com/questions/40826094/how-do-i-use-taglib-sharp
                        file = TagLib.File.Create(fileName);
                    }
                    catch (Exception ex)
                    {
                        WriteToLog(ex.ToString());
                        System.Threading.Thread.Sleep(1000);
                        return;
                    }
                    //get the old name
                    string oldFileName = Path.GetFileNameWithoutExtension(fileName);
                    //prepare the new name
                    string newFileName = string.Empty;
                    //manual check to make sure track and title exist
                    if(file.Tag.Track == 0)
                    {
                        while(true)
                        {
                            WriteToLog("ERROR: Track property is missing, please input manually!");
                            if (uint.TryParse(Console.ReadLine(),out uint result))
                            {
                                file.Tag.Track = result;
                                file.Save();
                            }
                        }
                    }
                    if(string.IsNullOrWhiteSpace(file.Tag.Title))
                    {
                        WriteToLog("ERROR: Title property is missing, please input manually!");
                        file.Tag.Title = Console.ReadLine();
                        file.Save();
                    }
                    switch(info.DownloadType)
                    {
                        case DownloadType.Other1:
                            //using the pre-parsed title...
                            newFileName = string.Format("{0}-{1}", file.Tag.Track.ToString(), file.Tag.Title);
                            break;
                        case DownloadType.YoutubeMix:
                            newFileName = string.Format("{0}-{1}", file.Tag.Track.ToString(), file.Tag.Title);
                            break;
                        case DownloadType.YoutubeSong:
                            if(file.Tag.Performers == null || file.Tag.Performers.Count() == 0)
                            {
                                WriteToLog("ERROR: Artist property is missing, please input manually!");
                                file.Tag.Performers = null;
                                file.Tag.Performers = new string[] { Console.ReadLine() };
                                file.Save();
                            }
                            newFileName = string.Format("{0}-{1} - {2}", file.Tag.Track.ToString(), file.Tag.Performers[0], file.Tag.Title);
                            break;
                        default:
                            WriteToLog("Invalid downloadtype: " + info.DownloadType.ToString());
                            Console.ReadLine();
                            continue;
                    }
                    //check for padding
                    //set padding to highest number of tracknumbers
                    //(if tracks go from 1-148, make sure filename for 1 is 001)
                    int trackPaddingLength = newFileName.Split('-')[0].Length;
                    int maxTrackNumPaddingLength = info.LastTrackNumber.ToString().Length;
                    if(trackPaddingLength < maxTrackNumPaddingLength)
                    {
                        WriteToLog("Correcting for track padding");
                        int numToPad = maxTrackNumPaddingLength - trackPaddingLength;
                        newFileName = newFileName.PadLeft(newFileName.Length + numToPad, '0');
                    }
                    //save the complete folder path
                    string completeFolderPath = Path.GetDirectoryName(fileName);
                    string completeOldPath = Path.Combine(completeFolderPath, oldFileName+Path.GetExtension(fileName));
                    string completeNewPath = Path.Combine(completeFolderPath, newFileName+Path.GetExtension(fileName));
                    WriteToLog(string.Format("renaming {0}\nto {1}", oldFileName,newFileName));
                    System.IO.File.Move(completeOldPath, completeNewPath);
                }

                //at the end of each folder, write the new value back to the xml file
                string xpath = string.Format("//{0}/{1}[@Folder='{2}']", DownloadInfoXml, nameof(DownloadInfo), info.Folder);
                XmlNode infoNode = doc.SelectSingleNode(xpath);
                if (infoNode == null)
                {
                    WriteToLog("failed to save node back folder=" + info.Folder);
                    continue;
                }
                infoNode.Attributes["LastTrackNumber"].Value = info.LastTrackNumber.ToString();
                doc.Save(DownloadInfoXml);
                WriteToLog("Saved LastTrackNumber for folder " + info.Folder);
            }
            
            //and we're all set here
            WriteToLog("Done");
            Console.ReadLine();
        }
    }
}
