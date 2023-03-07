using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace SimpleAvFinder
{
    internal class Program
    {
        public static Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
        private static Dictionary<string, string> procNameId = new Dictionary<string, string>();
        private static Dictionary<string, string> commandAgrs = new Dictionary<string, string>();
        private static string taskFile = "tasklist.txt";
        private static string configFile = "av.config";
        private static string pattern = @"(.+?) +(\d+) (.+?) +(\d+) +(\d+.* K).*";

        static void parseCommand(string[] args) {
            for (var i = 0; i < args.Length; i++) {

                if (i == args.Length) {
                    return;
                }
                try
                {
                    commandAgrs[args[i]] = args[i+1];
                }
                catch (Exception e) {
                    throw new Exception("Parse args failed.");
                }
                i++;
            }
        }

        static void Main(string[] args)
        {


            if (args.Length < 0) {
                help();
                System.Environment.Exit(0);
            }
            try
            {
                parseCommand(args);

                if (commandAgrs.ContainsKey("-h")) {
                    help();
                    System.Environment.Exit(0);
                }

                if (commandAgrs.ContainsKey("-c")) {
                    configFile = commandAgrs["-c"];
                }

                if (commandAgrs.ContainsKey("-t")) {
                    taskFile = commandAgrs["-t"];
                }

                initConfig();
                if (commandAgrs.ContainsKey("-type")) {
                    if (commandAgrs["-type"].ToLower() == "file")
                    {
                        TaskFindAVFromFile(taskFile);
                    }
                    else if (commandAgrs["-type"].ToLower() == "wmi")
                    {
                        WMIFindAv();

                    }
                    else {
                        TaskFindAV();
                    }
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                help();
                System.Environment.Exit(1);
            }



        }

        static void TaskFindAVFromFile(string taskFile) {
            parseTasklistFile(taskFile);
            foreach (var p in procNameId.Keys)
            {

                if (keyValuePairs.ContainsKey(p))
                {
                    Console.WriteLine(String.Format("发现 {1}! 进程名： {0} ", p, keyValuePairs[p]));
                }
            }
        }

        static void parseTasklistFile(string taskFile) {

            var allline = File.ReadAllText(taskFile);
            parseTasklist(allline);
        }

        static void parseTasklist(string input) {
            var lines = input.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines) {
                Match m = Regex.Match(line, pattern);
                var procName = m.Groups[1].Value;
                var procId = m.Groups[2].Value;
                procNameId[procName] = procId;
            }

        }

        static void help() {
            Console.WriteLine("help -h");
            Console.WriteLine("SimpleAvFinder.exe -type wmi");
            Console.WriteLine("SimpleAvFinder.exe -type tasklist");
            Console.WriteLine("SimpleAvFinder.exe -type wmi -c ./av.config");
            Console.WriteLine("SimpleAvFinder.exe -type file -t ./tasklist.txt");
            Console.WriteLine("SimpleAvFinder.exe -type tasklist -c ./av.config");
            Console.WriteLine("SimpleAvFinder.exe -type file -t ./tasklist.txt -c ./av.config");
        }

        static void TaskFindAV() {
            Process proc = new Process();
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.FileName = "cmd.exe";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.Start();
            proc.StandardInput.WriteLine("tasklist");
            proc.StandardInput.WriteLine("exit");
            var result = proc.StandardOutput.ReadToEnd();

            parseTasklist(result);


            foreach (var p in procNameId.Keys) {

                if (keyValuePairs.ContainsKey(p)) {
                    Console.WriteLine(String.Format("发现 {1}! 进程名： {0}", p, keyValuePairs[p]));

                }
            }
        }

        static void WMIFindAv() {
            using (var searcher = new ManagementObjectSearcher("Select * from Win32_Process"))
            {

                var searchResult = searcher.Get();
                foreach (var item in searchResult)
                {
                    string procName = item["name"].ToString();
                    if (keyValuePairs.ContainsKey(procName))
                    {

                        Console.WriteLine(String.Format("发现 {1}! 进程名： {0}", procName, keyValuePairs[procName]));
                          
                    }
                }
            }
        }

        static void initConfig()
        {
            string currentPath = System.IO.Directory.GetCurrentDirectory();
            string file =  Path.Combine(currentPath, configFile);
            var lines = File.ReadAllLines(file);

            foreach (var line in lines) {
                var _tmp = line.Split(':');
                keyValuePairs[_tmp[0]] = _tmp[1];
            }
            
        }
    }
}
