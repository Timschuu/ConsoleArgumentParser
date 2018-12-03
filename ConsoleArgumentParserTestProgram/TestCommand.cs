using System;
using System.Collections.Generic;
using ConsoleArgumentParser;
using ICommand = ConsoleArgumentParser.ICommand;

namespace ConsoleArgumentParserTestProgram
{
    [Command("-w")]
    public class TestCommand : ICommand
    {
        private readonly List<string> _messages;
        private bool _red;
        
        public void Execute()
        {
            if (_red)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            foreach (string message in _messages)
            {
                Console.WriteLine(message);
            }
        }

        public TestCommand(IEnumerable<string> args)
        {
            _messages = new List<string>();
            _messages.AddRange(args);
        }

        [CommandArgument("--r")]
        private void RedSubCommand(IEnumerable<string> args)
        {
            _red = true;
        }
    }
}