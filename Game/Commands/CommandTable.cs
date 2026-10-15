using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RotMG.Common;
using RotMG.Game.Entities;

namespace RotMG.Game.Commands
{
    public sealed class CommandInfo
    {
        public string Name;
        public string[] Aliases;
        public Rank MinRank;
        public bool Silent;
        public Action<Player, CommandCall> Handler;
    }

    //[W11] Registry of chat commands. Built once, on first use, by reflection over the [Command] methods of
    //Player. A duplicate name throws InvalidOperationException when the table is built; it never shadows another command.
    public static class CommandTable
    {
        private static readonly Dictionary<string, CommandInfo> ByName = Discover();

        //Builds the table now if it is not built yet. Program calls this at startup, so the reflection scan (and any
        //duplicate-name error) happens on the main thread before the first chat command, not on the tick thread.
        public static int Warm() => ByName.Count;

        //key is the caller's first word after ToLower(). Same lookup the old switch did.
        public static bool TryFind(string key, out CommandInfo info) => ByName.TryGetValue(key, out info);

        //Every name (primary and alias) that a caller of this rank may use, sorted. A name that ToLower() can never
        //produce (for example "/dcAll") cannot be typed, so it is not listed.
        public static List<string> NamesFor(Rank rank)
        {
            List<string> names = new List<string>();
            foreach (KeyValuePair<string, CommandInfo> entry in ByName)
            {
                if (rank >= entry.Value.MinRank && entry.Key == entry.Key.ToLower())
                    names.Add(entry.Key);
            }
            names.Sort(StringComparer.Ordinal);
            return names;
        }

        private static Dictionary<string, CommandInfo> Discover()
        {
            Dictionary<string, CommandInfo> table = new Dictionary<string, CommandInfo>(StringComparer.Ordinal);
            foreach (MethodInfo method in typeof(Player).GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                CommandAttribute attr = method.GetCustomAttribute<CommandAttribute>();
                if (attr == null)
                    continue;
                Action<Player, CommandCall> handler = Delegate.CreateDelegate(typeof(Action<Player, CommandCall>), method)
                    as Action<Player, CommandCall>;
                if (handler == null)
                    throw new InvalidOperationException($"[Command] {method.Name} must be void {method.Name}(CommandCall)");
                CommandInfo info = new CommandInfo
                {
                    Name = attr.Name,
                    Aliases = attr.Aliases,
                    MinRank = attr.MinRank,
                    Silent = attr.Silent,
                    Handler = handler
                };
                foreach (string key in new[] { attr.Name }.Concat(attr.Aliases))
                {
                    if (table.ContainsKey(key))
                        throw new InvalidOperationException($"duplicate chat command {key} (on {method.Name})");
                    table[key] = info;
                }
            }
            return table;
        }
    }
}
