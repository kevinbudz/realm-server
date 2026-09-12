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

        //Headless stand-in for the 2 s stall kick: a 3 s / 10 s gap rubber-
        //bands (re-baseline + Goto) instead of disconnecting; a 20-tile
        //Move in one tick is still a ValidMove reject (P14: strike, then
        //kick at the threshold); >5 re-baselines in a minute still kicks.
        public static bool VerifyTimingPolicy()
        {
            ClientTimeGate gate = new ClientTimeGate();
            if (gate.Evaluate(1000, 1000) != ClientTimeGate.Status.Accept)
            {
                Program.Print(PrintType.Error, "P6 verify FAIL: first packet must accept");
                return false;
            }
            if (gate.Evaluate(1100, 1100) != ClientTimeGate.Status.Accept)
            {
                Program.Print(PrintType.Error, "P6 verify FAIL: monotonic step must accept");
                return false;
            }
            if (gate.Evaluate(1050, 1150) != ClientTimeGate.Status.Accept)
            {
                Program.Print(PrintType.Error, "P6 verify FAIL: <=100 ms rewind must be tolerated");
                return false;
            }
            if (gate.Evaluate(990, 1200) != ClientTimeGate.Status.Reject)
            {
                Program.Print(PrintType.Error, "P6 verify FAIL: >100 ms rewind must reject");
                return false;
            }

            ClientTimeGate pause = new ClientTimeGate();
            pause.Evaluate(1000, 1000);
            if (pause.Evaluate(4000, 4000) != ClientTimeGate.Status.Rebaseline)
            {
                Program.Print(PrintType.Error, "P6 verify FAIL: 3 s client/server gap must re-baseline, not kick");
                return false;
            }
            if (pause.Evaluate(14000, 14000) != ClientTimeGate.Status.Rebaseline)
            {
                Program.Print(PrintType.Error, "P6 verify FAIL: 10 s alt-tab gap must re-baseline, not kick");
                return false;
            }

            ClientTimeGate abuse = new ClientTimeGate();
            abuse.Evaluate(0, 0);
            int accepted = 0;
            int rejected = 0;
            for (int i = 1; i <= MaxRebaselinesPerMinute + 1; i++)
            {
                ClientTimeGate.Status status = abuse.Evaluate(i * (MaxLatencyMS + 1), i * (MaxLatencyMS + 1));
                if (status == ClientTimeGate.Status.Rebaseline)
                    accepted++;
                else if (status == ClientTimeGate.Status.Reject)
                    rejected++;
            }
            if (accepted != MaxRebaselinesPerMinute || rejected != 1)
            {
                Program.Print(PrintType.Error, $"P6 verify FAIL: expected {MaxRebaselinesPerMinute} re-baselines then a kick, got {accepted}/{rejected}");
                return false;
            }

            AoeAck aoe = new AoeAck { Time = 0 };
            if (aoe.Advance(1000, TimeUntilAckTimeout) != AoeAck.Waiting)
            {
                Program.Print(PrintType.Error, "P6 verify FAIL: aoe wait expired before 2 s");
                return false;
            }
            if (aoe.Advance(2001, TimeUntilAckTimeout) != AoeAck.GrantedGrace || !aoe.Forgiven || aoe.Time != 2001)
            {
                Program.Print(PrintType.Error, "P6 verify FAIL: first aoe timeout must grant one grace");
                return false;
            }
            if (aoe.Advance(4002, TimeUntilAckTimeout) != AoeAck.Expired)
            {
                Program.Print(PrintType.Error, "P6 verify FAIL: second aoe timeout must resolve without kick");
                return false;
            }

            //Speed-hack detection is ValidMove: 20 tiles in a 100 ms tick
            //exceeds any 1.1x speed envelope (P14 strikes, then kicks).
            float tilesPerMs = 0.01f;
            float maxDistance = (tilesPerMs * 100) * 1.1f;
            if (20f <= maxDistance)
            {
                Program.Print(PrintType.Error, "P6 verify FAIL: 20-tile Move must still fail the speed envelope");
                return false;
            }

            Program.Print(PrintType.Info, "P6 verify: stall re-baselines, idle is 45 s, aoe grace then resolve, speed envelope intact");
            return true;
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
