using System;
using System.Collections.Generic;
using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Networking;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class SayState
    {
        public bool Done;
    }

    //Chat-bubble speech, nearby players or every player when Broadcast is set. Texts are ';'-separated.
    //Cooldown 0 is the reference SayBehavior: once per state entry, with Probability, no placeholders.
    //Cooldown above 0 is the reference TauntBehavior: every Cooldown ms (CooldownVariance spreads it), with Probability.
    //Taunts need a player within 10 tiles when the text names {PLAYER}, and substitute {PLAYER}, {HITPOINTS} and {HP}.
    public class Say : Behavior
    {
        public readonly string[] Texts;
        public readonly float Probability;
        public readonly bool Broadcast;
        public readonly int BubbleTime;
        public readonly int Cooldown;
        public readonly int CooldownVariance;

        public Say(string texts, double probability = 1, bool broadcast = false, int bubbleTime = 10, int cooldown = 0, int cooldownVariance = 0)
        {
            List<string> list = new List<string>();
            if (!string.IsNullOrWhiteSpace(texts))
                foreach (string raw in texts.Split(';'))
                {
                    string text = raw.Trim();
                    if (text.Length > 0)
                        list.Add(text);
                }
            Texts = list.ToArray();
            Probability = (float)probability;
            Broadcast = broadcast;
            BubbleTime = bubbleTime;
            Cooldown = cooldown;
            CooldownVariance = cooldownVariance;
        }

        public override void Enter(Entity host)
        {
            if (Cooldown > 0)
                host.StateCooldown[Id] = 0;
            else
                host.StateObject[Id] = new SayState();
        }

        public override bool Tick(Entity host)
        {
            if (Cooldown > 0)
            {
                host.StateCooldown[Id] -= Settings.MillisecondsPerTick;
                if (host.StateCooldown[Id] > 0)
                    return false;

                host.StateCooldown[Id] = Cooldown;
                if (CooldownVariance != 0)
                    host.StateCooldown[Id] += MathUtils.NextIntSnap(-CooldownVariance, CooldownVariance, Settings.MillisecondsPerTick);
            }
            else
            {
                if (!(host.StateObject.TryGetValue(Id, out object o) && o is SayState state) || state.Done)
                    return false;
                state.Done = true;
            }

            if (Texts.Length == 0 || !MathUtils.Chance(Probability))
                return false;

            string text = Texts[MathUtils.Next(Texts.Length)];
            if (Cooldown > 0)
            {
                if (text.Contains("{PLAYER}"))
                {
                    Entity player = host.GetNearestPlayer(10);
                    if (player == null || !(player is Player p))
                        return false;
                    text = text.Replace("{PLAYER}", p.Name);
                }
                string hp = host.HP.ToString();
                text = text.Replace("{HITPOINTS}", hp).Replace("{HP}", hp);
            }

            ReadOnlySpan<byte> packet = GameServer.Text("#" + (host.Desc.DisplayId ?? host.Desc.Id), host.Id, -1, BubbleTime, "", text);
            if (Broadcast && host.Parent != null)
            {
                foreach (Player player in host.Parent.Players.Values)
                    player.Client.Send(packet);
            }
            else
                BehaviorHelpers.BroadcastNearby(host, packet, 15);
            return true;
        }

        public override void Exit(Entity host)
        {
            host.StateObject.Remove(Id);
            host.StateCooldown.Remove(Id);
        }
    }
}
