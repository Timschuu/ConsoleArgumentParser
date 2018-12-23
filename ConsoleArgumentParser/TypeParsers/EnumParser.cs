using System;
using ConsoleArgumentParser.Interfaces;

namespace ConsoleArgumentParser.TypeParsers
{
    public class EnumParser : ITypeParser
    {
        public bool TryParse(string s, Type targettype, out object value)
        {
            value = default(object);
            if (!Enum.TryParse(targettype, s, true, out object val))
            {
                return false;
            }

            value = val;
            return true;
        }
    }
}