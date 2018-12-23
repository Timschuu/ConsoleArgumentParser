using System;
using ConsoleArgumentParser.Interfaces;

namespace ConsoleArgumentParser.TypeParsers
{
    public class FloatParser : ITypeParser
    {
        public object Parse(string s, Type targettype)
        {
            if (!float.TryParse(s, out float val))
            {
                throw new TypeParsingException();
            }

            return val;
        }
    }
}