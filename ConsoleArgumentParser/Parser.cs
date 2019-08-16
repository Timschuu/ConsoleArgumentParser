using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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
        /// <summary>
        /// Gets invoked when a correct command is used but the arguments do not match the command signature
        /// </summary>
        public event EventHandler<ParserErrorArgs> WrongCommandUsage;
        /// <summary>
        /// Gets invoked when a subcommand is used that does not exist or the given arguments do not match
        /// </summary>
        public event EventHandler<ParserErrorArgs> InvalidSubCommand;
        /// <summary>
        /// Gets invoked when an unknown command is used
        /// </summary>
        public event EventHandler<EventArgs> UnknownCommand;

        private readonly List<Type> _registeredCommands;
        private readonly string _commandPrefix;
        private readonly string _subcommandPrefix;

        /// <summary>
        /// Creates a new instance of the ConsoleArgumentParser
        /// </summary>
        /// <param name="commandPrefix">The prefix to define commands</param>
        /// <param name="subcommandPrefix">The prefix to define subcommands</param>
        public Parser(string commandPrefix, string subcommandPrefix)
        {
            _registeredCommands = new List<Type>();
            _commandPrefix = commandPrefix;
            _subcommandPrefix = subcommandPrefix;
        }

        /// <summary>
        /// Registers a command for this parser to use
        /// </summary>
        /// <param name="command">The classtype of the command</param>
        /// <returns>True on success, otherwise false</returns>
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
            var output = new List<string>();
            i++;
            while (i < args.Count && !args[i].StartsWith(_subcommandPrefix))
            {
                output.Add(args[i]);
                i++;
            }

            i--;
            return output;
        }

        /// <summary>
        /// Generates a string containing a formatted helptext for all registered commands
        /// </summary>
        /// <returns>The helpstring</returns>
        public string GetHelpString()
        {
            var sb = new StringBuilder();
            foreach (var registeredCommand in _registeredCommands)
            {
                var commandattribute = registeredCommand.GetAttribute<CommandAttribute>();
                var constructorInfos = registeredCommand.GetConstructors();
                var subCommands = registeredCommand.GetSubCommands();

                sb.Append(commandattribute.Name);
                sb.Append(" ");
                sb.Append(GetMethodParameterString(constructorInfos[0]));

                if (constructorInfos.Length > 1)
                {
                    sb.Append($" (and {constructorInfos.Length - 1} Overloads)");
                }

                sb.Append(subCommands.Aggregate("", (current, subCommand) => current + " [" +
                    subCommand.GetAttributeValue((CommandArgumentAttribute caa) => caa.Name) +
                    " " + GetMethodParameterString(subCommand) + "]"));

                sb.AppendLine();
                sb.AppendLine(commandattribute.Description);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string GetMethodParameterString(MethodBase method)
        {
            var sb = new StringBuilder();
            var ctorparas = method.GetParameters();
            foreach (var ctorpara in ctorparas)
            {
                sb.Append(ctorpara.Name);
                if (IsParams(ctorpara))
                {
                    sb.Append("[s]");
                }

                sb.Append(" ");

            }

            return sb.ToString().Trim();
        }

        private Type GetCommandTypeByName(string name)
        {
            if (name.StartsWith(_commandPrefix))
            {
                name = name.Replace(_commandPrefix, "");
            }
            return _registeredCommands.FirstOrDefault(c => c.GetAttributeValue((CommandAttribute ca) => ca.Name) == _commandPrefix + name);
        }

        /// <summary>
        /// Generates a string containing a formatted helptext for a specific registered command
        /// </summary>
        /// <param name="command">A registered command.</param>
        /// <returns>The helpstring. Empty, if the command could not be found.</returns>
        public string GetHelpString(string command)
        {
            var commandtype = GetCommandTypeByName(command);
            if (commandtype == null)
            {
                return "";
            }

            var commandattribute = commandtype.GetAttribute<CommandAttribute>();

            var sb = new StringBuilder();
            sb.AppendLine("Command: " + commandattribute.Name);
            sb.AppendLine(commandattribute.Description);

            var constructors = commandtype.GetConstructors();
            sb.AppendLine(constructors.Length + " Overloads");

            sb.Append(constructors.Aggregate("",
                (current, constructor) => current + commandattribute.Name +
                                          " " + GetMethodParameterString(constructor) + Environment.NewLine));

            sb.AppendLine();
            sb.AppendLine("Arguments:");

            var subcommands = commandtype.GetSubCommands();

            sb.Append(subcommands.Aggregate("", (current, subcommand) =>
            {
                var commandArgumentAttribute = subcommand.GetAttribute<CommandArgumentAttribute>();
                return current + "[" + commandArgumentAttribute.Name +
                       " " + GetMethodParameterString(subcommand) + "] " +
                       commandArgumentAttribute.Description + Environment.NewLine;
            }));

            return sb.ToString();
        }

        private IEnumerable<string> GetArgsUntilNextArgument(ref int i, IReadOnlyList<string> args)
        {
            var argslList = new List<string>();
            i++;
            while (i < args.Count && (!args[i].StartsWith(_commandPrefix) || args[i].StartsWith(_subcommandPrefix)))
            {
                argslList.Add(args[i]);
                i++;
            }

            i--;
            return argslList;
        }

        /// <summary>
        /// Parses an entire user input including multiple commands
        /// </summary>
        /// <param name="args">The user input. Can be the command line args passed to main</param>
        public void ParseCommands(IEnumerable<string> args)
        {
            var arguments = args.ToArray();
            for (var i = 0; i < arguments.Length; i++)
            {
                ParseCommand(arguments[i], GetArgsUntilNextArgument(ref i, arguments));
            }
        }

        /// <summary>
        /// Parses a single command. Command and arguments need to seperated first.
        /// </summary>
        /// <param name="commandname">The command name including the prefix</param>
        /// <param name="arguments">The command parameters</param>
        /// <returns>True on success, otherwise false</returns>
        public bool ParseCommand(string commandname, IEnumerable<string> arguments)
        {
            var commandtype = GetCommandTypeByName(commandname);
            if (commandtype == null)
            {
                OnUnknownCommand();
                return false;
            }

            var arglist = arguments.ToList();
            var commandarguments = new List<string>();

            int i;
            for (i = 0; i < arglist.Count; i++)
            {
                if (arglist[i].StartsWith(_commandPrefix))
                {
                    break;
                }

                commandarguments.Add(arglist[i]);
            }
            arglist.RemoveRange(0, i);

            var constructorInfo = GetCorrectOverload(commandtype.GetConstructors(), commandarguments) as ConstructorInfo;

            if (constructorInfo == null)
            {
                OnWrongCommandUsage(new ParserErrorArgs(commandname));
                return false;
            }

            var ctorParas = constructorInfo.GetParameters();

            var ctorInvokingArgs = ParseArguments(commandarguments, ctorParas)?.ToArray();

            if (ctorInvokingArgs == null)
            {
                return false;
            }

            var cmd = (ICommand) constructorInfo.Invoke(ctorInvokingArgs);

            if (!ParseSubCommands(arglist, commandname, cmd))
            {
                return false;
            }

            cmd.Execute();
            return true;
        }

        private MethodBase GetCorrectOverload(IEnumerable<MethodBase> overloads, IReadOnlyList<string> args)
        {
            return overloads
                .Where(c => c.GetParameters().Length <= args.Count)
                .OrderBy(c =>  args.Count - c.GetParameters().Length)
                .ThenBy(c => IncludesParams(c.GetParameters()))
                .FirstOrDefault(c => CompareParameters(args, c.GetParameters()));
        }

        private static bool IncludesParams(IEnumerable<ParameterInfo> parameters)
        {
            return parameters.Any(IsParams);
        }

        private bool CompareParameters(IReadOnlyList<string> args, IReadOnlyList<ParameterInfo> expected)
        {
            if (args.Count < expected.Count)
            {
                return false;
            }

            for (var i = 0; i < expected.Count; i++)
            {
                if (i == expected.Count - 1 && IsParams(expected[i]))
                {
                    return true;
                }

                if (ParseArgument(args[i], expected[i].ParameterType) == null)
                {
                    return false;
                }
            }

            return true;
        }

        private bool ParseSubCommands(IReadOnlyList<string> arglist, string command, ICommand cmd)
        {
            for (var j = 0; j < arglist.Count; j++)
            {
                var subcommand = arglist[j];
                if (!subcommand.StartsWith(_subcommandPrefix))
                {
                    OnWrongCommandUsage(new ParserErrorArgs(command));
                    return false;
                }

                var subcommandargs = GetStringsUntilNextArgument(ref j, arglist).ToList();

                var methods = cmd.GetType()
                    .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                    .Where(m => m.GetCustomAttributes(typeof(CommandArgumentAttribute), true)
                        .FirstOrDefault(a => ((CommandArgumentAttribute) a)?.Name == subcommand) != null).ToList();

                if (!methods.Any())
                {
                    OnInvalidSubCommand( new ParserErrorArgs(command, subcommand));
                    return false;
                }

                var methodInfo = GetCorrectOverload(methods, subcommandargs) as MethodInfo;

                if (methodInfo == null)
                {
                    OnInvalidSubCommand( new ParserErrorArgs(command, subcommand));
                    return false;
                }

                var parameterInfos = methodInfo.GetParameters();

                var invokingargs = ParseArguments(subcommandargs, parameterInfos)?.ToArray();

                if (invokingargs == null)
                {
                    OnInvalidSubCommand(new ParserErrorArgs(command, subcommand));
                    return false;
                }

                methodInfo.Invoke(cmd, invokingargs);
            }

            return true;
        }

        /// <summary>
        /// Adds a custom type parser to parse types that are not beeing handled by the library.
        /// </summary>
        /// <param name="type">The type that will be parsed</param>
        /// <param name="parser">An iTypeParser intance</param>
        /// <returns>True on success, otherwise false</returns>
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
            {typeof(int),     new PrimitiveParser<int>(int.TryParse)},
            {typeof(float),   new PrimitiveParser<float>(float.TryParse)},
            {typeof(double),  new PrimitiveParser<double>(double.TryParse)},
            {typeof(uint),    new PrimitiveParser<uint>(uint.TryParse)},
            {typeof(long),    new PrimitiveParser<long>(long.TryParse)},
            {typeof(char),    new PrimitiveParser<char>(char.TryParse)},
            {typeof(bool),    new PrimitiveParser<bool>(bool.TryParse)},
            {typeof(byte),    new PrimitiveParser<byte>(byte.TryParse)},
            {typeof(sbyte),   new PrimitiveParser<sbyte>(sbyte.TryParse)},
            {typeof(short),   new PrimitiveParser<short>(short.TryParse)},
            {typeof(decimal), new PrimitiveParser<decimal>(decimal.TryParse)},
            {typeof(ushort),  new PrimitiveParser<ushort>(ushort.TryParse)},
            {typeof(ulong),   new PrimitiveParser<ulong>(ulong.TryParse)},
            {typeof(string),  new StringParser()}
        };

        private List<object> ParseArguments(IReadOnlyList<string> args, IReadOnlyList<ParameterInfo> expectedParameters)
        {
            if (args.Count < expectedParameters.Count)
            {
                return null;
            }
            var parsedArgs = new List<object>();
            for (var i = 0; i < args.Count; i++)
            {
                var expectedType = expectedParameters[i].ParameterType;

                var isParams = IsParams(expectedParameters[i]);

                if (i == expectedParameters.Count - 1 && isParams)
                {
                    var paramList = new List<object>();
                    for (; i < args.Count; i++)
                    {
                        paramList.Add(args[i]);
                    }

                    parsedArgs.Add(paramList.ToArray());
                    break;
                }

                if (i == expectedParameters.Count - 1 && !isParams && args.Count > expectedParameters.Count)
                {
                    return null;
                }

                var parsedPara = ParseArgument(args[i], expectedType);
                if (parsedPara == null)
                {
                    return null;
                }

                parsedArgs.Add(parsedPara);
            }

            return parsedArgs;
        }

        private object ParseArgument(string argument, Type expectedType)
        {
            if (!_typeParsingSwitch.ContainsKey(expectedType))
            {
                return null;
            }

            if (!_typeParsingSwitch[expectedType].TryParse(argument, expectedType, out var parsedPara))
            {
                return null;
            }

            return parsedPara;
        }

        private static bool IsParams(ParameterInfo param)
        {
            return param.GetCustomAttributes(typeof (ParamArrayAttribute), false).Length > 0;
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