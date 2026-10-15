using System;
using RotMG.Common;

namespace RotMG.Game.Commands
{
    //[W11] Marks a Player method as a chat command. Discovered by reflection (CommandTable). The name and the
    //aliases are matched against the caller's first word after ToLower(), so they must be written in lower case
    //to be reachable. The rank is checked for every command before the method runs.
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class CommandAttribute : Attribute
    {
        public CommandAttribute(string name, Rank minRank, params string[] aliases)
        {
            Name = name;
            MinRank = minRank;
            Aliases = aliases ?? Array.Empty<string>();
        }

        public string Name { get; }
        public Rank MinRank { get; }
        public string[] Aliases { get; }

        //false (default): a caller below MinRank gets "Not ranked".
        //true: a caller below MinRank gets no reply. Used by commands that were silently ignored before W11.
        public bool Silent { get; set; }
    }
}
