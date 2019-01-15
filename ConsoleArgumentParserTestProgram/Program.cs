using System;
using ConsoleArgumentParser;

namespace ConsoleArgumentParserTestProgram
{
    internal class Program
    {
        public static Parser Parser { get; private set; }
        public static void Main(string[] args)
        {
            Parser = new Parser("-", "--");
            Parser.WrongCommandUsage += (s, e) => Console.WriteLine("Invalid command usage on command " + e.Command);
            Parser.UnknownCommand += (s, e) => Console.WriteLine("Unknown command");
            Parser.InvalidSubCommand += (s, e) => Console.WriteLine($"Invalid sub command {e.Subcommand} in command {e.Command}");
            Parser.AddCustomTypeParser(typeof(ConsoleColor), new ConsoleColorParser());
            
            Parser.RegisterCommand(typeof(TestCommand));
            Parser.RegisterCommand(typeof(HelpCommand));

            Parser.ParseCommands(args);
        }
    }
}