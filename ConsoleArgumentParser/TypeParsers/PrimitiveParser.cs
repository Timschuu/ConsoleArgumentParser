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
            value = default(object);
            if (!_parser(s, out T val))
            {
                return false;
            }

            value = val;
            return true;
        }
    }
}