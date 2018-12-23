using System;
using ConsoleArgumentParser;
// ReSharper disable UnusedMember.Local

namespace ConsoleArgumentParserTestProgram
{
    [Command("-w", "Tests something.")]
    public class TestCommand : ConsoleArgumentParser.Interfaces.ICommand
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

        public TestCommand(params object[] text)
        {
            _message = (string)text[0];
        }

        [CommandArgument("--r")]
        private void RedSubCommand(bool red)
        {
            _red = red;
        }
    }
}