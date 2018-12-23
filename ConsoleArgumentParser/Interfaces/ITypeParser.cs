using System;

namespace ConsoleArgumentParser.Interfaces
{
    public interface ITypeParser
    {
        object Parse(string s, Type targettype);
    }
}