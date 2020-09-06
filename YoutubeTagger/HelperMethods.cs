using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace YoutubeTagger
{
    partial class Program
    {
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
    }
}
