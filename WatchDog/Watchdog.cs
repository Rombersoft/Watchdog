using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Collections.Generic;

namespace WatchDog
{
    public class Watchdog : IMessageFilter
    {
        private static Process _mainProcess;
        internal static int IdProcConfig { get; set; }
        private TCP_Server _tcpServer; //you can not use it
        private Stopwatch _stopwatch;

        public Watchdog()
        {
            _stopwatch = new Stopwatch();
            _tcpServer = new TCP_Server(1983);
            _tcpServer.AppDontReply += new Action(OnAppDontReplyOrHang);
            StartWatchingApplication();
        }
            
        public void OnAppDontReplyOrHang()
        {
            Console.WriteLine("Application is not responding");
            if (_mainProcess != null)
                _mainProcess.Kill();
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            _stopwatch.Stop();
            _tcpServer.TimerWaitEvent.Stop();
            _tcpServer.TimerWaitEvent.Interval = 1800000;
            _tcpServer.TimerWaitEvent.Start();
            Console.WriteLine("Application was shutdowned");
        }

        public bool PreFilterMessage(ref Message m)
        {
            return false;
        }

        private void StartWatchingApplication()
        {
            IdProcConfig = 0;
            try
            {
                _mainProcess.Kill();
            }
            catch(Exception){}
            Thread.Sleep(1000);
            Console.WriteLine("Start application...");
            try
            {
                _mainProcess = new Process();
                _mainProcess.Exited += new EventHandler(Process_Exited);
                _mainProcess.EnableRaisingEvents = true;
                _mainProcess.StartInfo.FileName = "myApp.exe";
                _mainProcess.StartInfo.Arguments = "forward";
                _mainProcess.StartInfo.UseShellExecute = false;
                _mainProcess.StartInfo.CreateNoWindow = true;
                _mainProcess.StartInfo.RedirectStandardError = true;
                _mainProcess.StartInfo.RedirectStandardOutput = true;
                _mainProcess.OutputDataReceived += TerminalUI_OutputDataReceived;
                _mainProcess.ErrorDataReceived += TerminalUI_ErrorDataReceived;
                _mainProcess.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
                _mainProcess.Start();
                _stopwatch.Reset();
                _stopwatch.Start();
            }
            catch(Exception e)
            {
                Console.WriteLine("Unsuccessful start: {0}", e.Message);
            }
        }

        void TerminalUI_OutputDataReceived (object sender, DataReceivedEventArgs e)
        {
            //TODO: Here you can get some output
        }

        void TerminalUI_ErrorDataReceived (object sender, DataReceivedEventArgs e)
        {
            if(!string.IsNullOrEmpty(e.Data))
                Console.WriteLine("Wachdog <Error of running application> -> {0}", e.Data);
        }
    }
}
