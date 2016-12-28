using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Threading;


namespace DMXRemoteServer
{
    public class DmxInterface
    {
        [DllImport("K8062D.dll")]
        public static extern void StartDevice();

        [DllImport("K8062D.dll")]
        public static extern void SetData(int channel, int data);

        [DllImport("K8062D.dll")]
        public static extern void StopDevice();

        [DllImport("K8062D.dll")]
        public static extern void SetChannelCount(int count);
    }


    class Program
    {
        private static TcpListener Listener;
        private static ArrayList Threads = new ArrayList();
        public static Boolean ServerStopped;

        public static void Main()
        {
            DmxInterface.StartDevice();
            DmxInterface.SetChannelCount(100);
            //Initialize Listener, start Listener
            int port = 4711;
            Listener = new TcpListener(IPAddress.Any, port);
            Listener.Start();
            Console.WriteLine("Creating new listener on Port 4711");
            //Initialize MainServerThread, start MainServerThread
            Console.WriteLine("Starting MainThread");
            Thread mainThread = new Thread(Run);
            mainThread.Start();
            Console.WriteLine("Done");
            while (!ServerStopped)
            {
                Console.WriteLine("Write 'stop' to stop Server...");
                var cmd = Console.ReadLine();
                if (cmd != null && cmd.ToLower().Equals("stop"))
                {
                    Console.WriteLine("stopping server");
                    ServerStopped = true;
                }
                else
                {
                    Console.WriteLine("unknow command: " + cmd);
                }
            }
            EndThreads(mainThread);
        }

        public static void EndThreads(Thread mainThread)
        {
            //stopping MainThread
            mainThread.Abort();
            Console.WriteLine("MainThread stopped");
            //stopping all Threads
            for (IEnumerator e = Threads.GetEnumerator(); e.MoveNext();)
            {
                //Getting next ServerThread
                Console.WriteLine("Stopping the ServerThreads");
                ServerThread serverThread = (ServerThread)e.Current;
                //Stop it
                serverThread.Stop = true;
                while (serverThread.Running)
                {
                    Thread.Sleep(1000);
                }
            }
            //Stopping Listener
            Listener.Stop();
            Console.WriteLine("listener stopped");
            Thread.Sleep(5000);
            Console.Clear();
        }
        //opening a new ServerThread
        public static void Run()
        {
            while (true)
            {
                Console.WriteLine("Listener waiting for connection wish");
                //waiting for incoming connection wish
                TcpClient client = Listener.AcceptTcpClient();
                Console.WriteLine("Connected...");
                Threads.Add(new ServerThread(client));
            }
        }
    }

    class ServerThread
    {
        public bool Stop;
        public bool Running;
        private TcpClient client;

        public ServerThread(TcpClient client)
        {
            this.client = client;
            //starting new thread with run() function
            Console.WriteLine("Starting new ServerThread");
            new Thread(Run).Start();
        }
        //threaded function
        public void Run()
        {
            Stream stream = null;
            Boolean gotstream = false;
            Console.WriteLine("Reading incoming Data Stream");
            Running = true;
            while (!gotstream)
            {
                //making sure stream is not null to avoid exeption
                if (client != null && client.GetStream() != null)
                {
                    //getting stream, setting flag true
                    stream = client.GetStream();
                    gotstream = true;
                }
            }
            byte[] incomingValues = new byte[] {255, 255};
            Boolean gotData = false;
            while (client.Connected)
            {
                    try
                    {
                        //reading Byte Array
                        stream.Read(incomingValues, 0, incomingValues.Length);
                        gotData = true;
                    }
                    catch (Exception e)
                    {
                        //catching exeptions
                        Console.WriteLine("Ooops, something wrent wrogn \n" + e);
                    }
                    if (gotData)
                    {
                        //converting bytes to int, sending DMX Values
                        int channel = Convert.ToInt32(incomingValues[0]);
                        int value = Convert.ToInt32(incomingValues[1]);
                        DmxInterface.SetData(channel, value);
                        Console.WriteLine("Received Data... \n Channel: " + channel + " Value: " + value);
                    }
            }
            Console.WriteLine("Connection lost");
        }
    }
}
