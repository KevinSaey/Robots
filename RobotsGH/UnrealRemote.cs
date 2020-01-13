using System;
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
        NetworkStream _networkStream;
        byte[] _bytes = new byte[6 * 4];
        List<string> _log = new List<string>();
        bool _connect, _play, _pause;

        public UnrealRemote() : base("UnrealRemote", "UE Remote", "Connect and stream to Unreal engine", "Robots", "Components") { }
        public override Guid ComponentGuid => new Guid("0f69c3b9-a687-45e6-a250-d015d7df7f26");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconURRemote;
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new ProgramParameter(), "Joints", "J", "Program", GH_ParamAccess.item);
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
            GH_String ip = null;

            if (!DA.GetData("IP", ref ip)) { _log.Add("No IP found"); }
            if(DA.GetData("Connect", ref _connect))
            {

            }

            DA.SetDataList("Log", _log);
        }


        protected async void Connect(IPAddress ip, int port)
        {
            _server = new TcpListener(ip, port);
            _server.Start();

            await AsyncConnect();
        }

        public async Task AsyncConnect()
        {
            try
            {
                _client = await _server.AcceptTcpClientAsync();

                NetworkStream _networkStream = _client.GetStream();
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
        }
    }
}