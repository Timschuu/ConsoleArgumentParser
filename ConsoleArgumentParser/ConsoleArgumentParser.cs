using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
// ReSharper disable UnusedMember.Global

namespace ConsoleArgumentParser
{
    public class Parser
    {
        public event EventHandler<ParserErrorArgs> WrongCommandUsage;
        public event EventHandler<ParserErrorArgs> InvalidSubCommand;
        
        private readonly List<Type> _registeredCommands;
        private readonly string _commandPrefix;
        private readonly string _subcommandPrefix;

        public Parser(string commandPrefix, string subcommandPrefix)
        {
            _registeredCommands = new List<Type>();
            _commandPrefix = commandPrefix;
            _subcommandPrefix = subcommandPrefix;
        }

        public bool RegisterCommand(Type command)
        {
            if (!command.IsClass || !command.GetInterfaces().Contains(typeof(ICommand)))
            {
                return false;
            }
            _registeredCommands.Add(command);
            return true;
        }
        
        private IEnumerable<string> GetStringsUntilNextArgument(ref int i, IReadOnlyList<string> args)
        {
            List<string> output = new List<string>();
            i++;
            while (i < args.Count && !args[i].StartsWith(_subcommandPrefix))
            {
                output.Add(args[i]);
                i++;
            }

            i--;
            return output;
        }

        public bool ParseCommand(string command, IEnumerable<string> arguments)
        {
            Type commandtype = _registeredCommands.FirstOrDefault(c => (c.GetCustomAttribute(typeof(CommandAttribute)) as CommandAttribute)?.Name == command);
            if (commandtype == null)
            {
                return false;
            }
            
            ConstructorInfo constructorInfo = commandtype.GetConstructors()[0];

            List<string> arglist = arguments.ToList();
            List<string> stringsuntilnextarg = new List<string>();
            int i;
            for (i = 0; i < arglist.Count; i++)
            {
                if (arglist[i].StartsWith(_commandPrefix))
                {
                    break;
                }

                stringsuntilnextarg.Add(arglist[i]);
            }
            arglist.RemoveRange(0, i);
            
            ICommand cmd = (ICommand) constructorInfo.Invoke(new object[] { stringsuntilnextarg });
            
            for (int j = 0; j < arglist.Count; j++)
            {
                string arg = arglist[j];
                if (!arg.StartsWith(_subcommandPrefix))
                {
                    OnWrongCommandUsage(new ParserErrorArgs(command));
                    return false;
                }

                List<string> subcommandargs = GetStringsUntilNextArgument(ref j, arglist).ToList();
                MethodInfo mi = cmd.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).FirstOrDefault(m =>
                    (m.GetCustomAttribute(typeof(CommandArgumentAttribute)) as CommandArgumentAttribute)?.Name == arg);

                if (mi == null)
                {
                    OnInvalidSubCommand( new ParserErrorArgs(command) {Subcommand = arg});
                    return false;
                }

                mi.Invoke(cmd, new object[] {subcommandargs});
            }
            cmd.Execute();
            return true;
        }

        private void OnWrongCommandUsage(ParserErrorArgs e)
        {
            WrongCommandUsage?.Invoke(this, e);
        }

        private void OnInvalidSubCommand(ParserErrorArgs e)
        {
            InvalidSubCommand?.Invoke(this, e);
        }
    }
}