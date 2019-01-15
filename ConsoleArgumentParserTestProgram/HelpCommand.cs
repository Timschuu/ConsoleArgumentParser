using System;
using ConsoleArgumentParser;
using ConsoleArgumentParser.Interfaces;

namespace ConsoleArgumentParserTestProgram
{
    [Command("-h", "Shows help.")]
    public class HelpCommand : ICommand
    {
        private readonly string _command;
        
        public HelpCommand()
        {
            
        }

        public HelpCommand(string command)
        {
            _command = command;
        }
        
        public void Execute()
        {
            Console.WriteLine(_command == null ? Program.Parser.GetHelpString() : Program.Parser.GetHelpString(_command));
        }
    }
}