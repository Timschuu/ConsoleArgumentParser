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
        
        public enum Color
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