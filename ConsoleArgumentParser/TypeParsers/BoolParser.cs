using System;
using ConsoleArgumentParser.Interfaces;

namespace ConsoleArgumentParser.TypeParsers
{
    public class BoolParser : ITypeParser
    {
        public bool TryParse(string s, Type targettype, out object value)
        {
            value = default(object);
            if (!bool.TryParse(s, out bool val))
            {
                return false;
            }

            value = val;
            return true;
        }
    }
}