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
            Parser.ArgumentParsingError += (sender, errorArgs) =>
            {
                Console.WriteLine($"Parser error in command {errorArgs.Command}" + (errorArgs.Subcommand != null ? $" (subcommand {errorArgs.Subcommand})" : ""));
            };
            Parser.WrongCommandUsage += (s, e) => Console.WriteLine("Invalid command usage on command " + e.Command);

            Parser.AddCustomTypeParser(typeof(ConsoleColor), new ConsoleColorParser());
            
            Parser.RegisterCommand(typeof(TestCommand));
            Parser.RegisterCommand(typeof(HelpCommand));

            Parser.ParseCommands(args);
        }
    }
}