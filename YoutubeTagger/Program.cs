using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using System.Reflection;
using System.ComponentModel;

namespace YoutubeTagger
{

    partial class Program
    {
        static void Init()
        {
            //hook up assembly resolver
            //https://stackoverflow.com/a/25990979/3128017
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        static int Main(string[] args)
        {
            Init();
            return RunProgram(args);
        }

        static int RunProgram(string[] args)
        {
            //init tag parsing, load xml data
            //check to make sure download info xml file is present
            if (!File.Exists(DownloadInfoXml))
            {
                WriteToLog(string.Format("{0} is missing, application cannot continue", DownloadInfoXml));
                Console.ReadLine();
                Environment.Exit(-1);
            }
            WriteToLog("---------------------------APPLICATION START---------------------------");
            WriteToLog("Loading Xml document");
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(DownloadInfoXml);
            }
            catch (XmlException ex)
            {
                WriteToLog("Failed to load Xml document");
                WriteToLog(ex.ToString());
                Console.ReadLine();
                Environment.Exit(-1);
            }
            try
            {
                //https://www.freeformatter.com/xpath-tester.html#ad-output
                //get some default settings
                NoPrompts = bool.Parse(doc.SelectSingleNode("/DownloadInfo.xml/Settings/NoPrompts").InnerText.Trim());
                NoErrorPrompts = bool.Parse(doc.SelectSingleNode("/DownloadInfo.xml/Settings/NoErrorPrompts").InnerText.Trim());
                ForceWriteFFBinaries = bool.Parse(doc.SelectSingleNode("/DownloadInfo.xml/Settings/ForceWriteFFBinaries").InnerText.Trim());
                UpdateYoutubeDL = bool.Parse(doc.SelectSingleNode("/DownloadInfo.xml/Settings/UpdateYoutubeDL").InnerText.Trim());
                CopyBinaries = bool.Parse(doc.SelectSingleNode("/DownloadInfo.xml/Settings/CopyBinaries").InnerText.Trim());
                RunScripts = bool.Parse(doc.SelectSingleNode("/DownloadInfo.xml/Settings/RunScripts").InnerText.Trim());
                SaveNewDate = bool.Parse(doc.SelectSingleNode("/DownloadInfo.xml/Settings/SaveNewDate").InnerText.Trim());
                ParseTags = bool.Parse(doc.SelectSingleNode("/DownloadInfo.xml/Settings/ParseTags").InnerText.Trim());
                CopyFiles = bool.Parse(doc.SelectSingleNode("/DownloadInfo.xml/Settings/CopyFiles").InnerText.Trim());
                DeleteBinaries = bool.Parse(doc.SelectSingleNode("/DownloadInfo.xml/Settings/DeleteBinaries").InnerText.Trim());

                //and some default command line settings
                DefaultCommandLine = doc.SelectSingleNode("/DownloadInfo.xml/CommandLine/Default").InnerText.Trim();
                DateAfterCommandLine = doc.SelectSingleNode("/DownloadInfo.xml/CommandLine/DateAfter").InnerText.Trim();
                YoutubeMixDurationCommandLine = doc.SelectSingleNode("/DownloadInfo.xml/CommandLine/YoutubeMixDuration").InnerText.Trim();
                YoutubeSongDurationCommandLine = doc.SelectSingleNode("/DownloadInfo.xml/CommandLine/YoutubeSongDuration").InnerText.Trim();
                YoutubeDlUrl = UpdateTextFromXmlEntry(nameof(YoutubeDlUrl), YoutubeDlUrl, doc, "/DownloadInfo.xml/CommandLine/YoutubeDlUrl");

                //for each xml element "DownloadInfo" in element "DownloadInfo.xml"
                foreach (XmlNode infosNode in doc.SelectNodes("//DownloadInfo.xml/DownloadInfos/DownloadInfo"))
                {
                    DownloadInfo temp = new DownloadInfo();

                    //get list of fields in the DownloadInfo class
                    List<FieldInfo> fields = temp.GetType().GetFields().ToList();
                    foreach (XmlAttribute attribute in infosNode.Attributes)
                    {
                        //find a field with the matching attribute name
                        FieldInfo field = fields.Find(fieldd => fieldd.Name.Equals(attribute.Name));

                        //convert the string value to the data type
                        var converter = TypeDescriptor.GetConverter(field.FieldType);
                        try
                        {
                            field.SetValue(temp, converter.ConvertFrom(attribute.Value));
                        }
                        catch(Exception ex)
                        {
                            WriteToLog(string.Format("ERROR: Failed to parse xml attribute '{0}' to data type", field.Name));
                            WriteToLog(ex.ToString());
                            Console.ReadLine();
                            Environment.Exit(-1);
                        }
                    }

                    //get the list of paths that the parsed music files should be copied to
                    XmlNodeList pathsList = infosNode.ChildNodes;
                    if (pathsList.Count > 0)
                    {
                        temp.CopyPaths = new string[pathsList.Count];
                        int i = 0;
                        foreach (XmlNode paths in pathsList)
                        {
                            //check to make sure the path is valid before trying to use later
                            if (!Directory.Exists(paths.InnerText))
                            {
                                if (temp.FirstRun)
                                {
                                    WriteToLog(string.Format("INFO: Path {0} does not exist, but firstRun = true, creating path", paths.InnerText));
                                    Directory.CreateDirectory(paths.InnerText);
                                }
                                else
                                {
                                    WriteToLog(string.Format("ERROR: The folder '{0}' declared in the xml does not exist",paths.InnerText));
                                    Console.ReadLine();
                                    Environment.Exit(-1);
                                }
                            }
                            temp.CopyPaths[i++] = paths.InnerText;
                        }
                    }
                    else
                    {
                        WriteToLog("ERROR: Paths count is 0 for downloadFolder of folder attribute " + infosNode.Attributes[nameof(DownloadInfo.Folder)].Value);
                        Environment.Exit(-1);
                    }

                    //if it's the first time running, then we can set the last track count to 0 (if not already)
                    if (temp.FirstRun)
                        temp.LastTrackNumber = 0;

                    //and finally add it to the list
                    DownloadInfos.Add(temp);
                }
            }
            catch (Exception ex)
            {
                WriteToLog(ex.ToString());
                Console.ReadLine();
                Environment.Exit(-1);
            }

            //check to make sure we have at least one downloadInfo to run!
            if (DownloadInfos.Count == 0)
            {
                WriteToLog("No DownloadInfos parsed! (empty xml file?)");
                if (!NoErrorPrompts)
                    Console.ReadLine();
                Environment.Exit(-1);
            }

            //check to make sure at least one downloadInfo enabled
            List<DownloadInfo> enabledInfos = DownloadInfos.Where(info => info.Enabled).ToList();
            if (enabledInfos.Count == 0)
            {
                WriteToLog("No DownloadInfos enabled!");
                if (!NoErrorPrompts)
                    Console.ReadLine();
                Environment.Exit(-1);
            }

            //only process enabled infos
            DownloadInfos = enabledInfos;

            //if not silent, add start of application here
            if (!NoPrompts)
            {
                WriteToLog("Press enter to start");
                //https://stackoverflow.com/questions/11512821/how-to-stop-c-sharp-console-applications-from-closing-automatically
                Console.ReadLine();
            }

            //run an update on youtube-dl
            //https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process?redirectedfrom=MSDN&view=netframework-4.7.2
            if (!NoPrompts)
            {
                UpdateYoutubeDL = GetUserResponse("UpdateYoutubeDL?");
            }
            if (UpdateYoutubeDL)
            {
                WriteToLog("Update YoutubeDL");
                //create folder if it does not already exist
                CheckMissingFolder(BinaryFolder);

                //check if youtube-dl is missing, if so download it
                string youtubeDLPath = Path.Combine(BinaryFolder, YoutubeDL);
                if (!File.Exists(youtubeDLPath))
                {
                    WriteToLog("Youtube-dl.exe does not exist, download it");
                    try
                    {
                        using (WebClient client = new WebClient())
                        {
                            client.DownloadFile(YoutubeDlUrl, youtubeDLPath);
                        }
                    }
                    catch (WebException ex)
                    {
                        WriteToLog("Failed to download youtube-dl.exe");
                        WriteToLog(ex.ToString());
                        if (!NoErrorPrompts)
                            Console.ReadLine();
                        Environment.Exit(-1);
                    }
                }
                else
                    WriteToLog("Youtube-dl.exe exists");
                try
                {
                    using (Process updateYoutubeDL = new Process())
                    {
                        //set properties
                        updateYoutubeDL.StartInfo.RedirectStandardError = false;
                        updateYoutubeDL.StartInfo.RedirectStandardOutput = false;
                        updateYoutubeDL.StartInfo.UseShellExecute = true;
                        updateYoutubeDL.StartInfo.WorkingDirectory = BinaryFolder;
                        updateYoutubeDL.StartInfo.FileName = YoutubeDL;
                        updateYoutubeDL.StartInfo.CreateNoWindow = false;
                        updateYoutubeDL.StartInfo.Arguments = "--update";
                        updateYoutubeDL.Start();
                        updateYoutubeDL.WaitForExit();
                        if (updateYoutubeDL.ExitCode != 0)
                        {
                            WriteToLog(string.Format("ERROR: update process exited with code {0}", updateYoutubeDL.ExitCode));
                            if (!NoErrorPrompts)
                                Console.ReadLine();
                            Environment.Exit(-1);
                        }
                        System.Threading.Thread.Sleep(TimeSpan.FromSeconds(10.0));
                    }
                }
                catch (Exception e)
                {
                    WriteToLog(e.ToString());
                    if (!NoErrorPrompts)
                        Console.ReadLine();
                    Environment.Exit(-1);
                }
            }
            else
                WriteToLog("UpdateYoutubeDl skipped");

            if (!NoPrompts)
            {
                CopyBinaries = GetUserResponse("CopyBinaries?");
            }
            if (CopyBinaries)
            {
                WriteToLog("Copy Binaries");
                CheckMissingFolder(BinaryFolder);

                //if embedded binary does not exist, OR force binary embedded write, then write it
                foreach (string s in EmbeddedBinaryFiles)
                {
                    string binaryPath = Path.Combine(BinaryFolder, s);
                    if (!File.Exists(binaryPath) || ForceWriteFFBinaries)
                    {
                        WriteToLog(string.Format("File {0} does not exist or ForceWriteFFBinaries is on, writing binaries to disk", s));
                        File.WriteAllBytes(binaryPath, GetEmbeddedResource(Path.GetFileNameWithoutExtension(s)));
                    }
                }

                //copy the binaries to each folder
                foreach (DownloadInfo info in DownloadInfos.Where(temp => temp.DownloadType == DownloadType.YoutubeSong || temp.DownloadType == DownloadType.YoutubeMix))
                {
                    CheckMissingFolder(info.Folder);
                    foreach (string binaryFile in BinaryFiles)
                    {
                        WriteToLog(string.Format("Copying file {0} into folder {1}", binaryFile, info.Folder));
                        string fileToCopy = Path.Combine(info.Folder, binaryFile);
                        if (File.Exists(fileToCopy))
                            File.Delete(fileToCopy);
                        File.Copy(Path.Combine(BinaryFolder, binaryFile), fileToCopy);
                    }
                }
            }
            else
                WriteToLog("CopyBinaries skipped");

            //ask user if we will run the scripts
            if (!NoPrompts)
            {
                RunScripts = GetUserResponse("RunScripts?");
            }
            if (RunScripts)
            {
                WriteToLog("Running scripts");
                //build and run the process list for youtube downloads
                //build first
                List<Process> processes = new List<Process>();
                //only create processes for youtube download types
                foreach (DownloadInfo info in DownloadInfos.Where(temp => temp.DownloadType == DownloadType.YoutubeSong || temp.DownloadType == DownloadType.YoutubeMix))
                {
                    //make sure folder path exists
                    CheckMissingFolder(info.Folder);
                    //make sure required binaries exist
                    foreach (string binaryFile in BinaryFiles)
                        CheckMissingFile(Path.Combine(info.Folder, binaryFile));
                    //delete any previous song file entries
                    foreach (string file in Directory.GetFiles(info.Folder, "*", SearchOption.TopDirectoryOnly).Where(file => ValidExtensions.Contains(Path.GetExtension(file))))
                    {
                        WriteToLog("Deleting old song file from previous run: " + Path.GetFileName(file));
                        File.Delete(file);
                    }
                    WriteToLog(string.Format("Build process info folder {0}", info.Folder));
                    processes.Add(new Process()
                    {
                        StartInfo = new ProcessStartInfo()
                        {
                            RedirectStandardError = false,
                            RedirectStandardOutput = false,
                            UseShellExecute = true,
                            WorkingDirectory = info.Folder,
                            FileName = YoutubeDL,
                            CreateNoWindow = false,
                            Arguments = string.Format(DefaultCommandLine,//from xml
                                info.FirstRun ? string.Empty : DateAfterCommandLine,//date after (youtube-dl command line key)
                                info.FirstRun ? string.Empty : info.LastDate,//date after (youtube-dl command line arg)
                                info.DownloadType == DownloadType.YoutubeMix ? YoutubeMixDurationCommandLine : YoutubeSongDurationCommandLine,//youtube-dl match filter duration selector
                                string.IsNullOrEmpty(info.CustomYoutubedlCommands) ? string.Empty : info.CustomYoutubedlCommands,
                                info.DownloadURL)
                        }
                    });
                }
                //run them all now
                foreach (Process p in processes)
                {
                    try
                    {
                        WriteToLog(string.Format("Launching process for folder {0} using arguments {1}", p.StartInfo.WorkingDirectory, p.StartInfo.Arguments));
                        p.Start();
                    }
                    catch (Exception ex)
                    {
                        WriteToLog("An error has occurred running a process, stopping all");
                        KillProcesses(processes);
                        WriteToLog(ex.ToString());
                        if (!NoErrorPrompts)
                            Console.ReadLine();
                        Environment.Exit(-1);
                    }
                }
                //iterate to wait for all to complete
                foreach (Process p in processes)
                {
                    p.WaitForExit();
                    WriteToLog(string.Format("Process of folder {0} has finished or previously finished", p.StartInfo.WorkingDirectory));
                }
                //check exit code status
                bool forceExit = false;
                foreach (Process p in processes)
                {
                    WriteToLog(string.Format("Process of folder {0} exited of code {1}", p.StartInfo.WorkingDirectory, p.ExitCode));
                    if (p.ExitCode == 0 || p.ExitCode == 1)
                    {
                        WriteToLog("Valid exit code");
                    }
                    else
                    {
                        WriteToLog("Invalid exit code, marked to exit");
                        forceExit = true;
                    }
                }
                //determine if to quit
                if (forceExit)
                {
                    WriteToLog("An exit code above was bad, exiting");
                    KillProcesses(processes);
                    if (!NoErrorPrompts)
                        Console.ReadLine();
                    Environment.Exit(-1);
                }
                //after all completed successfully, dispose of them
                foreach (Process p in processes)
                    p.Dispose();
                GC.Collect();
                WriteToLog("All processes completed");
            }
            else
                WriteToLog("RunScripts skipped");

            //save the new date for when the above scripts were last run
            if (!NoPrompts)
            {
                SaveNewDate = GetUserResponse("SaveNewDate?");
            }
            if (SaveNewDate)
            {
                WriteToLog("Saving new date");
                //https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings
                string newDate = string.Format("{0:yyyyMMdd}", DateTime.Now);
                for (int i = 0; i < DownloadInfos.Count; i++)
                {
                    WriteToLog(string.Format("Changing and saving xml old date from {0} to {1}", DownloadInfos[i].LastDate, newDate));
                    DownloadInfos[i].LastDate = newDate;
                    //then save it in xml
                    string xpath = string.Format("//DownloadInfo.xml/DownloadInfos/DownloadInfo[@Folder='{0}']", DownloadInfos[i].Folder);
                    XmlNode infoNode = doc.SelectSingleNode(xpath);
                    if (infoNode == null)
                    {
                        WriteToLog("Failed to save xml doc back folder " + DownloadInfos[i].Folder);
                        if (!NoErrorPrompts)
                            Console.ReadLine();
                        Environment.Exit(-1);
                    }
                    XmlAttribute lastDate = infoNode.Attributes["LastDate"];
                    if (lastDate == null)
                    {
                        //make it first then
                        lastDate = doc.CreateAttribute("LastDate");
                        infoNode.Attributes.Append(lastDate);
                    }
                    lastDate.Value = DownloadInfos[i].LastDate;
                    doc.Save(DownloadInfoXml);
                }
            }
            else
                WriteToLog("SaveNewDate skipped");

            //start naming and tagging
            //get a list of files from the directory listed
            if (!NoPrompts)
            {
                ParseTags = GetUserResponse("ParseTags?");
            }
            if (ParseTags)
            {
                WriteToLog("Parsing tags");
                for (int j = 0; j < DownloadInfos.Count; j++)
                {
                    DownloadInfo info = DownloadInfos[j];
                    //save the track number in a backup in case if copying is true, and there's a "same title with different track number" error
                    //if that happends, the old number needs to be written back to disk because the whole naming process is not invalid
                    info.BackupLastTrackNumber = info.LastTrackNumber;
                    WriteToLog("");
                    WriteToLog("-----------------------Parsing directory " + info.Folder + "----------------------");
                    CheckMissingFolder(info.Folder);
                    //make and filter out the lists
                    List<string> files = Directory.GetFiles(info.Folder, "*", SearchOption.TopDirectoryOnly).Where(file => ValidExtensions.Contains(Path.GetExtension(file))).ToList();
                    //check to make sure there are valid audio files before proceding
                    if (files.Count == 0)
                    {
                        WriteToLog("No valid audio files in directory");
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
                    WriteToLog(string.Format("First entry, track number padding = {0}\nmax entry, track number padding = {1}\n",
                        firstEntryNumTrackNums, maxEntryNumTrackNums));
                    if (firstEntryNumTrackNums != maxEntryNumTrackNums)
                    {
                        //inform and ask
                        WriteToLog("Not equal, padding entries");
                        //use the last entry as reference point for how many paddings to do
                        for (int i = 0; i < files.Count; i++)
                        {
                            string oldFileName = Path.GetFileName(files[i]);
                            int numToPadOut = maxEntryNumTrackNums - oldFileName.Split('-')[0].Length;
                            if (numToPadOut > 0)
                            {
                                string newFileName = oldFileName.PadLeft(oldFileName.Length + numToPadOut, '0');
                                WriteToLog(string.Format("{0}\nrenamed to\n{1}", oldFileName, newFileName));
                                File.Move(Path.Combine(info.Folder, oldFileName), Path.Combine(info.Folder, newFileName));
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

                    //step 1: parse the tag info
                    for (int i = 0; i < files.Count; i++)
                    {
                        bool fileDeleted = false;
                        bool skipFile = false;
                        string fileName = files[i];
                        WriteToLog("Parsing " + fileName);

                        //create TagLib instance
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
                            if (!NoErrorPrompts)
                                Console.ReadLine();
                            Environment.Exit(-1);
                        }

                        //assign tag infos from xml properties
                        //album
                        tag.Album = info.Album;

                        //album artist
                        //https://stackoverflow.com/questions/17292142/taglib-sharp-not-editing-artist
                        if (!string.IsNullOrEmpty(info.AlbumArtist))
                        {
                            tag.AlbumArtists = null;
                            tag.AlbumArtists = new string[] { info.AlbumArtist };
                        }

                        //track number
                        //last saved number in the xml will be the last track number applied
                        //so up it first, then use it
                        tag.Track = ++info.LastTrackNumber;

                        //genre
                        tag.Genres = null;
                        tag.Genres = new string[] { info.Genre };

                        if (info.DownloadType == DownloadType.Other1)
                        {
                            //Other1 is mixes, add current year and artist as VA
                            tag.Performers = null;
                            tag.Performers = new string[] { "VA" };
                            tag.Year = (uint)DateTime.Now.Year;
                            WriteToLog("Song treated as heartAtThis mix");
                        }
                        else//youtube mix and song
                        {
                            //artist and title and year from filename
                            //get the name of the file to parse tag info to
                            string fileNameToParse = Path.GetFileNameWithoutExtension(fileName);
                            //replace "–" with "-", as well as "?-" with "-"
                            fileNameToParse = fileNameToParse.Replace('–', '-').Replace("?-", "-");
                            //split based on "--" unique separater
                            string[] splitFileName = fileNameToParse.Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries);
                            //if the count is 1, then there are no "--", implies a run in this directory already happened
                            if (splitFileName.Count() == 1)
                            {
                                WriteToLog(string.Format("File {0} seems to have already been parsed, skipping", splitFileName[0]));
                                //decrease the number, it's not a new song
                                info.LastTrackNumber--;
                                continue;
                            }

                            //split into mix parsing and song parsing
                            if (info.DownloadType == DownloadType.YoutubeMix)
                            {
                                //from youtube-dl output template:
                                //[0] = autonumber (discard), [1] = title (actual title), [2] = upload year
                                //title
                                tag.Title = splitFileName[1].Trim();

                                //year is YYYMMDD, only want YYYY
                                bool validYear = false;
                                string yearString = string.Empty;
                                yearString = splitFileName[2].Substring(0, 4).Trim();
                                //remove any extra "-" characters
                                yearString = yearString.Replace("-", string.Empty).Trim();
                                while (!validYear)
                                {
                                    try
                                    {
                                        tag.Year = uint.Parse(yearString);
                                        validYear = true;
                                    }
                                    catch
                                    {
                                        Console.Beep(1000, 500);
                                        WriteToLog("ERROR: Invalid year, please manually type");
                                        WriteToLog("Original: " + yearString);
                                        WriteToLog("Enter new (ONLY \"YYYY\")");
                                        yearString = Console.ReadLine().Trim();
                                    }
                                }

                                //artist (is VA for mixes)
                                tag.Performers = null;
                                tag.Performers = new string[] { "VA" };
                                WriteToLog("Song treated as youtube mix");
                            }
                            else//youtube song
                            {
                                //from youtube-dl output template:
                                //[0] = autonumber (discard), [1] = title (artist and title), [2] = upload year

                                //first get the artist title combo from parsed filename form youtube-dl
                                string filenameArtistTitle = splitFileName[1].Trim();
                                string[] splitArtistTitleName = null;

                                bool validArtistTitleParse = false;
                                while (!validArtistTitleParse)
                                {
                                    //labels divide the artist and title by " - "
                                    //split into artist [0] and title [1]
                                    splitArtistTitleName = filenameArtistTitle.Split(new string[] { " - " }, StringSplitOptions.None);

                                    //should be 2 entries for this to work
                                    if (splitArtistTitleName.Count() < 2)
                                    {
                                        Console.Beep(1000, 500);
                                        WriteToLog("ERROR: not enough split entries for parsing, please enter manually! (count is " + splitArtistTitleName.Count() + " )");
                                        WriteToLog("Original: " + filenameArtistTitle);
                                        WriteToLog("Enter new: (just arist - title combo)");
                                        WriteToLog("Or \"skip\" to remove song from parsing");
                                        filenameArtistTitle = Console.ReadLine().Trim();
                                        if (filenameArtistTitle.Equals("skip"))
                                        {
                                            skipFile = true;
                                            break;
                                        }
                                    }
                                    else
                                        validArtistTitleParse = true;
                                }

                                if (skipFile)
                                {
                                    filenameArtistTitle = "skipping - skipping--6969";
                                }

                                //get the artist name from split
                                tag.Performers = null;
                                tag.Performers = new string[] { splitArtistTitleName[0].Trim() };

                                //include anything after it rather than just get the last one, in case there's more split characters
                                //skip one for the artist name
                                tag.Title = string.Join(" - ", splitArtistTitleName.Skip(1)).Trim();

                                //year is YYYMMDD, only want YYYY
                                bool validYear = false;
                                string yearString = string.Empty;
                                yearString = splitFileName[2].Substring(0, 4).Trim();
                                //remove any extra "-" characters
                                yearString = yearString.Replace("-", string.Empty).Trim();
                                while (!validYear)
                                {
                                    try
                                    {
                                        tag.Year = uint.Parse(yearString);
                                        validYear = true;
                                    }
                                    catch
                                    {
                                        Console.Beep(1000, 500);
                                        WriteToLog("ERROR: Invalid year, please manually type");
                                        WriteToLog("Original: " + yearString);
                                        WriteToLog("Enter new (ONLY \"YYYY\")");
                                        yearString = Console.ReadLine().Trim();
                                    }
                                }
                                WriteToLog("Song treated as youtube song");
                            }
                        }
                        if (skipFile)
                        {
                            WriteToLog(string.Format("Skipping song {0}", tag.Title));
                            File.Delete(fileName);
                            //also delete the entry from list of files to process
                            files.Remove(fileName);
                            //also put the counter back for track numbers
                            info.LastTrackNumber--;
                            //also decremant the counter as to not skip
                            i--;
                            //also note it
                            fileDeleted = true;
                        }
                        //check to make sure song doesn't already exist (but only if it's not the first time downloading
                        else if (!info.FirstRun)
                        {
                            if (SongAlreadyExists(info.CopyPaths, tag.Title))
                            {
                                WriteToLog(string.Format("WARNING: Song {0} already exists in a copy folder, deleting the entry!", tag.Title));
                                if (!NoPrompts)
                                    Console.ReadLine();
                                File.Delete(fileName);
                                //also delete the entry from list of files to process
                                files.Remove(fileName);
                                //also put the counter back for track numbers
                                info.LastTrackNumber--;
                                //also decremant the counter as to not skip
                                i--;
                                //also note it
                                fileDeleted = true;
                            }
                        }
                        if (!fileDeleted)
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
                            if (!NoErrorPrompts)
                                Console.ReadLine();
                            Environment.Exit(-1);
                        }
                        //get the old name
                        string oldFileName = Path.GetFileNameWithoutExtension(fileName);
                        //prepare the new name
                        string newFileName = string.Empty;
                        //manual check to make sure track and title exist
                        if (file.Tag.Track == 0)
                        {
                            WriteToLog(string.Format("ERROR: Track property is missing in file {0}", fileName));
                            if (NoErrorPrompts)
                                Environment.Exit(-1);
                            while (true)
                            {
                                WriteToLog("Please input unsigned int manually!");
                                if (uint.TryParse(Console.ReadLine(), out uint result))
                                {
                                    file.Tag.Track = result;
                                    file.Save();
                                    break;
                                }
                            }
                        }
                        if (string.IsNullOrWhiteSpace(file.Tag.Title))
                        {
                            WriteToLog(string.Format("ERROR: Title property is missing in file {0}", fileName));
                            if (NoErrorPrompts)
                                Environment.Exit(-1);
                            WriteToLog("Please input manually!");
                            file.Tag.Title = Console.ReadLine();
                            file.Save();
                        }
                        switch (info.DownloadType)
                        {
                            case DownloadType.Other1:
                                //using the pre-parsed title...
                                newFileName = string.Format("{0}-{1}", file.Tag.Track.ToString(), file.Tag.Title);
                                break;
                            case DownloadType.YoutubeMix:
                                newFileName = string.Format("{0}-{1}", file.Tag.Track.ToString(), file.Tag.Title);
                                break;
                            case DownloadType.YoutubeSong:
                                if (file.Tag.Performers == null || file.Tag.Performers.Count() == 0)
                                {
                                    WriteToLog(string.Format("ERROR: Artist property is missing in file {0}", fileName));
                                    if (NoErrorPrompts)
                                        Environment.Exit(-1);
                                    WriteToLog("Please input manually!");
                                    file.Tag.Performers = null;
                                    file.Tag.Performers = new string[] { Console.ReadLine() };
                                    file.Save();
                                }
                                newFileName = string.Format("{0}-{1} - {2}", file.Tag.Track.ToString(), file.Tag.Performers[0], file.Tag.Title);
                                break;
                            default:
                                WriteToLog("Invalid downloadtype: " + info.DownloadType.ToString());
                                if (!NoErrorPrompts)
                                    Console.ReadLine();
                                continue;
                        }
                        //check for padding
                        //set padding to highest number of tracknumbers
                        //(if tracks go from 1-148, make sure filename for 1 is 001)
                        int trackPaddingLength = newFileName.Split('-')[0].Length;
                        int maxTrackNumPaddingLength = info.LastTrackNumber.ToString().Length;
                        if (trackPaddingLength < maxTrackNumPaddingLength)
                        {
                            WriteToLog("Correcting for track padding");
                            int numToPad = maxTrackNumPaddingLength - trackPaddingLength;
                            newFileName = newFileName.PadLeft(newFileName.Length + numToPad, '0');
                        }
                        //save the complete folder path
                        string completeFolderPath = Path.GetDirectoryName(fileName);
                        string completeOldPath = Path.Combine(completeFolderPath, oldFileName + Path.GetExtension(fileName));
                        string completeNewPath = Path.Combine(completeFolderPath, newFileName + Path.GetExtension(fileName));
                        WriteToLog(string.Format("Renaming {0}\n                           to {1}", oldFileName, newFileName));
                        File.Move(completeOldPath, completeNewPath);
                    }

                    //also change firstRun to false if not done already
                    if (info.FirstRun)
                    {
                        WriteToLog(string.Format("DEBUG: folder {0} firstRun is true, setting to false", info.Folder));
                        info.FirstRun = false;
                    }

                    //at the end of each folder, write the new value back to the xml file
                    string xpath = string.Format("//DownloadInfo.xml/DownloadInfos/DownloadInfo[@Folder='{0}']", info.Folder);
                    XmlNode infoNode = doc.SelectSingleNode(xpath);

                    if (infoNode == null)
                    {
                        WriteToLog("Failed to select node for saving LastTrackNumber back to folder " + info.Folder);
                        if (!NoErrorPrompts)
                            Console.ReadLine();
                        Environment.Exit(-1);
                    }

                    XmlAttribute lastTrackNumber = infoNode.Attributes["LastTrackNumber"];
                    if (lastTrackNumber == null)
                    {
                        lastTrackNumber = doc.CreateAttribute("LastTrackNumber");
                        infoNode.Attributes.Append(lastTrackNumber);
                    }
                    lastTrackNumber.Value = info.LastTrackNumber.ToString();

                    infoNode.Attributes[nameof(info.FirstRun)].Value = info.FirstRun.ToString();
                    doc.Save(DownloadInfoXml);
                    WriteToLog("Saved LastTrackNumber for folder " + info.Folder);
                }
            }
            else
                WriteToLog("ParseTags skipped");

            //copy newly parsed files to their directories
            if (!NoPrompts)
            {
                CopyFiles = GetUserResponse("CopyFiles?");
            }
            if (CopyFiles)
            {
                WriteToLog("Copy Files");
                for (int j = 0; j < DownloadInfos.Count; j++)
                {
                    DownloadInfo info = DownloadInfos[j];
                    CheckMissingFolder(info.Folder);
                    //make and filter out the lists
                    List<string> files = Directory.GetFiles(info.Folder, "*", SearchOption.TopDirectoryOnly).Where(file => ValidExtensions.Contains(Path.GetExtension(file))).ToList();
                    WriteToLog("");
                    WriteToLog("-----------------------CopyFiles for directory " + info.Folder + "----------------------");
                    if (files.Count == 0)
                    {
                        WriteToLog("no files to copy");
                        continue;
                    }
                    bool breakout = false;
                    //using copy for all then delete because you can't move across drives (easily)
                    foreach (string copypath in info.CopyPaths)
                    {
                        WriteToLog("Copying files to directory " + copypath);
                        foreach (string file in files)
                        {
                            WriteToLog(Path.GetFileName(file));
                            string newPath = Path.Combine(copypath, Path.GetFileName(file));
                            if(File.Exists(newPath))
                            {
                                WriteToLog("WARNING: The file '{0}' already exists, this could indicate multiple copyTo targets! Skipping!");
                                if (!NoPrompts)
                                    Console.ReadLine();
                            }
                            else
                                File.Copy(file, newPath);
                        }
                    }
                    if (breakout)
                        continue;

                    //now delete
                    WriteToLog("Deleting files in infos folder");
                    foreach (string file in files)
                        if (File.Exists(file))
                            File.Delete(file);
                }
            }
            else
                WriteToLog("CopyFiles skipped");

            //delete the binaries in each folder
            if (!NoPrompts)
            {
                DeleteBinaries = GetUserResponse("DeleteBinaries?");
            }
            if (DeleteBinaries)
            {
                WriteToLog("Delete Binaries");
                CheckMissingFolder(BinaryFolder);
                foreach (DownloadInfo info in DownloadInfos.Where(temp => temp.DownloadType == DownloadType.YoutubeSong || temp.DownloadType == DownloadType.YoutubeMix))
                {
                    CheckMissingFolder(info.Folder);
                    foreach (string binaryFile in BinaryFiles)
                    {
                        WriteToLog(string.Format("Deleting file {0} in folder {1} if exist", binaryFile, info.Folder));
                        string fileToCopy = Path.Combine(info.Folder, binaryFile);
                        if (File.Exists(fileToCopy))
                            File.Delete(fileToCopy);
                    }
                }
            }
            else
                WriteToLog("DeleteBinaries skipped");

            //and we're all set here
            WriteToLog("Done");
            if (!NoPrompts)
                Console.ReadLine();
            Environment.ExitCode = 0;
            Environment.Exit(0);
            return 0;
        }
    }
}
