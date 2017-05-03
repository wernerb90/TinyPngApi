using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace TinyPngApi
{
    class Program
    {
        static bool recursive = true;
        static string API_KEY = "";
        static int MinSize = 30 * 1000;
        static long SavedBytes = 0;

        static List<string> DB = new List<string>();
        static string DBFile = "";
        static string LogFile = "";

        static int LogLevelDisplay = 1; // 0 = none, 1 = info, 2 = debug

        static void LogLine(string str)
        {
            if (LogLevelDisplay > 0) Console.WriteLine(DateTime.Now.ToString("HH-mm-ss") + " | " + str);
            string line = str + Environment.NewLine;

            if (!string.IsNullOrEmpty(LogFile))
            {
                if (!File.Exists(LogFile))
                    File.WriteAllText(LogFile, "", Encoding.UTF8);

                //File.AppendAllText(LogFile, line);
                string content = File.ReadAllText(LogFile);
                File.WriteAllText(LogFile, content + line, Encoding.UTF8);

            }
        }

        static List<string> GetDB()
        {
            return File.ReadAllText(DBFile).Split(Environment.NewLine.ToCharArray()).ToList();
        }
        static void SaveDB()
        {
            File.WriteAllText(DBFile, String.Join(Environment.NewLine, DB), Encoding.UTF8);
        }

        static void Main(string[] args)
        {
            LogFile = Directory.GetCurrentDirectory() + "\\TinyPngApi_log_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt";
            Console.WriteLine(LogFile);

            LogLine(DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            LogLine("--- ---");
            if (LogLevelDisplay > 0) LogLine("Starting TinyPngApi");
            if (LogLevelDisplay > 0) LogLine("--- --- --- --- ---");

            DBFile = Directory.GetCurrentDirectory() + "\\TinyPngApi.dat";
            if (LogLevelDisplay > 1) LogLine("DB File: " + DBFile);

            if (File.Exists(DBFile))
                DB = GetDB();

            if (LogLevelDisplay > 1) LogLine("DB Size: " + DB.Count);
            
            string pagePath = "";

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-target")
                {
                    pagePath = args[i + 1];
                }
                if (args[i] == "-key")
                {
                    API_KEY = args[i + 1];
                }
            }

            if (string.IsNullOrEmpty(pagePath))
            {
                Console.WriteLine("Enter directory path and press ENTER:");
                do
                {
                    pagePath = Console.ReadLine();
                } while (pagePath == "");
                if (LogLevelDisplay > 0) LogLine("INPUT: " + pagePath);
            }

            if (string.IsNullOrEmpty(pagePath) || pagePath == "current")
                pagePath = Directory.GetCurrentDirectory();


            if (API_KEY == "")
            {
                string keyPath = Directory.GetCurrentDirectory() + "\\TinyPngApi.key";

                if (File.Exists(keyPath))
                    API_KEY = File.ReadAllText(keyPath);
                
                if (API_KEY == "") {

                    Console.WriteLine("Enter API KEY and press ENTER:");
                    do
                    {
                        API_KEY = Console.ReadLine();
                    } while (pagePath == "");
                }
            }

            if (LogLevelDisplay > 0) LogLine("Path: " + pagePath);
            if (LogLevelDisplay > 1) LogLine("Key: " + (!string.IsNullOrEmpty(API_KEY) ? "exists" : "not found"));

            string[] files = buildFileList(pagePath);
            if (LogLevelDisplay > 0) LogLine("Found: " + files.ToList().Count + " files.");
            
            for (int i = 0; i < files.Length; i++)
            {
                if (LogLevelDisplay > 0) LogLine("==== ==== ==== ==== ==== ==== ====");
                if (LogLevelDisplay > 0) LogLine("Starting: " + files[i].Replace(Directory.GetCurrentDirectory(), ""));

                string[] parts = files[i].Split('.');
                string ext = parts[parts.Length - 1].ToLower().Trim();
                ImageFormat format = ImageFormat.Jpeg;

                switch (ext)
                {
                    case "png":
                        format = ImageFormat.Png;
                        break;
                    case "gif":
                        format = ImageFormat.Gif;
                        break;
                }

                if (File.Exists(files[i]) && !DB.Contains(files[i]))
                {
                    long BeforeLength = 0;

                    using (FileStream file = File.OpenRead(files[i]))
                    {
                        BeforeLength = file.Length;
                        if (LogLevelDisplay > 0) LogLine("Filesize BEFORE: " + BeforeLength.ToString());
                    }

                    if (BeforeLength > MinSize)
                    {

                        using (WebClient wc = new WebClient())
                        {
                            string auth = Convert.ToBase64String(Encoding.UTF8.GetBytes("api:" + API_KEY));
                            wc.Headers.Add(HttpRequestHeader.Authorization, "Basic " + auth);

                            try
                            {
                                wc.UploadData("https://api.tinypng.com/shrink", File.ReadAllBytes(files[i]));
                                /* Compression was successful, retrieve output from Location header. */
                                wc.DownloadFile(wc.ResponseHeaders["Location"], files[i]);

                                if (LogLevelDisplay > 1) LogLine("Compression and Download SUCCESS");

                                DB.Add(files[i]);
                                SaveDB();

                                using (FileStream f = File.OpenRead(files[i]))
                                {
                                    if (LogLevelDisplay > 0) LogLine("Filesize AFTER: " + f.Length.ToString());
                                    SavedBytes += (BeforeLength - f.Length);
                                    if (LogLevelDisplay > 0) LogLine("SAVED so far: " + SavedBytes.ToString());
                                }
                            }
                            catch (WebException ex)
                            {
                                /* Something went wrong! You can parse the JSON body for details. */
                                LogLine("wc.UploadData failed. - " + ex.Message);
                            }
                        }
                        
                    }
                    else
                    {
                        if (LogLevelDisplay > 1) LogLine("---- Skipped, smaller than " + MinSize.ToString() + " bytes.");
                    }
                }
            }

            SaveDB();

            if (LogLevelDisplay > 0) LogLine("-");
            if (LogLevelDisplay > 1) LogLine("--");
            if (LogLevelDisplay > 0) LogLine("Saved " + SavedBytes.ToString() + " bytes.");
            if (LogLevelDisplay > 1) LogLine("--");
            if (LogLevelDisplay > 0) LogLine("-");
            if (LogLevelDisplay > 0) LogLine("Press any key to continue!");
            Console.ReadKey();
            Environment.Exit(0);
        }

        private static string[] buildFileList(string pagePath)
        {
            List<string> result = new List<string>();
            result.AddRange(Directory.GetFiles(pagePath, "*.jpg"));
            result.AddRange(Directory.GetFiles(pagePath, "*.png"));
            if (recursive)
            {
                string[] subfolders = Directory.GetDirectories(pagePath);
                foreach (string subfolder in subfolders)
                {
                    result.AddRange(buildFileList(subfolder));
                }
            }
            return result.ToArray<string>();
        }

        private static void WriteHelp()
        {
            LogLine("");
            LogLine("-?                       Show this message");
            LogLine("-target                  Directory with png/jpg files(this argument is mandatory)");
            LogLine("-key                     The API key to use(this argument is mandatory)");
            LogLine("-recursive               If present all subdirectories will be searched for png and jpg files");
            Console.ReadKey();

        }
    
    }
}
