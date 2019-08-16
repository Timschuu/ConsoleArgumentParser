using System;

namespace ConsoleArgumentParser
{
    public class ParserErrorArgs : EventArgs
    {
        public string Command { get; }
        public string Subcommand { get; }

        public ParserErrorArgs(string command, string subcommand = null)
        {
            Command = command;
            Subcommand = subcommand;
        }
    }
}