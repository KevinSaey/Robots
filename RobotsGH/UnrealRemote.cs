﻿using System;
using System.Collections.Generic;
using System.Linq;

using System.Threading;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Robots.Grasshopper
{
    public class UnrealRemote : GH_Component
    {
        TcpListener _server;
        TcpClient _client;
        public bool Connected => _client?.Connected == true;
        string _ip;
        int _port;
        bool _logDirty = false;

        byte[] _bytes = new byte[6 * 4];
        List<string> _log = new List<string>();
        bool _connect, _play, _pause;
        Task _task;

        public UnrealRemote() : base("UnrealRemote", "UE Remote", "Connect and stream to Unreal engine", "Robots", "Components") { }
        public override Guid ComponentGuid => new Guid("0f69c3b9-a687-45e6-a250-d015d7df7f26");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconURRemote;
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new ProgramParameter(), "Program", "P", "Program", GH_ParamAccess.item);
            pManager.AddNumberParameter("PO", "Port", "Port of the server", GH_ParamAccess.item);
            pManager.AddTextParameter("IP", "IP", "IP address of the server", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Connect", "C", "Connect Unreal", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Play", "P", "Play", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Pause", "S", "Pause", GH_ParamAccess.item, false);
            for (int i = 0; i < 5; i++)
            {
                pManager[i].Optional = true;
            }
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Log", "L", "Log", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool connect = false;
            DA.GetData("Connect", ref connect);

            if (!connect && Connected)
            {
                Dispose();
            }

            if (Connected)
            {
                GH_Program program = null;
                if (!DA.GetData(0, ref program)) { return; }
                SendJoints(program.Value);
            }

            if (connect && !Connected)
            {
                GH_Number port = new GH_Number();
                GH_String ip = new GH_String();
                if (!DA.GetData("PO", ref port)) { _log.Add("No Port found"); return; }
                if (!DA.GetData("IP", ref ip)) { _log.Add("No IP found"); return; }

                _port = (int)port.Value;
                _ip = ip.ToString();

                _log.Clear();
                ConnectToUnreal();

                // if (_task == null || _task.Status != TaskStatus.Running)
                //  {


                //_task = Task.Run(async () => await ConnectToUnreal())
                // .ContinueWith(task =>
                //  {
                //       this.ExpireSolution(true);
                //   }, TaskScheduler.FromCurrentSynchronizationContext());
                // }
                //else
                // {
                //      _log.Add("Trying to connect to Unreal");
                // }
            }

            DA.SetDataList("Log", _log);
        }

        private void SendJoints(Program program)
        {
            var joints = GetJointsFromProgram(program);

            RobotCell cell = program.RobotSystem as RobotCell;
            var robot = cell.MechanicalGroups[0].Robot;
            var bytes = new List<byte>(6 * 4);

            for (int i = 0; i < 6; i++)
            {
                var joint = joints[i];
                var degree = robot.RadianToDegree(joint, i);
                bytes.AddRange(BitConverter.GetBytes((float)degree));
            }

            _client.GetStream().Write(bytes.ToArray(), 0, bytes.Count);
        }

        protected void ConnectToUnreal()
        {
            _log.Add("Waiting for Unreal to connect...");
            Connect(_ip, _port);
            if (Connected)
            {
                _log.Add("Unreal connected.");
            }
            else
            {
                _log.Add("Unreal connection error.");
                Dispose();
            }

        }

        protected void Connect(string ip, int port)
        {
            try
            {
                _log.Add("Connecting");
                IPAddress serverIp = IPAddress.Parse(ip);
                // Dispose();
                _server = new TcpListener(serverIp, port);
                _server.Server.LingerState = new LingerOption(true, 30);
                _server.Start();

                _client = _server.AcceptTcpClient();

                _log.Add($"Connected to - {_server.Server.RemoteEndPoint}");
            }
            catch (SocketException e)
            {
                _log.Add($"SocketException - {e}");

            }
        }

        Vector6 GetJointsFromProgram(Program program)
        {
            var joints = program.CurrentSimulationTarget.Joints;
            return new Vector6(joints.Select(j => (float)j).ToArray());
        }

        //public async Task SendJointsAsync(Vector6 joints)
        //{
        //    var bytes = new List<byte>(6 * 4);

        //    for (int i = 0; i < 6; i++)
        //    {
        //        bytes.AddRange(BitConverter.GetBytes(joints[i]));
        //    }

        //    await SendAsync(bytes.ToArray());
        //}

        //public async Task<int> ReadAsync()
        //{
        //    byte[] bytes = new byte[4];

        //    if (!Connected)
        //    {
        //        _log.Add("Can't receive data, not connected.");
        //        return -1;
        //    }

        //    var stream = _client.GetStream();

        //    do
        //    {
        //        await stream.ReadAsync(bytes, 0, bytes.Length);
        //    }
        //    while (stream.DataAvailable);

        //    var info = BitConverter.ToInt32(bytes, 0);
        //    _log.Add($"Info no. {info}");
        //    return info;
        //}

        //async Task SendAsync(byte[] bytes)
        //{
        //    if (!Connected)
        //    {
        //        _log.Add("Can't send data, not connected.");
        //        return;
        //    }

        //   // var stream = _client.GetStream();
        //    await stream.WriteAsync(bytes, 0, bytes.Length);
        //}

        void Log(string text)
        {
            _log.Add($"Server: {text}");
        }

        public void Dispose()
        {
            if (_client == null) return;
            if (_server == null) return;

            _client.Close();
            _client.Dispose();
            _server.Stop();
            _client = null;
            _server = null;
            _log.Add("Disconnected.");
        }

        /*
                protected async void Connect(string strIP, int port)
                {
                    try
                    {
                        //IPAddress ip = IPAddress.Parse("192.168.1.1");
                        IPEndPoint ipEnd = new IPEndPoint(IPAddress.Any, 9999);
                        _server = new TcpListener(ipEnd);
                        _server.Start();

                        //await AsyncConnect();
                    }
                    catch (Exception e)
                    {
                        _log.Add($"exception source: {e.Source}");
                        _log.Add($"exception: {e.Message}");
                    }
                }

                public async Task AsyncConnect()
                {
                    try
                    {
                        _client = await _server.AcceptTcpClientAsync();

                        _networkStream = _client.GetStream();
                        _log.Add($"Server connected to a client");
                    }
                    catch (SocketException e)
                    {
                        _log.Add($"SocketException: {e}");
                    }
                }

                public async Task AsyncSendJoints(float[] joints)
                {
                    try
                    {
                        Buffer.BlockCopy(joints, 0, _bytes, 0, _bytes.Length);
                        await _networkStream.WriteAsync(_bytes, 0, _bytes.Length);
                    }
                    catch (Exception e)
                    {
                        _log.Add($"Exeption:{e.Message}");
                    }
                }*/
    }
}