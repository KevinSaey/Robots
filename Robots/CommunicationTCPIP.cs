using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

//code adapted from https://docs.microsoft.com/en-us/dotnet/framework/network-programming/asynchronous-server-socket-example
namespace Robots
{
    public class ServerTCPIP
    {
        TcpListener _server;
        TcpClient _client;
        NetworkStream _networkStream;
        byte[] _bytes = new byte[6 * 4];

        ServerTCPIP(IPAddress ip, int port)
        {
            _server = new TcpListener(ip, port);
            _server.Start();

        }

        public async Task Connect()
        {
            try
            {
                _client = await _server.AcceptTcpClientAsync();

                NetworkStream _networkStream = _client.GetStream();
                Console.WriteLine($"Server connected to a client");
            }
            catch (SocketException e)
            {
                Console.WriteLine($"SocketException: {e}");
            }
        }

        public async Task TrySendJoints(float[] joints)
        {
            try
            {
                Buffer.BlockCopy(joints, 0, _bytes, 0, _bytes.Length);
                await _networkStream.WriteAsync(_bytes, 0, _bytes.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exeption:{e.Message}");

            }
        }
    }
    /*public class ServerTCPIP
    {
        System.Net.Sockets.
    }


    public class ObjectState
    {
        public Socket wSocket = null;
        public const int bufferSize = 1024;
        public byte[] buffer = new byte[bufferSize];
        public StringBuilder sb = new StringBuilder();

    }

    public class AsyncSocketListener
    {
        public static ManualResetEvent allCompleted = new ManualResetEvent(false);
        public static void StartListener()
        {
            byte[] bytes = new byte[1024];

            IPHostEntry ipHost = ipHost = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ip = ipHost.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ip, 4343);
            Socket listener = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                while (true)
                {
                    allCompleted.Reset();
                    Console.WriteLine($"Waiting for incomming connections.");
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                    allCompleted.WaitOne();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }
        private static void AcceptCallback(IAsyncResult ar)
        {
            allCompleted.Set();
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            ObjectState state = new ObjectState();
            state.wSocket = handler;
            handler.BeginReceive(state.buffer, 0, ObjectState.bufferSize, 0, new AsyncCallback(ReadCallback), state);

        }

        private static void ReadCallback(IAsyncResult ar)
        {
            string content = String.Empty;
            ObjectState state = (ObjectState)ar.AsyncState;
            Socket handler = state.wSocket;
            int bytesRead = handler.EndReceive(ar);
            if (bytesRead > 0)
            {
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                content = state.sb.ToString();
                if (content.IndexOf("<EOF>", StringComparison.Ordinal) > -1)
                {
                    Console.WriteLine($"Read: {content.Length} bytes from socket Data: {content}");
                    Send(handler, content);
                }
                else
                {
                    handler.BeginReceive(state.buffer, 0, ObjectState.bufferSize, 0, new AsyncCallback(ReadCallback), state);
                }
            }
        }

        private static void Send(Socket handler, string content)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(content);
            handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket)ar.AsyncState;
                int byteSent = handler.EndSend(ar);
                Console.WriteLine($"Sent: {byteSent} to client");

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (Exception e)
            {

                Console.WriteLine(e);
            }
        }
    }*/
}
