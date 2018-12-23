using System;
using ConsoleArgumentParser.Interfaces;

namespace ConsoleArgumentParser.TypeParsers
{
    public class IntParser : ITypeParser
    {
        public object Parse(string s, Type targettype)
        {
            if (!int.TryParse(s, out int val))
            {
                throw new TypeParsingException();
            }

            return val;
        }
    }
}