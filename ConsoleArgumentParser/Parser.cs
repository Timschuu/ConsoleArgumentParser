using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMethodReturnValue.Global

namespace ConsoleArgumentParser
{
    public class Parser
    {
        public event EventHandler<ParserErrorArgs> WrongCommandUsage;
        public event EventHandler<ParserErrorArgs> InvalidSubCommand;
        public event EventHandler<EventArgs> UnknownCommand;
        public event EventHandler<ParserErrorArgs> ArgumentParsingError;
        
        private readonly List<CommandType> _registeredCommands;
        private readonly string _commandPrefix;
        private readonly string _subcommandPrefix;

        public Parser(string commandPrefix, string subcommandPrefix)
        {
            _registeredCommands = new List<CommandType>();
            _commandPrefix = commandPrefix;
            _subcommandPrefix = subcommandPrefix;
        }

        public bool RegisterCommand(CommandType commandType)
        {
            if (!commandType.Command.IsClass || !commandType.Command.GetInterfaces().Contains(typeof(ICommand)))
            {
                return false;
            }
            _registeredCommands.Add(commandType);
            return true;
        }
        
        private IEnumerable<string> GetStringsUntilNextArgument(ref int i, List<string> args)
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

        public string GetHelpString()
        {
            string output = "";
            foreach (CommandType registeredCommand in _registeredCommands)
            {
                output += registeredCommand.Helptext + "\n";
            }

            return output;
        }

        public bool ParseCommand(string command, IEnumerable<string> arguments)
        {
            Type commandtype = _registeredCommands.FirstOrDefault(c => c.Command.GetCustomAttributes(typeof(CommandAttribute), true)
                                                                           .FirstOrDefault(a => ((CommandAttribute) a)
                                                                                                ?.Name == command) != null)?.Command;
            if (commandtype == null)
            {
                OnUnknownCommand();
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

            List<ParameterInfo> ctorParas = constructorInfo.GetParameters().ToList();

            object[] ctorInvokingArgs = ParseArguments(stringsuntilnextarg, ctorParas, command).ToArray();
            
            ICommand cmd = (ICommand) constructorInfo.Invoke(ctorInvokingArgs);
            
            for (int j = 0; j < arglist.Count; j++)
            {
                string arg = arglist[j];
                if (!arg.StartsWith(_subcommandPrefix))
                {
                    OnWrongCommandUsage(new ParserErrorArgs(command));
                    return false;
                }

                List<string> subcommandargs = GetStringsUntilNextArgument(ref j, arglist).ToList();
                MethodInfo mi = cmd.GetType()
                    .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                    .FirstOrDefault(m => m
                    .GetCustomAttributes(typeof(CommandArgumentAttribute), true)
                    .FirstOrDefault(a => ((CommandArgumentAttribute) a)?.Name == arg) != null);

                if (mi == null)
                {
                    OnInvalidSubCommand( new ParserErrorArgs(command) {Subcommand = arg});
                    return false;
                }
                
                List<ParameterInfo> parameterInfos = mi.GetParameters().ToList();

                if (parameterInfos.Count != subcommandargs.Count)
                {
                    OnWrongCommandUsage(new ParserErrorArgs(command));
                    return false;
                }

                object[] invokingargs = ParseArguments(subcommandargs, parameterInfos, command).ToArray();
                
                mi.Invoke(cmd, invokingargs);
            }
            cmd.Execute();
            return true;
        }
        
        private readonly Dictionary<Type, Func<Action<ParserErrorArgs>, string, string, Type, object>> _typeParsingSwitch = new Dictionary<Type, Func<Action<ParserErrorArgs>, string, string, Type, object>>
        {
            {typeof(int), (action, command, s, _) =>
            {
                if (!int.TryParse(s, out int val))
                {
                    action(new ParserErrorArgs(command));
                }

                return val;
            }},
            {typeof(float), (action, command, s, _) =>
            {
                if (!float.TryParse(s, out float val))
                {
                    action(new ParserErrorArgs(command));
                }

                return val;
            }},
            {typeof(double), (action, command, s, _) =>
            {
                if (!double.TryParse(s, out double val))
                {
                    action(new ParserErrorArgs(command));
                }

                return val;
            }},
            {typeof(string), (action, command, s, _) => s},
            {typeof(bool), (action, command, s, _) =>
            {
                if (!bool.TryParse(s, out bool val))
                {
                    action(new ParserErrorArgs(command));
                }

                return val;
            }},
            {typeof(Enum), (action, command, s, type) =>
            {
                if (!Enum.TryParse(type, s, true, out object val))
                {
                    action(new ParserErrorArgs(command));
                }

                return val;
            }}
        };

        private List<object> ParseArguments(IReadOnlyList<string> args, IReadOnlyList<ParameterInfo> expectedParameters, string currentcommmand)
        {
            List<object> parsedArgs = new List<object>();
            for (int i = 0; i < args.Count; i++)
            {
                Type expectedType = expectedParameters[i].ParameterType;
                Type parsingTarget = expectedType;
                if (expectedType.BaseType == typeof(Enum))
                {
                    expectedType = typeof(Enum);
                }
                if (!_typeParsingSwitch.ContainsKey(expectedType))
                {
                    OnArgumentParsingError(new ParserErrorArgs(currentcommmand));
                    return null;
                }

                parsedArgs.Add(_typeParsingSwitch[expectedType](OnArgumentParsingError, currentcommmand, args[i], parsingTarget));
            }

            return parsedArgs;
        }

        private void OnArgumentParsingError(ParserErrorArgs e)
        {
            ArgumentParsingError?.Invoke(this, e);
        }
        
        private void OnWrongCommandUsage(ParserErrorArgs e)
        {
            WrongCommandUsage?.Invoke(this, e);
        }

        private void OnInvalidSubCommand(ParserErrorArgs e)
        {
            InvalidSubCommand?.Invoke(this, e);
        }

        private void OnUnknownCommand()
        {
            UnknownCommand?.Invoke(this, EventArgs.Empty);
        }
    }
}