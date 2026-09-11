using RotMG.Common;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace RotMG.Game.Entities
{
    public partial class Player
    {
        private const float MoveSpeedThreshold = 1.1f;
        private const int SpeedHistoryCount = 10; //in world ticks (10 = 1 sec history), the lower the count, the stricter the detection

        public float MoveMultiplier = 1f;
        public int MoveTime;
        public int AwaitingMoves;
        public Queue<int> AwaitingGoto;
        public List<float> SpeedHistory;
        public List<float> MultiplierHistory;
        public int TickId;
        public float PushX;
        public float PushY;

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
            if (!ValidTime(time))
            {
                Client.Disconnect();
                return;
            }

            if (AwaitingGoto.Count > 0)
            {
                foreach (int gt in AwaitingGoto)
                {
                    if (gt + TimeUntilAckTimeout < time)
                    {
                        Program.Print(PrintType.Error, "Goto ack timed out");
                        Client.Disconnect();
                        return;
                    }
                }
#if DEBUG
                Program.Print(PrintType.Error, "Waiting for goto ack...");
#endif
                return;
            }

            if (!ValidMove(time, pos))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Invalid move");
#endif
                Client.Disconnect();
                return;
            }

            if (TileFullOccupied(pos.X, pos.Y))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Tile occupied");
#endif
                Client.Disconnect();
                return;
            }

            AwaitingMoves--;
            if (AwaitingMoves < 0)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Too many move packets");
#endif
                Client.Disconnect();
                return;
            }

            Tile? tile = Parent.GetTile((int)pos.X, (int)pos.Y);
            if (tile == null)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Move out of bounds");
#endif
                Client.Disconnect();
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
            //Server-authoritative sweep: deals real bullet damage over the
            //validated movement segment and records unreported contacts
            //for counting at expiry (see VerifyProjectiles). Damage here
            //cannot disconnect mid-loop; suspicion only counts at expiry.
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

            if (!AwaitingGoto.TryDequeue(out int t))
            {
#if DEBUG
                Program.Print(PrintType.Error, "No GotoAck to ack");
#endif
                Client.Disconnect();
                return;
            }
        }

        //Admin teleports (/tq) target entities the player has often never
        //seen: every unseen tile mismatches the client's last-seen
        //UpdateCount, so those callers bypass the seen check. The client's
        //update loop streams the destination tiles on the following ticks,
        //exactly as on dungeon entry. RegionUnblocked still applies.
        public bool Teleport(int time, Position pos, bool ignoreSeen = false)
        {
            if (!RegionUnblocked(pos.X, pos.Y))
                return false;

            Tile? tile = Parent.GetTileF((int)pos.X, (int)pos.Y);
            if (tile == null || (!ignoreSeen && GetSeenTileUpdate((int)pos.X, (int)pos.Y) != tile.Value.UpdateCount))
                return false;

            Parent.MoveEntity(this, pos);
            AwaitingGoto.Enqueue(time);

            byte[] eff = GameServer.ShowEffect(ShowEffectIndex.Teleport, Id, 0xFFFFFFFF, pos);
            byte[] go = GameServer.Goto(Id, pos);

            foreach (Player player in Parent.Players.Values)
            {
                if (player.Client.Account.Effects)
                    player.Client.Send(eff);
                player.Client.Send(go);
            }
            return true;
        }
    }
}
