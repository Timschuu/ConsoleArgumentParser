using System;
using System.Linq;
using ConsoleArgumentParser;

namespace ConsoleArgumentParserTestProgram
{
    internal class Program
    {
        public static Parser Parser { get; set; }
        public static void Main(string[] args)
        {
            Parser = new Parser("-", "--");
            Parser.ArgumentParsingError += (sender, errorArgs) =>
            {
                Console.WriteLine($"Parser error in command {errorArgs.Command}" + (errorArgs.Subcommand != null ? $" (subcommand {errorArgs.Subcommand})" : ""));
            };
                
            
            Parser.RegisterCommand(typeof(TestCommand));
            Parser.RegisterCommand(typeof(HelpCommand));

            Parser.ParseCommand(args[0], args.ToList().GetRange(1, args.Length - 1));
        }
    }
}