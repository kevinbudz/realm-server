using System;
using System.Collections.Generic;
using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Networking;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    //Port of the reference OryxTaunt behavior. The reference looks up StringList entries by the host's id; the lines
    //below are copied from files/xml/stringlist.xml ('new', 'killed', 'death') for the hosts that use OryxTaunt in the
    //XML data. {KILLER} is replaced with the killer's name. Lines are sent as "#Oryx the Mad God" to every player in the
    //world, as in the reference. The reference's killer is the death's shooter; Death() gets no killer in this engine,
    //so the top damage dealer stands in for it.
    public class OryxTaunt : Behavior
    {
        private sealed class Lines
        {
            public string[] New;
            public string[] Killed;
            public string[] Death;
        }

        private static readonly Dictionary<string, Lines> Taunts = new Dictionary<string, Lines>
        {
            ["Dragon Head Leader"] = new Lines
            {
                New = new[]
                {
                    "The Rock Dragon has been summoned.",
                    "Beware my Rock Dragon. All who face him shall perish.",
                },
                Killed = new[]
                {
                    "{KILLER} knows not what he has done.  That Lair was guarded for the Realm's own protection!",
                    "{KILLER}, you have angered me for the last time!",
                    "{KILLER} will never survive the trials that lie ahead.",
                    "A filthy weakling like {KILLER} could never have defeated my Rock Dragon!!!",
                    "You shall not live to see the next sunrise, {KILLER}!",
                },
                Death = new[]
                {
                    "My Rock Dragon will return!",
                    "The Rock Dragon has failed me!",
                },
            },
            ["Beach Bum"] = new Lines
            {
                New = new[]
                {
                    "An elusive Beach Bum is hiding somewhere in the Realm.",
                    "What is this lazy Beach Bum doing in my Realm?!",
                },
                Killed = new string[0],
                Death = new string[0],
            },
            ["Ghost Ship"] = new Lines
            {
                New = new[]
                {
                    "A Ghost Ship has entered the Realm.",
                    "My Ghost Ship will terrorize you pathetic peasants!",
                },
                Killed = new[]
                {
                    "{KILLER}, you foul creature.  I shall see to your death personally!",
                    "{KILLER}, has crossed me for the last time! My Ghost Ship shall be avenged.",
                    "{KILLER} is such a jerk!",
                    "How could a creature like {KILLER} defeat my dreaded Ghost Ship?!",
                    "The spirits of the sea will seek revenge on your worthless soul, {KILLER}!",
                },
                Death = new[]
                {
                    "My Ghost Ship will return!",
                    "Alas, my beautiful Ghost Ship has sunk!",
                },
            },
            ["Skull Shrine"] = new Lines
            {
                New = new[]
                {
                    "Your futile efforts are no match for a Skull Shrine!",
                },
                Killed = new[]
                {
                    "{KILLER}, you insignificant cur!  The penalty for destroying a Skull Shrine is death!",
                    "{KILLER}, you contemptible pig!  Ruining my Skull Shrine will be the last mistake you ever make!",
                    "{KILLER}, you will rue the day you dared to defile my Skull Shrine!",
                    "{KILLER} razed one of my Skull Shrines -- I WILL HAVE MY REVENGE!",
                },
                Death = new[]
                {
                    "You defaced a Skull Shrine!  Minions, to arms!",
                },
            },
            ["Cube God"] = new Lines
            {
                New = new[]
                {
                    "Your meager abilities cannot possibly challenge a Cube God!",
                },
                Killed = new[]
                {
                    "{KILLER}, you pathetic swine!  How dare you assault my Cube God?",
                    "{KILLER}, you wretched dog!  You killed my Cube God!",
                    "{KILLER}, you may have destroyed my Cube God but you will never defeat me!",
                    "I have many more Cube Gods, {KILLER}!",
                },
                Death = new[]
                {
                    "You have dispatched my Cube God, but you will never escape my Realm!",
                },
            },
            ["Pentaract"] = new Lines
            {
                New = new[]
                {
                    "Behold my Pentaract, and despair!",
                },
                Killed = new[]
                {
                    "{KILLER}, you lowly scum!  You'll regret that you ever touched my Pentaract!",
                    "{KILLER}, you flea-ridden animal!  You destroyed my Pentaract!",
                    "{KILLER}, by destroying my Pentaract you have sealed your own doom!",
                },
                Death = new[]
                {
                    "That was but one of many Pentaracts!",
                    "You have razed my Pentaract, but you will die here in my Realm!",
                },
            },
            ["Grand Sphinx"] = new Lines
            {
                New = new[]
                {
                    "At last, a Grand Sphinx will teach you to respect!",
                },
                Killed = new[]
                {
                    "{KILLER}, you up-jumped goat herder!  You shall pay for defeating my Grand Sphinx!",
                    "{KILLER}, you foul ruffian!  Do not think I forget your defiling of my Grand Sphinx!",
                    "{KILLER}, you pestiferous lout!  I will not forget what you did to my Grand Sphinx!",
                    "My Grand Sphinx, she was so beautiful.  I will kill you myself, {KILLER}!",
                    "My Grand Sphinx had lived for thousands of years!  You, {KILLER}, will not survive the day!",
                },
                Death = new[]
                {
                    "The death of my Grand Sphinx shall be avenged!",
                },
            },
            ["Lord of the Lost Lands"] = new Lines
            {
                New = new[]
                {
                    "Cower in fear of my Lord of the Lost Lands!",
                    "My Lord of the Lost Lands will make short work of you!",
                },
                Killed = new[]
                {
                    "You win this time, {KILLER}, but mark my words:  You will fall before the day is done.",
                    "{KILLER}, I will never forget you exploited my Lord of the Lost Lands' weakness!",
                    "{KILLER}, you have done me a service! That Lord of the Lost Lands was not worthy of serving me.",
                    "You got lucky this time {KILLER}, but you stand no chance against me!",
                },
                Death = new[]
                {
                    "How dare you foul-mouthed hooligans treat my Lord of the Lost Lands with such indignity!",
                    "What trickery is this?!  My Lord of the Lost Lands was invincible!",
                },
            },
            ["Hermit God"] = new Lines
            {
                New = new[]
                {
                    "My Hermit God's thousand tentacles shall drag you to a watery grave!",
                },
                Killed = new[]
                {
                    "You were lucky this time, {KILLER}!  You will rue this day that you killed my Hermit God!",
                    "You naive imbecile, {KILLER}! Without my Hermit God, Dreadstump is free to roam the seas without fear!",
                    "My Hermit God was more than you'll ever be, {KILLER}.  I will kill you myself!",
                },
                Death = new[]
                {
                    "This is preposterous!  There is no way you could have defeated my Hermit God!",
                },
            },
        };

        public override void Enter(Entity host)
        {
            if (host.Parent == null || !Taunts.TryGetValue(host.Desc.Id, out Lines lines))
                return;
            string taunt = Pick(lines.New);
            if (taunt != null)
                Announce(host.Parent, taunt);
        }

        public override void Death(Entity host)
        {
            if (host.Parent == null || !Taunts.TryGetValue(host.Desc.Id, out Lines lines))
                return;

            Player killer = TopDamager(host);
            if (killer != null)
            {
                string taunt = Pick(lines.Killed);
                if (taunt != null)
                    Announce(host.Parent, taunt.Replace("{KILLER}", killer.Name));
            }
            else
            {
                string taunt = Pick(lines.Death);
                if (taunt != null)
                    Announce(host.Parent, taunt);
            }
        }

        private static string Pick(string[] lines)
        {
            return lines.Length == 0 ? null : lines[MathUtils.Next(lines.Length)];
        }

        private static void Announce(World world, string text)
        {
            ReadOnlySpan<byte> packet = GameServer.Text("#Oryx the Mad God", 0, -1, 0, "", text);
            foreach (Player player in world.Players.Values)
                player.Client.Send(packet);
        }

        private static Player TopDamager(Entity host)
        {
            Enemy enemy = host as Enemy;
            if (enemy == null || enemy.DamageStorage == null)
                return null;

            Player best = null;
            int bestDamage = 0;
            foreach (KeyValuePair<int, int> record in enemy.DamageStorage)
            {
                if (record.Value <= bestDamage)
                    continue;
                if (host.Parent.GetEntity(record.Key) is Player player)
                {
                    best = player;
                    bestDamage = record.Value;
                }
            }
            return best;
        }
    }
}
