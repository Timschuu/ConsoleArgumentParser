using System;
using System.Linq;
using ConsoleArgumentParser;
// ReSharper disable UnusedMember.Local

namespace ConsoleArgumentParserTestProgram
{
    [Command("-w", "Tests something.")]
    public class TestCommand : ConsoleArgumentParser.Interfaces.ICommand
    {
        private readonly string _message;
        private ConsoleColor _color = ConsoleColor.White;

        public void Execute()
        {
            Console.ForegroundColor = _color;
            Console.WriteLine(_message);
        }

        public TestCommand(params object[] text)
        {
            _message = Array.ConvertAll(text, input => (string) input).Aggregate("", (current, next) => current + next + " ");
        }

        [CommandArgument("--color")]
        private void EnumSubCommand(ConsoleColor color)
        {
            _color = color;
        }

        [CommandArgument("--int")]
        private void IntSubCommand(int int1, int int2)
        {
            Console.WriteLine(int1 + int2);
        }
    }
}