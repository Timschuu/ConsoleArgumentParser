using System;
using ConsoleArgumentParser.Interfaces;

namespace ConsoleArgumentParser.TypeParsers
{
    public class FloatParser : ITypeParser
    {
        public bool TryParse(string s, Type targettype, out object value)
        {
            value = default(object);
            if (!float.TryParse(s, out float val))
            {
                return false;
            }

            value = val;
            return true;
        }
    }
}