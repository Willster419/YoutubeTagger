using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace YoutubeTagger
{
    partial class Program
    {
        //attaches the assembly resolver event
        private static void AttachAssemblyResolver()
        {
            //hook up assembly resolver
            //https://stackoverflow.com/a/25990979/3128017
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        //method executed on the assembly resolver event (application can't find a dynamic assembly)
        //gets the name of the assembly, and looks for that name with the dll extension inside
        //the application, where it is embedded internally. this allows to ship the application
        //as a single executable without the need to include dlls as separate files
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string dllName = new AssemblyName(args.Name).Name + ".dll";
            Assembly assem = Assembly.GetExecutingAssembly();
            string resourceName = assem.GetManifestResourceNames().FirstOrDefault(rn => rn.EndsWith(dllName));
            using (Stream stream = assem.GetManifestResourceStream(resourceName))
            {
                byte[] assemblyData = new byte[stream.Length];
                stream.Read(assemblyData, 0, assemblyData.Length);
                return Assembly.Load(assemblyData);
            }
        }

        //get an embedded resource by name if exists, or null if it does not
        private static byte[] GetEmbeddedResource(string resourceName)
        {
            Assembly assem = Assembly.GetExecutingAssembly();
            //https://stackoverflow.com/questions/1024559/when-to-use-first-and-when-to-use-firstordefault-with-linq
            string resourceNameFound = assem.GetManifestResourceNames().FirstOrDefault(rn => rn.Contains(resourceName));
            if (!string.IsNullOrWhiteSpace(resourceNameFound))
            {
                using (Stream stream = assem.GetManifestResourceStream(resourceNameFound))
                {
                    byte[] assemblyData = new byte[stream.Length];
                    stream.Read(assemblyData, 0, assemblyData.Length);
                    return assemblyData;
                }
            }
            else
                return null;
        }

        //get an embedded resource by name if exists, or null if it does not
        private static Stream GetEmbeddedResourceStream(string resourceName)
        {
            Assembly assem = Assembly.GetExecutingAssembly();
            string resourceNameFound = assem.GetManifestResourceNames().FirstOrDefault(rn => rn.Contains(resourceName));
            if (!string.IsNullOrWhiteSpace(resourceNameFound))
            {
                return assem.GetManifestResourceStream(resourceNameFound);
            }
            else
                return null;
        }

        //write to the console and the logfile
        private static void WriteToLog(string logMessage)
        {
            Console.WriteLine(logMessage);
            File.AppendAllText(Logfile, string.Format("{0}:   {1}{2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), logMessage, Environment.NewLine));
        }

        //get the response to a user question
        private static bool GetUserResponse(string question)
        {
            //ask user the question
            WriteToLog(question);
            while (true)
            {
                if (bool.TryParse(Console.ReadLine(), out bool res))
                {
                    return res;
                }
                else
                {
                    WriteToLog("Response must be bool parse-able (i.e. true or false)");
                }
            }
        }

        //check if a song with a same title exists based on what was just parsed
        private static bool SongAlreadyExists(string[] copyFoldersToCheck, string titleOfSongToCheck)
        {
            bool doesSongAlreadyExist = false;
            foreach (string copyFolderToCheck in copyFoldersToCheck)
            {
                if (!Directory.Exists(copyFolderToCheck))
                    continue;
                //get a lsit of files in that copy folder (with media extension)
                foreach (string fileInCopyFolder in Directory.GetFiles(copyFolderToCheck).Where(filename => ValidExtensions.Contains(Path.GetExtension(filename))))
                {
                    TagLib.Tag tag = null;
                    TagLib.File file = null;
                    //get the taglist entry for that file
                    try
                    {
                        //https://stackoverflow.com/questions/40826094/how-do-i-use-taglib-sharp
                        file = TagLib.File.Create(fileInCopyFolder);
                        tag = file.Tag;
                    }
                    catch (Exception ex)
                    {
                        WriteToLog(ex.ToString());
                        if (!NoErrorPrompts)
                            Console.ReadLine();
                        Environment.Exit(-1);
                    }
                    if (tag.Title.Equals(titleOfSongToCheck))
                    {
                        doesSongAlreadyExist = true;
                        break;
                    }
                    file.Dispose();
                }
                if (doesSongAlreadyExist)
                    break;
            }
            return doesSongAlreadyExist;
        }

        //check if a folder path is missing. create and continue is true
        private static void CheckMissingFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
        }

        //check if a file is missing. error and exit if true
        private static void CheckMissingFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                WriteToLog(string.Format("ERROR: missing file {0} for path {1}", Path.GetFileName(filePath), filePath));
                if (!NoErrorPrompts)
                    Console.ReadLine();
                //https://stackoverflow.com/questions/10286056/what-is-the-command-to-exit-a-console-application-in-c
                Environment.Exit(-1);
            }
        }

        //kill all running youtube download processes in list, then dispose of all of them
        private static void KillProcesses(List<Process> processes)
        {
            foreach (Process proc in processes)
            {
                try
                {
                    proc.Kill();
                }
                catch
                {
                    //WriteToLog("process " + proc.Id + "not stopped");
                }
                try
                {
                    proc.Dispose();
                }
                catch
                {
                    //WriteToLog("process " + proc.Id + "not stopped");
                }
            }
        }

        //update text if an xml element is found
        private static string UpdateTextFromXmlEntry(string stringName, string defaultEntry, XmlDocument doc, string xpath)
        {
            XmlNode node = doc.SelectSingleNode(xpath);
            if (node == null)
            {
                WriteToLog(string.Format("The xml node '{0}' was not found, using default: {1}", stringName, defaultEntry));
                return defaultEntry;
            }
            string result = node.InnerText.Trim();
            if (result != defaultEntry)
                return node.InnerText.Trim();
            else
                return defaultEntry;
        }

        //parse the xml file
        private static void ParseDownloadInfoXml()
        {
            //init tag parsing, load xml data
            //check to make sure download info xml file is present
            if (!File.Exists(DownloadInfoXml))
            {
                WriteToLog(string.Format("{0} is missing, application cannot continue", DownloadInfoXml));
                if (!NoErrorPrompts)
                    Console.ReadLine();
                Environment.Exit(-1);
            }
            WriteToLog("---------------------------APPLICATION START---------------------------");
            WriteToLog("Loading Xml document");
            doc = new XmlDocument();
            try
            {
                doc.Load(DownloadInfoXml);
            }
            catch (XmlException ex)
            {
                WriteToLog("Failed to load Xml document");
                WriteToLog(ex.ToString());
                if (!NoErrorPrompts)
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
                ForceDownloadYoutubeDl = bool.Parse(doc.SelectSingleNode("/DownloadInfo.xml/Settings/ForceDownloadYoutubeDl").InnerText.Trim());
                CopyBinaries = bool.Parse(doc.SelectSingleNode("/DownloadInfo.xml/Settings/CopyBinaries").InnerText.Trim());
                RunScripts = bool.Parse(doc.SelectSingleNode("/DownloadInfo.xml/Settings/RunScripts").InnerText.Trim());
                SaveNewDate = bool.Parse(doc.SelectSingleNode("/DownloadInfo.xml/Settings/SaveNewDate").InnerText.Trim());
                ParseTags = bool.Parse(doc.SelectSingleNode("/DownloadInfo.xml/Settings/ParseTags").InnerText.Trim());
                CopyFiles = bool.Parse(doc.SelectSingleNode("/DownloadInfo.xml/Settings/CopyFiles").InnerText.Trim());
                DeleteBinaries = bool.Parse(doc.SelectSingleNode("/DownloadInfo.xml/Settings/DeleteBinaries").InnerText.Trim());
                DeleteOutputLogs = bool.Parse(doc.SelectSingleNode("/DownloadInfo.xml/Settings/DeleteOutputLogs").InnerText.Trim());

                //and some default command line settings
                DefaultCommandLine = doc.SelectSingleNode("/DownloadInfo.xml/CommandLine/Default").InnerText.Trim();
                DateAfterCommandLine = doc.SelectSingleNode("/DownloadInfo.xml/CommandLine/DateAfter").InnerText.Trim();
                YoutubeMixDurationCommandLine = doc.SelectSingleNode("/DownloadInfo.xml/CommandLine/YoutubeMixDuration").InnerText.Trim();
                YoutubeSongDurationCommandLine = doc.SelectSingleNode("/DownloadInfo.xml/CommandLine/YoutubeSongDuration").InnerText.Trim();
                YoutubeDlUrl = UpdateTextFromXmlEntry(nameof(YoutubeDlUrl), YoutubeDlUrl, doc, "/DownloadInfo.xml/CommandLine/YoutubeDlUrl");

                //for each xml element "DownloadInfo" in element "DownloadInfo.xml"
                int processingIndex = 0;
                foreach (XmlNode infosNode in doc.SelectNodes("//DownloadInfo.xml/DownloadInfos/DownloadInfo"))
                {
                    WriteToLog(string.Format("Processing DownloadInfo {0}", processingIndex));
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
                        catch (Exception ex)
                        {
                            WriteToLog(string.Format("ERROR: Failed to parse xml attribute '{0}' to data type for DownloadInfo {1}", field.Name, temp.Folder));
                            WriteToLog(ex.ToString());
                            if (!NoErrorPrompts)
                                Console.ReadLine();
                            Environment.Exit(-1);
                        }
                    }

                    //parse DownloadUrl
                    temp.DownloadURL = infosNode[nameof(temp.DownloadURL)].InnerText.Trim();
                    if (string.IsNullOrWhiteSpace(temp.DownloadURL) && (temp.DownloadType == DownloadType.YoutubeMix || temp.DownloadType == DownloadType.YoutubeSong))
                    {
                        WriteToLog(string.Format("ERROR: DownloadURL for '{0}' is blank", temp.Folder));
                        if (!NoErrorPrompts)
                            Console.ReadLine();
                        Environment.Exit(-1);
                    }

                    //parse CustomYoutubeDlCommands
                    temp.DownloadURL = infosNode[nameof(temp.CustomYoutubedlCommands)].InnerText.Trim();

                    //parse CopyPaths
                    //get the list of paths that the parsed music files should be copied to
                    XmlNodeList pathsList = (infosNode as XmlElement).SelectNodes("CopyPath");
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
                                    WriteToLog(string.Format("ERROR: The folder '{0}' declared in the xml does not exist", paths.InnerText));
                                    if (!NoErrorPrompts)
                                        Console.ReadLine();
                                    Environment.Exit(-1);
                                }
                            }

                            //check to make sure there's no duplicate CopyPath entries for this DownloadInfo
                            if(temp.CopyPaths.Contains(paths.InnerText))
                            {
                                WriteToLog("ERROR: Copy path already parsed, remove duplicate");
                                if (!NoErrorPrompts)
                                    Console.ReadLine();
                                Environment.Exit(-1);
                            }
                            temp.CopyPaths[i++] = paths.InnerText;
                        }
                    }
                    else
                    {
                        WriteToLog("ERROR: Paths count is 0 for downloadFolder of folder attribute " + temp.Folder);
                        if (!NoErrorPrompts)
                            Console.ReadLine();
                        Environment.Exit(-1);
                    }

                    //if it's the first time running, then we can set the last track count to 0 (if not already)
                    if (temp.FirstRun)
                        temp.LastTrackNumber = 0;

                    //and finally add it to the list
                    DownloadInfos.Add(temp);
                    processingIndex++;
                }
            }
            catch (Exception ex)
            {
                WriteToLog(ex.ToString());
                if (!NoErrorPrompts)
                    Console.ReadLine();
                Environment.Exit(-1);
            }
        }
    }
}
