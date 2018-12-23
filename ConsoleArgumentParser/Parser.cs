﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

            object[] ctorInvokingArgs = ParseArguments(stringsuntilnextarg, ctorParas, command, null)?.ToArray();

            if (ctorInvokingArgs == null)
            {
                OnWrongCommandUsage(new ParserErrorArgs(command));
                return false;
            }
            
            ICommand cmd = (ICommand) constructorInfo.Invoke(ctorInvokingArgs);
            
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
            cmd.Execute();
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
            {typeof(Enum), new EnumParser()},
            {typeof(bool), new BoolParser()}
        };

        private List<object> ParseArguments(IReadOnlyList<string> args, IReadOnlyList<ParameterInfo> expectedParameters, string currentcommmand, string currentsubcommand)
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