using System;
using ConsoleArgumentParser.Interfaces;

namespace ConsoleArgumentParser.TypeParsers
{
    public class IntParser : ITypeParser
    {
        public bool TryParse(string s, Type targettype, out object value)
        {
            value = default(object);
            if (!int.TryParse(s, out int val))
            {
                return false;
            }

            value = val;
            return true;
        }
    }
}