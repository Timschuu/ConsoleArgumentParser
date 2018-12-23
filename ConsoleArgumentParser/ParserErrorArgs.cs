using System;

namespace ConsoleArgumentParser
{
    public class ParserErrorArgs : EventArgs
    {
        public string Command { get; set; }
        public string Subcommand { get; set; }

        public ParserErrorArgs(string command, string subcommand = null)
        {
            Command = command;
            Subcommand = subcommand;
        }
    }
}