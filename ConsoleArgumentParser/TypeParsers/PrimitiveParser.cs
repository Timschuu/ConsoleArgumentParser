using System;
using ConsoleArgumentParser.Interfaces;

namespace ConsoleArgumentParser.TypeParsers
{
    public class PrimitiveParser<T> : ITypeParser
    {
        private readonly ParserDelegate<T> _parser;

        public PrimitiveParser(ParserDelegate<T> parser)
        {
            _parser = parser;
        }

        public bool TryParse(string s, Type targettype, out object value)
        {
            value = default;
            if (!_parser(s, out var val))
            {
                return false;
            }

            value = val;
            return true;
        }
    }
}