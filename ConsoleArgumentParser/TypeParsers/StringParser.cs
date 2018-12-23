using System;
using ConsoleArgumentParser.Interfaces;

namespace ConsoleArgumentParser.TypeParsers
{
    public class StringParser : ITypeParser
    {
        public bool TryParse(string s, Type targettype, out object value)
        {
            value = s;
            return true;
        }
    }
}