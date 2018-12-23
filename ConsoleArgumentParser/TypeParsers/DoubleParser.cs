using System;
using ConsoleArgumentParser.Interfaces;

namespace ConsoleArgumentParser.TypeParsers
{
    public class DoubleParser : ITypeParser
    {
        public object Parse(string s, Type targettype)
        {
            if (!double.TryParse(s, out double val))
            {
                throw new TypeParsingException();
            }

            return val;
        }
    }
}