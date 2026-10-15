using RotMG.Networking;
using System;
using System.Collections.Generic;

namespace RotMG.Game.Entities
{
    //Separates liveness from anti-cheat. ValidTime used to both police
    //packet timestamps and disconnect anyone quiet for 2 s, so a GC pause,
    //tab throttle, or a 1 ms timestamp rewind kicked the player. Idle is
    //now any inbound packet (plus a Ping/Pong keepalive) with a 45 s
    //timeout; ValidTime only rejects real time-travel and sustained
    //re-baseline abuse.
    public partial class Player
    {
        public const int MaxLatencyMS = 2000;
        public const int MaxBackwardsStepMS = 100;
        public const int IdleTimeoutMS = 45000;
        public const int PingIntervalMS = 5000;
        public const int MaxRebaselinesPerMinute = 5;
        public const int RebaselineWindowMS = 60000;

        private readonly ClientTimeGate _timeGate = new ClientTimeGate();
        private int _pingSerial;

        public bool ValidTime(int clientTime)
        {
            ClientTimeGate.Status status = _timeGate.Evaluate(clientTime, Manager.TotalTimeUnsynced);
            if (status == ClientTimeGate.Status.Rebaseline)
            {
#if DEBUG
                Program.Print(PrintType.Warn, "Client clock re-baselined, snapping to server position");
#endif
                MoveTime = clientTime;
                SnapClientToServerPosition();
                return true;
            }
            return status == ClientTimeGate.Status.Accept;
        }

        private void SnapClientToServerPosition()
        {
            if (Dead || Parent == null || AwaitingGoto == null || Client == null || Client.State != ProtocolState.Connected)
                return;
            EnqueueGotoWait();
            BroadcastGoto(Position, withTeleportEffect: false);
        }
    }

    //Pure clock gate so ValidTime can be unit-tested without a world.
    internal sealed class ClientTimeGate
    {
        public enum Status
        {
            Accept,
            Reject,
            Rebaseline
        }

        private int _serverStartTime = -1;
        private int _serverTime = -1;
        private int _clientStartTime = -1;
        private int _clientTime = -1;
        private readonly List<int> _rebaselineTimes = new List<int>();

        public Status Evaluate(int clientTime, int serverTime)
        {
            if (_serverTime == -1)
            {
                _clientTime = clientTime;
                _clientStartTime = clientTime;
                _serverTime = serverTime;
                _serverStartTime = serverTime;
                return Status.Accept;
            }

            int clientDiff = clientTime - _clientTime;
            int serverDiff = serverTime - _serverTime;

            if (clientDiff < 0)
            {
                if (-clientDiff > Player.MaxBackwardsStepMS)
                    return Status.Reject;
                _serverTime = serverTime;
                return Status.Accept;
            }

            int startDiff = Math.Abs((serverTime - _serverStartTime) - (clientTime - _clientStartTime));
            if (clientDiff > Player.MaxLatencyMS || serverDiff > Player.MaxLatencyMS || startDiff > Player.MaxLatencyMS)
                return TryRebaseline(clientTime, serverTime);

            _clientTime = clientTime;
            _serverTime = serverTime;
            return Status.Accept;
        }

        private Status TryRebaseline(int clientTime, int serverTime)
        {
            for (int i = _rebaselineTimes.Count - 1; i >= 0; i--)
            {
                if (serverTime - _rebaselineTimes[i] > Player.RebaselineWindowMS)
                    _rebaselineTimes.RemoveAt(i);
            }
            if (_rebaselineTimes.Count >= Player.MaxRebaselinesPerMinute)
                return Status.Reject;

            _rebaselineTimes.Add(serverTime);
            _clientTime = clientTime;
            _clientStartTime = clientTime;
            _serverTime = serverTime;
            _serverStartTime = serverTime;
            return Status.Rebaseline;
        }
    }
}
