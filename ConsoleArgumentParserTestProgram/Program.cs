using System.Linq;
using ConsoleArgumentParser;
using CommandType = ConsoleArgumentParser.CommandType;

namespace ConsoleArgumentParserTestProgram
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Parser parser = new Parser("-", "--");
            parser.RegisterCommand(new CommandType(typeof(TestCommand), "this is a test command")); 


            parser.ParseCommand(args[0], args.ToList().GetRange(1, args.Length - 1));
        }
    }
}