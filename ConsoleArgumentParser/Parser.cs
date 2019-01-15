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

        /// <summary>
        /// Generates a string containing a formatted helptext for all registered commands
        /// </summary>
        /// <returns>The helpstring</returns>
        public string GetHelpString()
        {
            string output = "";
            foreach (Type registeredCommand in _registeredCommands)
            {
                string line = "";
                string name = registeredCommand.GetAttributeValue((CommandAttribute ca) => ca.Name);
                string description = registeredCommand.GetAttributeValue((CommandAttribute ca) => ca.Description);
                var constructorInfos = registeredCommand.GetConstructors();
                
                List<MethodInfo> subCommands = registeredCommand.GetSubCommands();
                
                line += name + " ";
                line = constructorInfos.Aggregate(line, (current, constructorInfo) => current + "(" + GetMethodParameterString(constructorInfo) + ") ");
                line = line.Replace("() ", "");

                line = subCommands.Aggregate(line, (current, subCommand) => current + "[" + 
                    subCommand.GetAttributeValue((CommandArgumentAttribute caa) => caa.Name) + 
                    " " + GetMethodParameterString(subCommand) + "] ");

                line += "\n\t";
                line += description;
                output += line + "\n";
            }

            return output;
        }

        private static string GetMethodParameterString(MethodBase method)
        {
            string output = "";
            var ctorparas = method.GetParameters();
            foreach (var ctorpara in ctorparas)
            {
                output += ctorpara.Name;
                if (IsParams(ctorpara))
                {
                    output += "[s]";
                }

                output += " ";

            }

            return output.Trim();
        }

        public string GetHelpString(string command)
        {
            if (command.StartsWith(_commandPrefix))
            {
                command = command.Replace(_commandPrefix, "");
            }

            Type commandtype = _registeredCommands.FirstOrDefault(c => c.GetAttributeValue((CommandAttribute ca) => ca.Name) == _commandPrefix + command);
            if (commandtype == null)
            {
                return "";
            }

            string output = "Command: " + command + "\n";
            output += commandtype.GetAttributeValue((CommandAttribute ca) => ca.Description) + "\n";
            
            var constructors = commandtype.GetConstructors();
            output += constructors.Length + " Overloads\n";
           
            output = constructors.Aggregate(output,
                (current, constructor) => current + _commandPrefix + command + " " + GetMethodParameterString(constructor) + "\n");

            output += "\nArguments:\n";

            var subcommands = commandtype.GetSubCommands();
            
            output = subcommands.Aggregate(output, (current, subCommand) => current + "[" + 
                subCommand.GetAttributeValue((CommandArgumentAttribute caa) => caa.Name) + 
                " " + GetMethodParameterString(subCommand) + "] " +
                subCommand.GetAttributeValue((CommandArgumentAttribute caa) => caa.Description) +"\n");

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
        
        /// <summary>
        /// Parses an entire user input including multiple commands
        /// </summary>
        /// <param name="args">The user input. Can be the command line args passed to main</param>
        public void ParseCommands(IEnumerable<string> args)
        {
            string[] arguments = args.ToArray();
            for (int i = 0; i < arguments.Length; i++)
            {
                ParseCommand(arguments[i], GetArgsUntilNextArgument(ref i, arguments));
            }
        }
        
        /// <summary>
        /// Parses a single command. Command and arguments need to seperated first.
        /// </summary>
        /// <param name="command">The command name including the prefix</param>
        /// <param name="arguments">The command parameters</param>
        /// <returns>True on success, otherwise false</returns>
        public bool ParseCommand(string command, IEnumerable<string> arguments)
        {
            Type commandtype = _registeredCommands.FirstOrDefault(c => c.GetCustomAttributes(typeof(CommandAttribute), true)
                .FirstOrDefault(a => ((CommandAttribute) a)?.Name == command) != null);
            if (commandtype == null)
            {
                OnUnknownCommand();
                return false;
            }
            
            List<string> arglist = arguments.ToList();
            List<string> commandarguments = new List<string>();
            
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

            ConstructorInfo constructorInfo = GetCorrectOverload(commandtype.GetConstructors(), commandarguments) as ConstructorInfo;

            if (constructorInfo == null)
            {
                OnWrongCommandUsage(new ParserErrorArgs(command));
                return false;
            }

            List<ParameterInfo> ctorParas = constructorInfo.GetParameters().ToList();

            object[] ctorInvokingArgs = ParseArguments(commandarguments, ctorParas)?.ToArray();

            if (ctorInvokingArgs == null) return false;
            
            ICommand cmd = (ICommand) constructorInfo.Invoke(ctorInvokingArgs);

            if (!ParseSubCommands(arglist, command, cmd))
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

            for (int i = 0; i < expected.Count; i++)
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
            for (int j = 0; j < arglist.Count; j++)
            {
                string subcommand = arglist[j];
                if (!subcommand.StartsWith(_subcommandPrefix))
                {
                    OnWrongCommandUsage(new ParserErrorArgs(command));
                    return false;
                }

                List<string> subcommandargs = GetStringsUntilNextArgument(ref j, arglist).ToList();
                
                List<MethodInfo> methods = cmd.GetType()
                    .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                    .Where(m => m.GetCustomAttributes(typeof(CommandArgumentAttribute), true)
                        .FirstOrDefault(a => ((CommandArgumentAttribute) a)?.Name == subcommand) != null)
                    .ToList();

                if (!methods.Any())
                {
                    OnInvalidSubCommand( new ParserErrorArgs(command, subcommand));
                    return false;
                }
                
                MethodInfo methodInfo = GetCorrectOverload(methods, subcommandargs) as MethodInfo;

                if (methodInfo == null)
                {
                    OnInvalidSubCommand( new ParserErrorArgs(command, subcommand));
                    return false;
                }
                
                List<ParameterInfo> parameterInfos = methodInfo.GetParameters().ToList();

                object[] invokingargs = ParseArguments(subcommandargs, parameterInfos)?.ToArray();

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
        /// <param name="type"></param>
        /// <param name="parser"></param>
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
            {typeof(int), new IntParser()},
            {typeof(float), new FloatParser()},
            {typeof(double), new DoubleParser()},
            {typeof(string), new StringParser()},
            {typeof(bool), new BoolParser()}
        };

        private List<object> ParseArguments(IReadOnlyList<string> args, IReadOnlyList<ParameterInfo> expectedParameters)
        {
            if (args.Count < expectedParameters.Count)
            {
                return null;
            }
            List<object> parsedArgs = new List<object>();
            for (int i = 0; i < args.Count; i++)
            {
                Type expectedType = expectedParameters[i].ParameterType;
                
                bool isParams = IsParams(expectedParameters[i]);

                if (i == expectedParameters.Count - 1 && isParams)
                {
                    List<object> paramList = new List<object>();
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

                object parsedPara = ParseArgument(args[i], expectedType);
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