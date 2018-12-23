using System;
using ConsoleArgumentParser.Interfaces;

namespace ConsoleArgumentParser.TypeParsers
{
    public class EnumParser : ITypeParser
    {
        public object Parse(string s, Type targettype)
        {
            if (!Enum.TryParse(targettype, s, true, out object val))
            {
                throw new TypeParsingException();
            }

            return val;
        }
    }
}