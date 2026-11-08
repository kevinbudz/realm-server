using RotMG.Common;
using System;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace RotMG.Networking
{
    //Browser front door for the game socket. Each WebSocket is bridged to one loopback TCP pair: the server end goes to
    //GameServer.Admit exactly like an accepted raw socket, and the bridge end carries the bytes to and from the WebSocket.
    public static class WebSocketServer
    {
        private static bool _terminating;
        private static HttpListener _listener;
        private static Socket _pairListener;
        private static IPEndPoint _pairEndpoint;
        private static readonly object _pairLock = new object(); //One pending loopback connection at a time keeps the pairing exact.

        public static void Init()
        {
            _pairListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _pairListener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            _pairListener.Listen(16);
            _pairEndpoint = (IPEndPoint)_pairListener.LocalEndPoint;

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://{Settings.Address}:{Settings.WebSocketPort}/");
        }

        public static void Stop()
        {
            _terminating = true;
            try { _listener?.Close(); } catch { }
            try { _pairListener?.Close(); } catch { }
        }

        public static void Start()
        {
            _listener.Start();
            Program.Print(PrintType.Info, $"Started WebSocket listener at <{_listener.Prefixes.First()}>");

            while (!_terminating)
            {
                HttpListenerContext context;
                try
                {
                    context = _listener.GetContext();
                }
                catch (Exception ex)
                {
                    if (_terminating)
                        return;
#if DEBUG
                    Program.Print(PrintType.Error, ex.ToString());
#endif
                    Thread.Sleep(10);
                    continue;
                }
                //Each bridge runs on its own task, so a slow handshake never holds up the accept loop.
                _ = BridgeAsync(context);
            }
        }

        private static async Task BridgeAsync(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            //Origin is logged only: there is no allow-list yet, so any page may open the game socket.
            string origin = request.Headers["Origin"] ?? "(none)";
            if (!request.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
                return;
            }

            IPAddress realIp = request.RemoteEndPoint.Address; //X-Forwarded-For is ignored: nothing trusts a proxy here.
            Program.Print(PrintType.Info, $"WebSocket connect from <{realIp}> origin <{origin}>");

            WebSocket ws;
            try
            {
                HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(subProtocol: null);
                ws = wsContext.WebSocket;
            }
            catch (Exception ex)
            {
#if DEBUG
                Program.Print(PrintType.Error, ex.ToString());
#endif
                try { context.Response.StatusCode = 500; context.Response.Close(); } catch { }
                return;
            }

            Socket bridge = null;
            Socket server = null;
            try
            {
                (bridge, server) = CreateLoopbackPair();
                //Admit closes the server end itself on rejection, after writing the framed Failure into it.
                GameServer.Admit(server, realIp);
            }
            catch (Exception ex)
            {
#if DEBUG
                Program.Print(PrintType.Error, ex.ToString());
#else
                _ = ex;
#endif
                //Admit hands the server end off only as its last step, so on an exception it is still ours.
                GameServer.CloseSocket(server);
                GameServer.CloseSocket(bridge);
                ws.Dispose();
                return;
            }

            await PumpAsync(ws, bridge);
        }

        //Blocking connect plus accept on the private listener. The lock keeps two concurrent bridges from
        //pairing each other's sockets. The server end's peer must be our bridge end, or the connection is dropped.
        private static (Socket bridge, Socket server) CreateLoopbackPair()
        {
            lock (_pairLock)
            {
                Socket bridge = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    bridge.NoDelay = true;
                    bridge.Connect(_pairEndpoint);
                    Socket server = _pairListener.Accept();
                    if (!server.RemoteEndPoint.Equals(bridge.LocalEndPoint))
                    {
                        GameServer.CloseSocket(server);
                        throw new Exception("Loopback pairing received a foreign connection");
                    }
                    return (bridge, server);
                }
                catch
                {
                    GameServer.CloseSocket(bridge);
                    throw;
                }
            }
        }

        //Runs until either side closes or fails. Admit may already have written a framed Failure into the bridge,
        //so the socket-to-WebSocket pump keeps reading until EOF and sends every byte before the WebSocket closes.
        private static async Task PumpAsync(WebSocket ws, Socket bridge)
        {
            Task toSocket = PumpToSocketAsync(ws, bridge);
            Task toWebSocket = PumpToWebSocketAsync(bridge, ws);
            await Task.WhenAny(toSocket, toWebSocket);

            //Closing both unblocks whichever pump is still waiting.
            GameServer.CloseSocket(bridge);
            try
            {
                if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
                    await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
            }
            catch { }
            ws.Dispose();
        }

        private static async Task PumpToSocketAsync(WebSocket ws, Socket bridge)
        {
            byte[] buffer = new byte[GameServer.BufferSize];
            try
            {
                while (true)
                {
                    WebSocketReceiveResult result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                        return;

                    int offset = 0;
                    while (offset < result.Count)
                        offset += await bridge.SendAsync(new ArraySegment<byte>(buffer, offset, result.Count - offset), SocketFlags.None);
                }
            }
            catch { }
        }

        private static async Task PumpToWebSocketAsync(Socket bridge, WebSocket ws)
        {
            byte[] buffer = new byte[GameServer.BufferSize];
            try
            {
                while (true)
                {
                    int n = await bridge.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                    if (n == 0)
                        return;
                    await ws.SendAsync(new ArraySegment<byte>(buffer, 0, n), WebSocketMessageType.Binary, true, CancellationToken.None);
                }
            }
            catch { }
        }
    }
}
