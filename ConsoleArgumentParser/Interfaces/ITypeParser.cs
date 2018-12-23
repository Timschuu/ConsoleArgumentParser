using System;

namespace ConsoleArgumentParser.Interfaces
{
    public interface ITypeParser
    {
        bool TryParse(string s, Type targettype, out object value);
    }
}