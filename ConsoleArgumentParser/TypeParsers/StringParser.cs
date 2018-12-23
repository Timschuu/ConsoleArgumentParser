using System;
using ConsoleArgumentParser.Interfaces;

namespace ConsoleArgumentParser.TypeParsers
{
    public class StringParser : ITypeParser
    {
        public object Parse(string s, Type targettype)
        {
            return s;
        }
    }
}