using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Net.NetworkInformation;
using System.Timers;
using System.Windows.Forms;
using System.Threading;
using Terminal.Service.Loging;
using Timer = System.Timers.Timer;

namespace WatchDog
{
    public class Windows : IMessageFilter
    {
        private static Process _mainProcess;
        private Timer _timer;
        private Thread _thread;
        private int _selfId;
        private TCP_Server _tcpServer;
        private Stopwatch _stopwatch;

        public Windows()
        {
            MainClass.HideTaskbar();
            MainClass.HideDesktop();
            _stopwatch = new Stopwatch();
            _tcpServer = new TCP_Server();
            _tcpServer.AppDontReply += new Action(_tcpServer_AppDontReply);
            _selfId = Process.GetCurrentProcess().Id;
            _timer = new Timer(5000);
            _timer.Elapsed += new ElapsedEventHandler(Tick);
            /*while (true)
                {
                    Thread.Sleep(30000);
                    if (Ping()) break;
                }*/
            Thread.Sleep(10000);
            StartPayment();
        }

        void _tcpServer_AppDontReply()
        {
            Process.Start("cmd", "/r shutdown -t 20 -r -f");
        }

        private void process_Exited(object sender, EventArgs e)
        {
            _stopwatch.Stop();
            long seconds = _stopwatch.ElapsedMilliseconds / 1000;
            Logs.Main.Debug("Watchdog получил событие о закрытии платежной программы через {0} секунд после старта", seconds);
            _tcpServer.TimerWaitEvent.Stop();
            _tcpServer.TimerWaitEvent.Interval = 1800000;
            _tcpServer.TimerWaitEvent.Start();
            if (seconds > 3) FindConfigProcess();
        }

        public bool PreFilterMessage(ref Message m)
        {
            return false;
        }

        private void Tick(object Sender, ElapsedEventArgs args)
        {
            if((_thread != null)&&(_thread.IsAlive)) _thread.Abort();
            _thread = new Thread(new ThreadStart(FindConfigProcess));
            _thread.IsBackground = true;
            _thread.Start();
            _thread.Join(5000);
            if(_thread.IsAlive)
            {
                _thread.Abort();
            }
        }

        private void StartPayment()
        {
            try
            {
                _mainProcess.Kill();
            }
            catch (Exception) { }
            try
            {
                _mainProcess = new Process();
                _mainProcess.Exited += new EventHandler(process_Exited);
                _mainProcess.EnableRaisingEvents = true;
                _mainProcess.StartInfo.FileName = "TerminalUI.exe";
                _mainProcess.StartInfo.Arguments = _selfId.ToString();
                _mainProcess.StartInfo.UseShellExecute = false;
                _mainProcess.StartInfo.CreateNoWindow = true;
                _mainProcess.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
                _mainProcess.Start();
                _stopwatch.Reset();
                _stopwatch.Start();
            }
            catch(Exception e)
            {
                Logs.AppError.Warning("Старт платежного ПО: " + e.Message);
            }
            MainClass.HideTaskbar();
            MainClass.HideDesktop();
        }

        private void FindConfigProcess()
        {
            Process[] processes = Process.GetProcessesByName("Terminal.Config");
            Logs.Main.Debug("Найдено процессов конфигуратора {0}", processes.Length);
            if (processes.Length == 0)
            {
                if (_timer.Enabled) _timer.Stop();
                Process[] proc = Process.GetProcessesByName("TerminalUI");
                if (proc.Length > 0)
                {
                    Thread.Sleep(5000);
                    try
                    {
                        _mainProcess.Kill();
                    }
                    catch(Exception){}
                    proc = Process.GetProcessesByName("TerminalUI");
                    foreach(Process pr in proc)
                    {
                        pr.Kill();
                    }
                }
                StartPayment();
            }
            else
            {
                Thread.Sleep(5000);
                try
                {
                    _mainProcess.Kill();
                }
                catch(Exception){}
                if (!_timer.Enabled) _timer.Start();
                MainClass.ShowDesktop();
                MainClass.ShowTaskbar();
            }  
        }

        private bool Ping()
        {
            Ping ping = new Ping();
            PingReply reply;
            string[] hostNames = new[] { "176.9.11.100", "77.120.97.36", "77.120.119.159" };
            foreach (string host in hostNames)
            {
                try
                {
                    reply = ping.Send(host, 10000, new byte[1] { 27 });
                    if (reply.Status == IPStatus.Success)
                    {
                        return true;
                    }
                }
                catch (Exception) { }
            }
            return false;
        }
    }
}
