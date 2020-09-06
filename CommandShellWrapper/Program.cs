using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace CommandShellWrapper
{
    class Program
    {
        public static readonly string ApplicationStartupPath = AppDomain.CurrentDomain.BaseDirectory;

        private const string LogFilename = "Output.Log";

        private const string LogStart = @"/----------------------------------------------------------------------\";

        private const string LogStop = @"\----------------------------------------------------------------------/";

        private static FileStream FileStream = null;

        private static void WriteToLog(string message, params object[] args)
        {
            WriteToLog(string.Format(message, args));
        }

        private static void WriteToLog(string message)
        {
            Console.Write(message);
            FileStream.Write(Encoding.UTF8.GetBytes(message), 0, Encoding.UTF8.GetByteCount(message));
            FileStream.Flush();
        }

        private static void WriteLineToLog(string message, params object[] args)
        {
            WriteLineToLog(string.Format(message, args));
        }

        private static void WriteLineToLog(string message)
        {
            message = message + Environment.NewLine;
            Console.Write(message);
            FileStream.Write(Encoding.UTF8.GetBytes(message), 0, Encoding.UTF8.GetByteCount(message));
            FileStream.Flush();
        }

        static void Main(string[] args)
        {
            if (File.Exists(LogFilename))
                File.Delete(LogFilename);

            //create log file filestream
            try
            {
                FileStream = new FileStream(LogFilename, FileMode.Append, FileAccess.Write);
            }
            catch (Exception ex)
            {
                WriteLineToLog("Failed to create log file");
                WriteLineToLog(ex.ToString());
                Environment.Exit(69);
                return;
            }

            WriteLineToLog(LogStart);
            WriteLineToLog("Command line arguments : {0}", string.Join(" ",args));

            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WorkingDirectory = ApplicationStartupPath,
                    FileName = args[0],
                    CreateNoWindow = true,
                    Arguments = string.Join(" ", args.Skip(1))
                }
            };

            process.OutputDataReceived += Process_OutputDataReceived;
            process.ErrorDataReceived += Process_ErrorDataReceived;

            try
            {
                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                process.WaitForExit();
                WriteLineToLog("Process exited with code {0}", process.ExitCode);
                WriteLineToLog(LogStop);
                Environment.Exit(process.ExitCode);
            }
            catch (Exception ex)
            {
                WriteLineToLog("An error has occurred trying to run the process");
                WriteLineToLog(ex.ToString());
                Environment.Exit(420);
            }
        }

        private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if(!string.IsNullOrEmpty(e.Data))
                WriteLineToLog(e.Data);
        }

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
                WriteLineToLog(e.Data);
        }
    }
}
