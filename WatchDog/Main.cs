using System;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace WatchDog
{
    internal class MainClass
    {
        private static byte _countProcesses = 0;

        [STAThread]
        private static void Main()
        {
            //check if one or more instance this application are already run
            if (Environment.OSVersion.Platform.ToString().IndexOf("nix", StringComparison.InvariantCultureIgnoreCase) >= 0) //for Linux OS
            {
                using (Process process = new Process()) 
                {
                    process.StartInfo.FileName = "/bin/bash";
                    process.StartInfo.Arguments = "-c \"ps -ax | grep -v grep | grep WatchDog\"";
                    process.StartInfo.RedirectStandardOutput = true;
                    process.OutputDataReceived += GetProcesses;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    process.BeginOutputReadLine();
                    process.WaitForExit();
                }
                Thread.Sleep(500);
                if (_countProcesses > 1)
                {
                    Console.WriteLine("One as less instance of Watchdog is already run");
                    return;
                }
            }
            else   //for Windows OS
            {
                Process pr = RI();
                if (pr != null) return;
            }
            Thread.Sleep(5000);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            Application.AddMessageFilter(new Watchdog());
            Application.Run();
        }

        static void GetProcesses(object sender, DataReceivedEventArgs e)
        {
            if((!String.IsNullOrEmpty(e.Data))&&(e.Data.Contains("WatchDog"))) _countProcesses++;
        }

        public static Process RI()
        {
            Process current = Process.GetCurrentProcess();
            Process[] pr = Process.GetProcessesByName(current.ProcessName);
            foreach (Process i in pr)
            {
                if (i.Id != current.Id)
                {
                    if (Assembly.GetExecutingAssembly().Location.Replace("/", "\\") == current.MainModule.FileName)
                    {
                        return i;
                    }
                }
            }
            return null;
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("Unhandled exception: {0}", e.ExceptionObject.ToString());
        }
    }
}
