using System;
using ConsoleArgumentParser.Interfaces;

namespace ConsoleArgumentParser.TypeParsers
{
    public class BoolParser : ITypeParser
    {
        public object Parse(string s, Type targettype)
        {
            if (!bool.TryParse(s, out bool val))
            {
                throw new TypeParsingException();
            }

            return val;
        }
    }
}