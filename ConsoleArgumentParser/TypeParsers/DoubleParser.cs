using System;
using ConsoleArgumentParser.Interfaces;

namespace ConsoleArgumentParser.TypeParsers
{
    public class DoubleParser : ITypeParser
    {
        public bool TryParse(string s, Type targettype, out object value)
        {
            value = default(object);
            if (!double.TryParse(s, out double val))
            {
                return false;
            }

            value = val;
            return true;
        }
    }
}