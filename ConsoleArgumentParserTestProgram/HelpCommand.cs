using System;
using ConsoleArgumentParser;
using ConsoleArgumentParser.Interfaces;

namespace ConsoleArgumentParserTestProgram
{
    [Command("-help", "Shows help.")]
    public class HelpCommand : ICommand
    {
        public void Execute()
        {
            Console.WriteLine(Program.Parser.GetHelpString());
        }
    }
}