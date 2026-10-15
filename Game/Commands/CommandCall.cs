namespace RotMG.Game.Commands
{
    //[W11] What a command handler receives. Built from the same split that the chat switch used.
    public readonly struct CommandCall
    {
        public CommandCall(string command, string[] args, string input)
        {
            Command = command;
            Args = args;
            Input = input;
        }

        //The first word as typed (not lower-cased). /link and /unlink compare against it.
        public string Command { get; }

        //The words after the command, as the old switch split them.
        public string[] Args { get; }

        //Args joined with single spaces.
        public string Input { get; }
    }
}
