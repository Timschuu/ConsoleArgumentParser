using System;
using ConsoleArgumentParser;
using ICommand = ConsoleArgumentParser.ICommand;

namespace ConsoleArgumentParserTestProgram
{
    [Command("-w")]
    public class TestCommand : ICommand
    {
        private readonly string _message;
        private bool _red;
        
        public void Execute()
        {
            if (_red)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }

            Console.WriteLine(_message);
        }

        public TestCommand(string text)
        {
            _message = text;
        }

        [CommandArgument("--r")]
        private void RedSubCommand(bool red)
        {
            _red = red;
        }
    }
}