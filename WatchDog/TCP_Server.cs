using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

namespace WatchDog
{
    public class TCP_Server
    {
        internal event Action AppDontReply;
        internal Timer TimerWaitEvent { get; private set; }
        private int _port;
        public TCP_Server(int port)
        {
            _port = port;
            TimerWaitEvent = new Timer(1800000);
            TimerWaitEvent.Elapsed += new ElapsedEventHandler(_timer_Elapsed);
            new Thread(Run){IsBackground = true}.Start();
        }

        void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (AppDontReply != null) AppDontReply();
        }

        private void Run()
        {
            TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), _port);
            try
            {
                listener.Start();
                TimerWaitEvent.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error of listen port {0}: {1}", _port, ex.Message);
                return;
            }
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                byte[] buffer = new byte[100];
                NetworkStream stream = null;
                try
                {
                    client.SendTimeout = 2000;
                    client.ReceiveTimeout = 2000;
                    stream = client.GetStream();
                    stream.ReadTimeout = 5000;
                    stream.WriteTimeout = 30000;
                    int redBytes = 0;
                    redBytes = stream.Read(buffer, 0, buffer.Length);
                    if(redBytes == 1)
                    {
                        stream.Write(new byte[1]{27}, 0, 1);
                        TimerWaitEvent.Stop();
                        TimerWaitEvent.Start();
                    }
                    else //insuranse application when it sends signal that it will close
                    {
                        StringBuilder builder = new StringBuilder();
                        builder.Append(Encoding.ASCII.GetChars(buffer,0,redBytes));
                        string[] data = builder.ToString().Replace("\n", "").Replace("\r","").Split(' ');
                        byte[] buf;
                        if(data[0] == "some_signal")
                        {
                                TimerWaitEvent.Stop();  //if after 30 seconds application will not close we it kill forcely
                                TimerWaitEvent.Interval = 30000;
                                TimerWaitEvent.Start();
                                buf = new byte[1]{27};
                                stream.Write(buf, 0, 1);
                        }
                        else 
                        {
                            buf = Encoding.ASCII.GetBytes("Unknown package");
                            stream.Write(buf, 0, 1);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error of listen port {0}: {1}", _port, e.Message);
                    NetworkStream stream1 = client.GetStream();
                    stream1.Write(buffer, 0, buffer.Length);
                }
                finally
                {
                    if (stream != null) stream.Dispose();
                    client.Close();
                }
            }
        }
    }
}
