using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ConsoleArgumentParser.Extensions;
using ConsoleArgumentParser.Interfaces;
using ConsoleArgumentParser.TypeParsers;
// ReSharper disable EventNeverSubscribedTo.Global
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
            foreach (Type registeredCommand in _registeredCommands)
            {
                string line = "";
                string name = registeredCommand.GetAttributeValue((CommandAttribute ca) => ca.Name);
                string description = registeredCommand.GetAttributeValue((CommandAttribute ca) => ca.Description);
                ConstructorInfo constructorInfo = registeredCommand.GetConstructors()[0];
                List<ParameterInfo> ctorparas = constructorInfo.GetParameters().ToList();

                List<MethodInfo> subCommands = registeredCommand.GetSubCommands();
                
                line += name + " ";
                foreach (var ctorpara in ctorparas)
                {
                    line += ctorpara.Name;
                    if (IsParams(ctorpara))
                    {
                        line += "[s]";
                    }

                    line += " ";
                }

                foreach (var subCommand in subCommands)
                {
                    line += $"[{subCommand.GetAttributeValue((CommandArgumentAttribute caa) => caa.Name)} ";
                    line = subCommand.GetParameters().Aggregate(line, (current, parameter) => current + parameter.Name + " ");
                    line = line.Trim();
                    line += "] ";
                }

                line += "\n\t";
                line += description;
                output += line + "\n";
            }

            return output;
        }

        private IEnumerable<string> GetArgsUntilNextArgument(ref int i, IReadOnlyList<string> args)
        {
            List<string> argslList = new List<string>();
            i++;
            while (i < args.Count && (!args[i].StartsWith(_commandPrefix) || args[i].StartsWith(_subcommandPrefix)))
            {
                argslList.Add(args[i]);
                i++;
            }

            i--;
            return argslList;
        }
        
        public void ParseCommands(IEnumerable<string> args)
        {
            string[] arguments = args.ToArray();
            for (int i = 0; i < arguments.Length; i++)
            {
                ParseCommand(arguments[i], GetArgsUntilNextArgument(ref i, arguments));
            }
        }
        
        public bool ParseCommand(string command, IEnumerable<string> arguments)
        {
            Type commandtype = _registeredCommands.FirstOrDefault(c => c.GetCustomAttributes(typeof(CommandAttribute), true)
                .FirstOrDefault(a => ((CommandAttribute) a)?.Name == command) != null);
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

            object[] ctorInvokingArgs = ParseArguments(stringsuntilnextarg, ctorParas, command, null)?.ToArray();

            if (ctorInvokingArgs == null)
            {
                OnWrongCommandUsage(new ParserErrorArgs(command));
                return false;
            }
            
            ICommand cmd = (ICommand) constructorInfo.Invoke(ctorInvokingArgs);

            if (!ParseSubCommands(arglist, command, cmd))
            {
                return false;
            }
            
            cmd.Execute();
            return true;
        }

        private bool ParseSubCommands(List<string> arglist, string command, ICommand cmd)
        {
            for (int j = 0; j < arglist.Count; j++)
            {
                string subcommand = arglist[j];
                if (!subcommand.StartsWith(_subcommandPrefix))
                {
                    OnWrongCommandUsage(new ParserErrorArgs(command));
                    return false;
                }

                List<string> subcommandargs = GetStringsUntilNextArgument(ref j, arglist).ToList();
                MethodInfo mi = cmd.GetType()
                    .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                    .FirstOrDefault(m => m
                                             .GetCustomAttributes(typeof(CommandArgumentAttribute), true)
                                             .FirstOrDefault(a => ((CommandArgumentAttribute) a)?.Name == subcommand) != null);

                if (mi == null)
                {
                    OnInvalidSubCommand( new ParserErrorArgs(command, subcommand));
                    return false;
                }
                
                List<ParameterInfo> parameterInfos = mi.GetParameters().ToList();

                object[] invokingargs = ParseArguments(subcommandargs, parameterInfos, command, subcommand)?.ToArray();

                if (invokingargs == null)
                {
                    OnWrongCommandUsage(new ParserErrorArgs(command, subcommand));
                    return false;
                }
                
                mi.Invoke(cmd, invokingargs);
            }

            return true;
        }

        public bool AddCustomTypeParser(Type type, ITypeParser parser)
        {
            if (_typeParsingSwitch.ContainsKey(type))
            {
                return false;
            }
            _typeParsingSwitch.Add(type, parser);
            return true;
        }
        
        private readonly Dictionary<Type, ITypeParser> _typeParsingSwitch = new Dictionary<Type, ITypeParser>
        {
            {typeof(int), new IntParser()},
            {typeof(float), new FloatParser()},
            {typeof(double), new DoubleParser()},
            {typeof(string), new StringParser()},
            {typeof(bool), new BoolParser()}
        };

        private List<object> ParseArguments(IReadOnlyList<string> args, IReadOnlyList<ParameterInfo> expectedParameters, string currentcommmand, string currentsubcommand)
        {
            if (args.Count < expectedParameters.Count)
            {
                return null;
            }
            List<object> parsedArgs = new List<object>();
            for (int i = 0; i < args.Count; i++)
            {
                Type expectedType = expectedParameters[i].ParameterType;
                Type parsingTarget = expectedType;
                if (expectedType.BaseType == typeof(Enum))
                {
                    expectedType = typeof(Enum);
                }

                if (i == expectedParameters.Count - 1 && IsParams(expectedParameters[i]))
                {
                    List<object> paramList = new List<object>();
                    for (; i < args.Count; i++)
                    {
                        paramList.Add(args[i]);
                    }

                    parsedArgs.Add(paramList.ToArray());
                    break;
                }
                
                if (!_typeParsingSwitch.ContainsKey(expectedType))
                {
                    OnArgumentParsingError(new ParserErrorArgs(currentcommmand));
                    return null;
                }
                
                if (!_typeParsingSwitch[expectedType].TryParse(args[i], parsingTarget, out var parsedPara))
                {
                    OnArgumentParsingError(new ParserErrorArgs(currentcommmand, currentsubcommand));
                    return null;
                }
                parsedArgs.Add(parsedPara);
            }

            return parsedArgs;
        }
        
        private static bool IsParams(ParameterInfo param)
        {
            return param.GetCustomAttributes(typeof (ParamArrayAttribute), false).Length > 0;
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