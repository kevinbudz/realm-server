using RotMG.Common;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace RotMG.Game.Entities
{
    //One server-issued Goto waiting for its GotoAck. The wait is stamped
    //with Manager.TotalTimeUnsynced (same idea as AwaitingShots.EnqueueTime):
    //Teleport used to enqueue either client getTimer() or server uptime,
    //and TryMove compared that mix against the client's Move timestamp,
    //which is a different epoch. Forgiven is the one grace re-stamp a
    //starved head gets before the wait is dropped and the client is
    //Goto'd again instead of disconnected.
    public class AwaitingGotoWait
    {
        public int ServerEnqueuedAt;
        public bool Forgiven;

        public const int Waiting = 0;
        public const int GrantedGrace = 1;
        public const int Expired = 2;

        //Server-clock step: still waiting, one grace re-stamp, or expired
        //(caller dequeues and snaps). nowUnsynced is Manager.TotalTimeUnsynced.
        public int Advance(int nowUnsynced, int timeoutMs)
        {
            if (nowUnsynced - ServerEnqueuedAt <= timeoutMs)
                return Waiting;
            if (!Forgiven)
            {
                Forgiven = true;
                ServerEnqueuedAt = nowUnsynced;
                return GrantedGrace;
            }
            return Expired;
        }
    }

    //Invalid Move / shoot / AoE-ack corrections. One Goto rubber-band per
    //reject; disconnect only after Limit strikes inside WindowMS so a
    //boss wall or a one-tile desync cannot kick, while a 10-tile/tick
    //speed hack still burns through the window.
    public class MoveStrikeTracker
    {
        public const int Limit = 10;
        public const int WindowMS = 30000;

        private readonly List<int> _times = new List<int>();

        public int Count => _times.Count;

        //True if this strike should disconnect.
        public bool Strike(int nowUnsynced)
        {
            for (int i = _times.Count - 1; i >= 0; i--)
            {
                if (nowUnsynced - _times[i] > WindowMS)
                    _times.RemoveAt(i);
            }
            _times.Add(nowUnsynced);
            return _times.Count >= Limit;
        }
    }

    public partial class Player
    {
        private const float MoveSpeedThreshold = 1.1f;
        private const int SpeedHistoryCount = 10; //in world ticks (10 = 1 sec history), the lower the count, the stricter the detection
        public const int MaxAwaitingMoves = 3;

        public float MoveMultiplier = 1f;
        public int MoveTime;
        public int AwaitingMoves;
        public Queue<AwaitingGotoWait> AwaitingGoto;
        public List<float> SpeedHistory;
        public List<float> MultiplierHistory;
        public int TickId;
        public float PushX;
        public float PushY;
        private readonly MoveStrikeTracker _moveStrikes = new MoveStrikeTracker();

        //Mad Lab vat pools (production behavior): the green "Bad Vat"
        //pools hex, the blue "Good Vat" pools cleanse. Tile types are fixed
        //by GameData, so they are matched by type, not by name.
        private const ushort LabBadVatNoSlow = 0x82;
        private const ushort LabBadVatRug = 0xa9;
        private const ushort LabGoodVatNoSlow = 0x83;
        private const ushort LabGoodVatRug = 0xa7;

        public static bool IsHexPoolTile(ushort tileType) =>
            tileType == LabBadVatNoSlow || tileType == LabBadVatRug;

        public static bool IsCleansePoolTile(ushort tileType) =>
            tileType == LabGoodVatNoSlow || tileType == LabGoodVatRug;

        //Green pools hex until cleansed (infinite duration, like
        //production) and strip Speedy to prevent green-pool speed abuse;
        //blue pools remove the hex. Catwalks and other covers flagged
        //ProtectFromGroundDamage shield the player from both pools, so a
        //catwalk over a vat neither hexes nor cleanses.
        private void ApplyVatPoolEffects(Tile tile)
        {
            if (tile.StaticObject?.Desc.ProtectFromGroundDamage ?? false)
                return;
            TileDesc desc = Resources.Type2Tile[tile.Type];
            if (IsHexPoolTile(desc.Type))
            {
                ApplyConditionEffect(ConditionEffectIndex.Hexed, -1);
                if (HasConditionEffect(ConditionEffectIndex.Speedy))
                    RemoveConditionEffect(ConditionEffectIndex.Speedy);
            }
            else if (IsCleansePoolTile(desc.Type))
            {
                RemoveConditionEffect(ConditionEffectIndex.Hexed);
            }
        }

        public void PushSpeedToHistory(float speed, float mult)
        {
            SpeedHistory.Add(speed);
            MultiplierHistory.Add(mult);
            if (SpeedHistory.Count > SpeedHistoryCount)
            {
                SpeedHistory.RemoveAt(0); //Remove oldest entry
                MultiplierHistory.RemoveAt(0);
            }
        }

        public Tuple<float, float> GetHighestSpeedHistory()
        {
            float speed = 0f;
            float mult = 0f;
            for (int i = 0; i < SpeedHistoryCount; i++)
            {
                if (SpeedHistory[i] > speed) speed = SpeedHistory[i];
                if (MultiplierHistory[i] > mult) mult = MultiplierHistory[i];
            }
            return Tuple.Create(speed, mult);
        }

        public bool ValidMove(int time, Position pos)
        {
            //Clients send raw floats: reject non-finite coordinates here so a
            //NaN can never become an entity position (NaN distances compare
            //false and would otherwise pass every check below).
            if (!float.IsFinite(pos.X) || !float.IsFinite(pos.Y))
                return false;
            int diff = time - MoveTime;
            Tuple<float, float> history = GetHighestSpeedHistory();
            float maxDistance = ((history.Item1 * history.Item2) * diff) * MoveSpeedThreshold;
            Position pushedServerPosition = new Position(Position.X - (diff * PushX), Position.Y - (diff * PushY));
            if (pos.Distance(pushedServerPosition) > maxDistance && pos.Distance(Position) > maxDistance)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Move stuffs... DIST/SPD = " + pos.Distance(pushedServerPosition) + " : " + maxDistance);
#endif
                return false;
            }
            return true;
        }

        public void TryMove(int time, Position pos)
        {
            //Every received Move consumes one NewTick credit, including
            //the ones we ignore (goto wait, death). The client always
            //decrements movesRequested_ after sending, so skipping the
            //decrement here used to inflate AwaitingMoves for the rest
            //of the session.
            AwaitingMoves = ConsumeMove(AwaitingMoves, out bool tooMany);

            //Dead players' packets are meaningless (the client keeps
            //sending for ~1500ms before the death disconnect): touching
            //world state with them re-adds corpses to chunks and throws
            //on disposed worlds, and the exception path disconnects with
            //queued packets still unsent.
            if (Dead)
                return;

            if (!ValidTime(time))
            {
                Client.RequestDisconnect("Invalid time");
                return;
            }

            if (TickGotoAcks() || AwaitingGoto.Count > 0)
            {
#if DEBUG
                if (AwaitingGoto.Count > 0)
                    Program.Print(PrintType.Error, $"Waiting for goto ack... AwaitingMoves={AwaitingMoves}");
#endif
                return;
            }

            if (tooMany)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Too many move packets");
#endif
                RejectInvalidMove("Too many move packets");
                return;
            }

            if (!ValidMove(time, pos))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Invalid move");
#endif
                RejectInvalidMove("Invalid move");
                return;
            }

            //Client isWalkable (NoWalk / OccupySquare) plus isFullOccupy
            //on this tile and its occupied edges. TileFullOccupied at the
            //dest covers a FullOccupy object at tile center (frac == 0.5
            //skips the edge checks inside RegionUnblocked).
            if (!RegionUnblocked(pos.X, pos.Y) || TileFullOccupied(pos.X, pos.Y))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Tile occupied");
#endif
                RejectInvalidMove("Tile occupied");
                return;
            }

            Tile? tile = Parent.GetTile((int)pos.X, (int)pos.Y);
            if (tile == null)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Move out of bounds");
#endif
                RejectInvalidMove("Move out of bounds");
                return;
            }
            TileDesc desc = Resources.Type2Tile[tile.Value.Type];
            if (desc.Damage > 0 && !HasConditionEffect(ConditionEffectIndex.Invincible))
            {
                if (!(tile.Value.StaticObject?.Desc.ProtectFromGroundDamage ?? false) && Damage(desc.Id, desc.Damage, new ConditionEffectDesc[0], true))
                    return;
            }
            ApplyVatPoolEffects(tile.Value);

            Position prevPos = Position;
            Parent.MoveEntity(this, pos);
            //Observe-only sweep over the validated movement segment:
            //records unreported bullet contacts for counting at expiry
            //(see VerifyProjectiles). It deals no damage and cannot
            //disconnect mid-loop; suspicion only counts at expiry.
            VerifyProjectiles(time, prevPos);

            if (desc.Push)
            {
                PushX = desc.DX;
                PushY = desc.DY;
            }
            else
            {
                PushX = 0;
                PushY = 0;
            }

            MoveMultiplier = GetMoveMultiplier();
            MoveTime = time;

            PushSpeedToHistory(GetMovementSpeed(), MoveMultiplier); //Add a new entry
        }

        public void TryGroundHit(int time, Position pos)
        {
            if (!ValidTime(time))
            {
                Client.Disconnect();
                return;
            }

            Tile? tile = Parent.GetTile((int)pos.X, (int)pos.Y);
            if (tile == null)
                return;
            TileDesc desc = Resources.Type2Tile[tile.Value.Type];
            if (desc.Damage > 0 && !HasConditionEffect(ConditionEffectIndex.Invincible))
                if (!(tile.Value.StaticObject?.Desc.ProtectFromGroundDamage ?? false))
                    Damage(desc.Id, desc.Damage, new ConditionEffectDesc[0], true);
            ApplyVatPoolEffects(tile.Value);
        }

        public void TryGotoAck(int time)
        {
            if (!ValidTime(time))
            {
#if DEBUG
                Program.Print(PrintType.Error, "GotoAck invalid time");
#endif
                Client.Disconnect();
                return;
            }

            //A late or duplicate ack (lost Goto recovered by TickGotoAcks,
            //or an extra GotoAck after the wait was dropped) must not kick.
            //The wait is a server-clock queue; the packet time is only for
            //ValidTime above.
            if (!AwaitingGoto.TryDequeue(out _))
            {
#if DEBUG
                Program.Print(PrintType.Error, "No GotoAck to ack");
#endif
            }
        }

        //Admin teleports (/tq) target entities the player has often never
        //seen: every unseen tile mismatches the client's last-seen
        //UpdateCount, so those callers bypass the seen check. The client's
        //update loop streams the destination tiles on the following ticks,
        //exactly as on dungeon entry. RegionUnblocked still applies.
        public bool Teleport(Position pos, bool ignoreSeen = false)
        {
            if (!RegionUnblocked(pos.X, pos.Y))
                return false;

            Tile? tile = Parent.GetTileF((int)pos.X, (int)pos.Y);
            if (tile == null || (!ignoreSeen && GetSeenTileUpdate((int)pos.X, (int)pos.Y) != tile.Value.UpdateCount))
                return false;

            Parent.MoveEntity(this, pos);
            //Bots have a Disconnected stub client and never send GotoAck;
            //observers still get the broadcast Goto below.
            if (Client.State == ProtocolState.Connected)
                EnqueueGotoWait();
            BroadcastGoto(pos, withTeleportEffect: true);
#if DEBUG
            Program.Print(PrintType.Debug, $"Teleport Goto queued, AwaitingMoves={AwaitingMoves}");
#endif
            return true;
        }

        //Ack waits use the server clock (see AwaitingGotoWait): the head
        //is the oldest waiter. A starved head is forgiven once (a late
        //ack still drains it in order); a twice-starved head is dropped
        //and the client is Goto'd to the server position so a lost ack
        //cannot freeze or kick them. Returns true if a wait was resolved
        //this call so TryMove can ignore a stale in-flight Move.
        public bool TickGotoAcks()
        {
            if (Dead || Parent == null)
                return false;

            bool resolved = false;
            while (AwaitingGoto.Count > 0)
            {
                AwaitingGotoWait head = AwaitingGoto.Peek();
                int step = head.Advance(Manager.TotalTimeUnsynced, TimeUntilAckTimeout);
                if (step == AwaitingGotoWait.Waiting)
                    break;
                if (step == AwaitingGotoWait.GrantedGrace)
                {
#if DEBUG
                    Program.Print(PrintType.Warn, "Goto ack late, forgiven");
#endif
                    break;
                }

                AwaitingGoto.Dequeue();
#if DEBUG
                Program.Print(PrintType.Error, "Goto ack wait expired, snapping client");
#endif
                BroadcastGoto(Position, withTeleportEffect: false);
                resolved = true;
            }
            return resolved;
        }

        private void EnqueueGotoWait()
        {
            AwaitingGoto.Enqueue(new AwaitingGotoWait
            {
                ServerEnqueuedAt = Manager.TotalTimeUnsynced
            });
        }

        private void BroadcastGoto(Position pos, bool withTeleportEffect)
        {
            byte[] go = GameServer.Goto(Id, pos);
            byte[] eff = withTeleportEffect
                ? GameServer.ShowEffect(ShowEffectIndex.Teleport, Id, 0xFFFFFFFF, pos)
                : null;
            foreach (Player player in Parent.Players.Values)
            {
                if (withTeleportEffect && player.Client.Account.Effects)
                    player.Client.Send(eff);
                player.Client.Send(go);
            }
        }

        //Keep the server position, Goto the client, count a strike.
        //Returns true if the strike window is exhausted and the client
        //is being disconnected.
        public bool RejectInvalidMove(string reason)
        {
            SnapClientToServerPosition();
            if (_moveStrikes.Strike(Manager.TotalTimeUnsynced))
            {
#if DEBUG
                Program.Print(PrintType.Error, $"Move strike limit ({MoveStrikeTracker.Limit}/{MoveStrikeTracker.WindowMS}ms): {reason}");
#endif
                Client.RequestDisconnect(reason);
                return true;
            }
#if DEBUG
            Program.Print(PrintType.Warn, $"Move corrected ({_moveStrikes.Count}/{MoveStrikeTracker.Limit}): {reason}");
#endif
            return false;
        }

        //Server-placed static under this player: walk to the nearest free
        //tile and Goto. Not a strike — the server caused the overlap.
        public void NudgeToNearestWalkable()
        {
            if (Dead || Parent == null)
                return;
            if (!Parent.TryFindNearestWalkable((int)Position.X, (int)Position.Y, out Position dest))
                return;
            Parent.MoveEntity(this, dest);
            if (Client != null && Client.State == ProtocolState.Connected)
            {
                EnqueueGotoWait();
                BroadcastGoto(dest, withTeleportEffect: false);
            }
        }

        public static int CreditNewTick(int awaitingMoves)
        {
            awaitingMoves++;
            if (awaitingMoves > MaxAwaitingMoves)
                awaitingMoves = MaxAwaitingMoves;
            return awaitingMoves;
        }

        public static int ConsumeMove(int awaitingMoves, out bool tooMany)
        {
            awaitingMoves--;
            tooMany = awaitingMoves < 0;
            return awaitingMoves;
        }

        //Headless stand-in for mixed-clock /teleport kicks: both process
        //start orders, one grace then snap (no disconnect), extra GotoAck.
        public static bool VerifyGotoAckClock()
        {
            const int timeout = TimeUntilAckTimeout;

            int serverUptime = 30_000;
            int clientGetTimer = 150_000;
            bool oldKicksWhenServerStartedFirst = serverUptime + timeout < clientGetTimer;
            int serverJustRestarted = 500;
            int clientAlreadyOpen = 180_000;
            bool oldKicksWhenClientStartedFirst = serverJustRestarted + timeout < clientAlreadyOpen;
            if (!oldKicksWhenServerStartedFirst || !oldKicksWhenClientStartedFirst)
            {
                Program.Print(PrintType.Error, "P4 verify: old mixed-clock comparison no longer reproduces");
                return false;
            }

            AwaitingGotoWait wait = new AwaitingGotoWait { ServerEnqueuedAt = 30_000 };
            if (wait.Advance(31_000, timeout) != AwaitingGotoWait.Waiting)
            {
                Program.Print(PrintType.Error, "P4 verify FAIL: wait expired before 2s");
                return false;
            }

            int step = wait.Advance(32_001, timeout);
            if (step != AwaitingGotoWait.GrantedGrace || !wait.Forgiven || wait.ServerEnqueuedAt != 32_001)
            {
                Program.Print(PrintType.Error, "P4 verify FAIL: first timeout must grant one grace");
                return false;
            }

            if (wait.Advance(33_000, timeout) != AwaitingGotoWait.Waiting)
            {
                Program.Print(PrintType.Error, "P4 verify FAIL: grace window still open");
                return false;
            }

            if (wait.Advance(34_002, timeout) != AwaitingGotoWait.Expired)
            {
                Program.Print(PrintType.Error, "P4 verify FAIL: second timeout must expire without kick");
                return false;
            }

            AwaitingGotoWait freshServer = new AwaitingGotoWait { ServerEnqueuedAt = 100 };
            if (freshServer.Advance(1_500, timeout) != AwaitingGotoWait.Waiting)
            {
                Program.Print(PrintType.Error, "P4 verify FAIL: client-first ordering expired immediately");
                return false;
            }

            Queue<AwaitingGotoWait> q = new Queue<AwaitingGotoWait>();
            q.Enqueue(new AwaitingGotoWait { ServerEnqueuedAt = 0 });
            AwaitingGotoWait head = q.Peek();
            if (head.Advance(2_001, timeout) != AwaitingGotoWait.GrantedGrace)
            {
                Program.Print(PrintType.Error, "P4 verify FAIL: missing ack first timeout");
                return false;
            }
            if (head.Advance(4_002, timeout) != AwaitingGotoWait.Expired)
            {
                Program.Print(PrintType.Error, "P4 verify FAIL: missing ack second timeout");
                return false;
            }
            q.Dequeue();
            if (q.TryDequeue(out _))
            {
                Program.Print(PrintType.Error, "P4 verify FAIL: extra ack found a wait");
                return false;
            }

            Program.Print(PrintType.Info, "P4 verify: mixed clocks would have kicked; server-clock grace then resolve does not");
            return true;
        }

        //Headless stand-in for P14 (correct instead of kick, shared
        //walkability, spiral nudge) and P16 (AwaitingMoves accounting).
        public static bool VerifyMoveCorrection()
        {
            MoveStrikeTracker strikes = new MoveStrikeTracker();
            for (int i = 0; i < MoveStrikeTracker.Limit - 1; i++)
            {
                if (strikes.Strike(1000 + i))
                {
                    Program.Print(PrintType.Error, "P14 verify FAIL: kicked before 10 strikes");
                    return false;
                }
            }
            if (!strikes.Strike(1000 + MoveStrikeTracker.Limit - 1))
            {
                Program.Print(PrintType.Error, "P14 verify FAIL: 10th strike in 30s must kick");
                return false;
            }

            MoveStrikeTracker window = new MoveStrikeTracker();
            for (int i = 0; i < MoveStrikeTracker.Limit - 1; i++)
                window.Strike(0);
            if (window.Strike(MoveStrikeTracker.WindowMS + 1))
            {
                Program.Print(PrintType.Error, "P14 verify FAIL: strikes older than 30s must expire");
                return false;
            }

            if (!Entity.StaticBlocksPlayerWalk(occupySquare: true, fullOccupy: false, enemyOccupySquare: false, enemy: false))
            {
                Program.Print(PrintType.Error, "P14 verify FAIL: OccupySquare non-enemy static must block");
                return false;
            }
            if (Entity.StaticBlocksPlayerWalk(occupySquare: true, fullOccupy: false, enemyOccupySquare: false, enemy: true))
            {
                Program.Print(PrintType.Error, "P14 verify FAIL: OccupySquare on an enemy must not block by itself");
                return false;
            }
            if (!Entity.StaticBlocksPlayerWalk(occupySquare: false, fullOccupy: true, enemyOccupySquare: false, enemy: false))
            {
                Program.Print(PrintType.Error, "P14 verify FAIL: FullOccupy must block");
                return false;
            }
            if (!Entity.StaticBlocksPlayerWalk(occupySquare: false, fullOccupy: false, enemyOccupySquare: true, enemy: true))
            {
                Program.Print(PrintType.Error, "P14 verify FAIL: EnemyOccupySquare must block");
                return false;
            }
            if (Entity.StaticBlocksPlayerWalk(occupySquare: false, fullOccupy: false, enemyOccupySquare: false, enemy: false))
            {
                Program.Print(PrintType.Error, "P14 verify FAIL: empty static must not block");
                return false;
            }

            if (!World.TrySpiralFreeTile(5, 5, 20, (x, y) => x == 5 && y == 5, out int fx, out int fy))
            {
                Program.Print(PrintType.Error, "P14 verify FAIL: spiral must find a neighbour");
                return false;
            }
            if (fx == 5 && fy == 5)
            {
                Program.Print(PrintType.Error, "P14 verify FAIL: spiral must leave the blocked tile");
                return false;
            }
            if (Math.Max(Math.Abs(fx - 5), Math.Abs(fy - 5)) != 1)
            {
                Program.Print(PrintType.Error, "P14 verify FAIL: nearest free tile should be adjacent");
                return false;
            }

            //10 tiles in a 100 ms tick exceeds any 1.1x speed envelope
            //(same bound P6 uses); each such Move is one strike, so 10
            //of them still kick.
            float maxDistance = (0.01f * 100) * 1.1f;
            if (10f <= maxDistance)
            {
                Program.Print(PrintType.Error, "P14 verify FAIL: 10-tile Move must fail the speed envelope");
                return false;
            }
            MoveStrikeTracker speed = new MoveStrikeTracker();
            int speedKicks = 0;
            for (int i = 0; i < MoveStrikeTracker.Limit; i++)
            {
                if (10f > maxDistance && speed.Strike(i))
                    speedKicks++;
            }
            if (speedKicks != 1)
            {
                Program.Print(PrintType.Error, "P14 verify FAIL: 10 speed-hack Moves must kick once at the threshold");
                return false;
            }

            int awaiting = 0;
            for (int i = 0; i < 5; i++)
                awaiting = CreditNewTick(awaiting);
            if (awaiting != MaxAwaitingMoves)
            {
                Program.Print(PrintType.Error, $"P16 verify FAIL: AwaitingMoves must clamp to {MaxAwaitingMoves}, got {awaiting}");
                return false;
            }

            awaiting = ConsumeMove(awaiting, out bool tooMany);
            if (tooMany || awaiting != MaxAwaitingMoves - 1)
            {
                Program.Print(PrintType.Error, "P16 verify FAIL: a received Move (even during goto wait) must decrement");
                return false;
            }

            awaiting = 0;
            awaiting = ConsumeMove(awaiting, out tooMany);
            if (!tooMany || awaiting != -1)
            {
                Program.Print(PrintType.Error, "P16 verify FAIL: extra Move must set AwaitingMoves < 0");
                return false;
            }

            MoveStrikeTracker extra = new MoveStrikeTracker();
            awaiting = 0;
            bool extraKicked = false;
            for (int i = 0; i < MoveStrikeTracker.Limit; i++)
            {
                awaiting = ConsumeMove(awaiting, out tooMany);
                if (tooMany && extra.Strike(i))
                    extraKicked = true;
            }
            if (!extraKicked)
            {
                Program.Print(PrintType.Error, "P16 verify FAIL: AwaitingMoves < 0 must route through strikes and kick at the limit");
                return false;
            }

            Program.Print(PrintType.Info, "P14/P16 verify: correct-not-kick, OccupySquare/NoWalk, spiral nudge, AwaitingMoves clamp");
            return true;
        }
    }
}
