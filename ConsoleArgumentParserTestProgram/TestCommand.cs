using System;
using ConsoleArgumentParser;
// ReSharper disable UnusedMember.Local

namespace ConsoleArgumentParserTestProgram
{
    [Command("-w")]
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
        
        private enum Color
        {
            Red,
            Blue,
            Green,
            Yellow
        }

        public TestCommand(string text)
        {
            _message = text;
        }

        [CommandArgument("--enum")]
        private void EnumSubCommand(Color color)
        {
            Console.WriteLine(color.ToString());
        }

        [CommandArgument("--r")]
        private void RedSubCommand(bool red)
        {
            _red = red;
        }
    }
}